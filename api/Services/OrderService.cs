using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Hangfire;
using JCCommon.Clients.FileServices;
using LazyCache;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Documents.Extractors;
using Scv.Api.Jobs;
using Scv.Api.SignalR.Notifications;
using Scv.Core.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Order;

namespace Scv.Api.Services;

public interface IOrderService : ICrudService<OrderDto>
{
    Task<OperationResult> ValidateOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult<OrderDto>> ProcessOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult> ReviewOrder(string id, OrderReviewDto orderReview);
    Task<IEnumerable<OrderViewDto>> GetJudgeOrdersAsync(int judgeId);
    Task<OperationResult> SubmitOrder(string id);
    Task<OrderViewDto> GetOrderByIdAsync(string id, int judgeId);
}

public class OrderService : CrudServiceBase<IRepositoryBase<Order>, Order, OrderDto>, IOrderService
{
    private readonly FileServicesClient _filesClient;
    private readonly IJudgeService _judgeService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJudicialServicesClient _judicialClient;
    private readonly IDeskOrderDetailsExtractor _deskOrderDetailsExtractor;
    private readonly ICsoTextSanitizer _csoTextSanitizer;
    private readonly IAntiVirusService _antiVirusService;
    private readonly OrderSubmittedAckNotification _orderSubmittedAck;
    public const string NOTE_TO_APPEND_IF_CLERK_DESIGNATED = "-- NOTE -- Pursuant to PCF rule 169, I designate the Clerk of the Court to sign the order on my behalf.";

    public override string CacheName => "GetOrdersAsync";

    public OrderService(
        IAppCache cache,
        IMapper mapper,
        ILogger<OrderService> logger,
        IRepositoryBase<Order> orderRepo,
        FileServicesClient filesClient,
        IConfiguration configuration,
        IJudgeService judgeService,
        IBackgroundJobClient backgroundJobClient,
        IHttpContextAccessor httpContextAccessor,
        IJudicialServicesClient judicialClient,
        IDeskOrderDetailsExtractor deskOrderDetailsExtractor,
        ICsoTextSanitizer csoTextSanitizer,
        IAntiVirusService antiVirusService,
        OrderSubmittedAckNotification orderSubmittedAck
    ) : base(
            cache,
            mapper,
            logger,
            orderRepo)
    {
        _judgeService = judgeService;
        _filesClient = filesClient;
        _backgroundJobClient = backgroundJobClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
        _requestPartId = configuration.GetNonEmptyValue("Request:PartId");
        _httpContextAccessor = httpContextAccessor;
        _judicialClient = judicialClient;
        _deskOrderDetailsExtractor = deskOrderDetailsExtractor;
        _csoTextSanitizer = csoTextSanitizer;
        _antiVirusService = antiVirusService;
        _orderSubmittedAck = orderSubmittedAck;
    }

    public async Task<OperationResult> ValidateOrderRequestAsync(OrderRequestDto dto)
    {
        var errors = new List<string>();

        // Validate file existence based on court class
        var fileId = dto.PhysicalFileId;
        if (!Enum.TryParse<CourtClassCd>(dto.CourtClassCd, true, out var courtClass))
        {
            errors.Add($"Invalid CourtClassCd: {dto.CourtClassCd}");
            return OperationResult<OrderDto>.Failure([.. errors]);
        }

        if (IsCriminalCourtClass(courtClass))
        {
            var criminalFile = await FetchCriminalFileAsync(fileId);
            if (criminalFile == null)
            {
                errors.Add($"Criminal file with id: {fileId} is not found.");
            }
        }
        else if (IsCivilCourtClass(courtClass))
        {
            var civilFile = await FetchCivilFileAsync(fileId);
            if (civilFile == null || string.IsNullOrWhiteSpace(civilFile.PhysicalFileId))
            {
                errors.Add($"Civil file with id: {fileId} is not found.");
            }
        }
        else
        {
            errors.Add($"Unsupported CourtClassCd: {courtClass}.");
        }

        // Validate judge existence
        var judges = await _judgeService.GetJudges();
        if (!judges.Any(j => j.ParticipantId.Equals(dto.Referral.SentToPartId)))
        {
            errors.Add($"Judge with id: {dto.Referral.SentToPartId} is not found.");
        }

        // More business rules validation will be added here in the future

        return errors.Count > 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }

    public async Task<OperationResult<OrderDto>> ProcessOrderRequestAsync(OrderRequestDto dto)
    {
        try
        {
            // Determine if the order already exists. If it is, this is an edit request. Otherwise, create a new one.
            var fileId = dto.PhysicalFileId;
            var existingOrders = await this.Repo
                .FindAsync(o => o.OrderRequest.PhysicalFileId == fileId
                    && o.OrderRequest.Referral.SentToPartId.Equals(dto.Referral.SentToPartId)
                    && o.OrderRequest.Referral.ReferredDocumentId == dto.Referral.ReferredDocumentId
                    && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.AwaitingDocumentation));

            var existingOrder = existingOrders?.FirstOrDefault();
            OrderDto orderDto;

            if (existingOrder != null)
            {
                this.Logger.LogInformation("Updating existing order's request for fileId: {FileId}, sentToPartId: {SentToPartId}, referredDocumentId: {ReferredDocumentId} ",
                    fileId, dto.Referral.SentToPartId, dto.Referral.ReferredDocumentId);

                orderDto = this.Mapper.Map<OrderDto>(existingOrder);

                // Update the existing order's request
                orderDto.Id = existingOrder.Id;
                orderDto.OrderRequest = dto;
            }
            else
            {
                this.Logger.LogInformation("Creating new order for fileId: {FileId}, sentToPartId: {SentToPartId}, referredDocumentId: {ReferredDocumentId} ",
                    fileId, dto.Referral.SentToPartId, dto.Referral.ReferredDocumentId);

                orderDto = new OrderDto
                {
                    OrderRequest = dto,
                    Status = OrderStatus.Pending,
                    SubmitStatus = SubmitStatus.Pending,
                    SubmitAttempts = 0,
                };
            }

            // Populate other Order fields like StyleOfCause and JudgeId that is not part of the request.
            var populateResult = await PopulateOrder(dto, orderDto);
            if (!populateResult.Succeeded)
            {
                return populateResult;
            }

            orderDto = populateResult.Payload;

            if (existingOrder != null)
            {
                var result = await this.UpdateAsync(orderDto);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
            else
            {
                var result = await this.AddAsync(orderDto);
                if (!result.Succeeded)
                {
                    return result;
                }

                orderDto = result.Payload;

                _backgroundJobClient.Enqueue<SendOrderNotificationJob>(job => job.Execute(orderDto));
            }

            this.Logger.LogInformation("Successfully upserted order {OrderId}.", orderDto.Id);

            return OperationResult<OrderDto>.Success(orderDto);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Something went wrong when upserting the Order: {Message}", ex.Message);
            return OperationResult<OrderDto>.Failure("Something went wrong when upserting the Order");
        }

    }

    public async Task<OperationResult> ReviewOrder(string id, OrderReviewDto orderReview)
    {
        try
        {
            var order = await Repo.GetByIdAsync(id);
            if (order is null)
            {
                return OperationResult.Failure("Order not found");
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return OperationResult.Failure("No authenticated user context available to review this Order.");
            }

            var assignedJudgeId = order.JudgeId;
            var judgeId = user.JudgeId();
            if (assignedJudgeId != judgeId)
            {
                return OperationResult.Failure("Judge is not assigned to review this Order.");
            }

            var documentScan = await ScanReviewDocumentAsync(orderReview.DocumentData, "signed");
            if (!documentScan.Succeeded)
            {
                return documentScan;
            }

            var supportingScan = await ScanReviewDocumentAsync(orderReview.SupportingDocumentData, "supporting");
            if (!supportingScan.Succeeded)
            {
                return supportingScan;
            }

            var orderDto = Mapper.Map<OrderDto>(order);

            if (orderReview.Status == OrderStatus.Pending)
            {
                return OperationResult.Failure("Order review status cannot be set to Pending.");
            }

            orderReview.Adapt(orderDto);

            if (orderDto.OrderRequest?.Referral?.IsDeskOrder == true)
            {
                var deskOrderValidation = ValidateDeskOrder(orderDto);
                if (!deskOrderValidation.Succeeded)
                {
                    return deskOrderValidation;
                }
            }

            var result = await UpdateAsync(orderDto);

            if (!result.Succeeded)
            {
                return result;
            }

            _backgroundJobClient.Enqueue<SubmitOrderJob>(job => job.Execute(id));
            await _orderSubmittedAck.SendAsync(orderDto, user.UserId());

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unexpected error reviewing order {OrderId}.", id.SanitizeForLog());
            return OperationResult.Failure("Failed to submit order to CSO.");
        }
    }

    public override Task<OperationResult<OrderDto>> ValidateAsync(OrderDto dto, bool isEdit = false)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<OrderViewDto>> GetJudgeOrdersAsync(int judgeId)
    {
        var judgeOrders = await this.Repo.FindAsync(o => o.JudgeId == judgeId);
        return this.Mapper.Map<List<OrderViewDto>>(judgeOrders);
    }

    public async Task<OperationResult> SubmitOrder(string id)
    {
        var order = await Repo.GetByIdAsync(id);
        if (order is null)
        {
            this.Logger.LogWarning("Order {OrderId} not found for submission.", id);
            return OperationResult.Failure("Order not found");
        }

        var orderDto = Mapper.Map<OrderDto>(order);
        orderDto.SubmitAttempts = order.SubmitAttempts.HasValue
            ? order.SubmitAttempts.Value + 1
            : 1;
        var correlationId = Guid.NewGuid();

        try
        {
            var actionDto = await MapToOrderAction(orderDto);
            if (actionDto == null)
            {
                orderDto.SubmitStatus = SubmitStatus.Error;
                var mappingStatusResult = await UpdateAsync(orderDto);
                if (!mappingStatusResult.Succeeded)
                {
                    return mappingStatusResult;
                }
                return OperationResult.Failure("Failed to map Order to OrderAction.");
            }

            double documentId = orderDto.OrderRequest?.Referral?.ReferredDocumentId.GetValueOrDefault() ?? 0;

            await _judicialClient.SaveJudicialActionAsync(
                correlationId,
                documentId,
                actionDto);

            // Cleanup the successful, submitted order to remove potentially private document data and comments.
            orderDto.DocumentData = null;
            orderDto.SupportingDocumentData = null;
            orderDto.Comments = null;
            orderDto.SubmitStatus = SubmitStatus.Submitted;

            var cleanupResult = await UpdateAsync(orderDto);
            if (!cleanupResult.Succeeded)
            {
                this.Logger.LogWarning("Failed to clean up order post submission {OrderId}.", id);
                return cleanupResult;
            }

            this.Logger.LogInformation("Order {OrderId} submitted to CSO successfully.", id);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Unexpected error submitting order {OrderId}.", id);
            orderDto.SubmitStatus = SubmitStatus.Error;
            var errorResult = await UpdateAsync(orderDto);
            return errorResult.Succeeded
                ? OperationResult.Failure("Failed to submit order to CSO.")
                : errorResult;
        }
    }

    public async Task<OrderViewDto> GetOrderByIdAsync(string id, int judgeId)
    {
        var order = await Repo.GetByIdAsync(id);
        if (order == null || order.JudgeId != judgeId)
        {
            return null;
        }
        return Mapper.Map<OrderViewDto>(order);
    }

    #region Private Methods

    private async Task<OperationResult> ScanReviewDocumentAsync(string base64Data, string documentLabel)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
        {
            return OperationResult.Success();
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            return OperationResult.Failure($"The uploaded {documentLabel} document contains invalid base64 content.");
        }

        using var stream = new MemoryStream(bytes);
        var (isClean, _) = await _antiVirusService.ScanAsync(stream);
        if (!isClean)
        {
            return OperationResult.Failure($"The uploaded {documentLabel} document failed the antivirus scan.");
        }

        return OperationResult.Success();
    }

    private static bool IsCriminalCourtClass(CourtClassCd courtClass) =>
        courtClass is CourtClassCd.A or CourtClassCd.Y or CourtClassCd.T;

    private static bool IsCivilCourtClass(CourtClassCd courtClass) =>
        courtClass is CourtClassCd.C or CourtClassCd.F or CourtClassCd.L or CourtClassCd.M;

    private Task<CriminalFileDetailResponse> FetchCriminalFileAsync(int? fileId) =>
        GetDataFromCache(
            $"{CacheName}-CriminalFile-{_requestAgencyIdentifierId}-{_requestPartId}-{fileId}",
            () => _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId.ToString()));

    private Task<CivilFileDetailResponse> FetchCivilFileAsync(int? fileId) =>
        GetDataFromCache(
            $"{CacheName}-CivilFile-{_requestAgencyIdentifierId}-{_requestPartId}-{fileId}",
            () => _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId.ToString()));

    private async Task<OperationResult<OrderDto>> PopulateOrder(OrderRequestDto dto, OrderDto orderDto)
    {
        var styleOfCause = string.Empty;
        Enum.TryParse<CourtClassCd>(dto.CourtClassCd, true, out var courtClass);
        if (IsCriminalCourtClass(courtClass))
        {
            var criminalFile = await FetchCriminalFileAsync(dto.PhysicalFileId);
            if (criminalFile != null && criminalFile.Participant != null && criminalFile.Participant.Count > 0)
            {
                var participant = criminalFile.Participant.First();
                styleOfCause = !string.IsNullOrWhiteSpace(participant.LastNm)
                    ? $"{participant.LastNm}, {participant.GivenNm}"
                    : participant.OrgNm;
            }
        }
        else if (IsCivilCourtClass(courtClass))
        {
            var civilFile = await FetchCivilFileAsync(dto.PhysicalFileId);
            if (civilFile != null && !string.IsNullOrWhiteSpace(civilFile.PhysicalFileId))
            {
                styleOfCause = civilFile.SocTxt;
            }
        }
        else
        {
            return OperationResult<OrderDto>.Failure($"Unsupported CourtClassCd: {dto.CourtClassCd}.");
        }

        var judges = await _judgeService.GetJudges();
        var judge = judges.FirstOrDefault(j => j.ParticipantId.Equals(dto?.Referral?.SentToPartId));
        if (judge == null)
        {
            return OperationResult<OrderDto>.Failure($"Judge with part id: {dto?.Referral?.SentToPartId} is not found.");
        }

        orderDto.StyleOfCause = styleOfCause;
        orderDto.JudgeId = judge.PersonId;

        return OperationResult<OrderDto>.Success(orderDto);
    }

    private async Task<JudicialAction> MapToOrderAction(OrderDto orderDto)
    {
        var referral = orderDto.OrderRequest?.Referral;
        if (referral?.ReferredDocumentId == null)
        {
            this.Logger.LogError("Order {OrderId} is invalid and cannot be submitted.", orderDto.Id);
            return null;
        }

        var judges = await _judgeService.GetJudges();
        var judge = judges.FirstOrDefault(j => j.PersonId == orderDto.JudgeId);
        if (judge == null)
        {
            this.Logger.LogError("Judge with id: {JudgeId} is not found for Order {OrderId}.", orderDto.JudgeId, orderDto.Id);
            return null;
        }

        var isValidAgencyId = double.TryParse(_requestAgencyIdentifierId, out var agencyId);
        if (!isValidAgencyId)
        {
            this.Logger.LogError("Invalid AgencyIdentifierId configuration value: {AgencyIdentifierId}.", _requestAgencyIdentifierId);
            return null;
        }

        if (judge.ParticipantId is null or 0)
        {
            this.Logger.LogError("Invalid ParticipantId for Judge with id: {JudgeId}.", orderDto.JudgeId);
            return null;
        }

        var actionDto = Mapper.Map<JudicialAction>(orderDto);
        actionDto.OrderTerms ??= [];
        actionDto.ReviewedBy = new Reviewer
        {
            AgencyId = agencyId,
            PaasSeqNo = orderDto.OrderRequest?.Referral?.ReferredByPaasSeqNo.GetValueOrDefault() ?? 0,
            PartId = judge.ParticipantId.Value
        };

        SetActionReviewDates(orderDto, actionDto);

        if (orderDto.OrderRequest?.Referral?.IsDeskOrder == true)
        {
            var deskOrderValidation = ValidateDeskOrder(orderDto);
            if (!deskOrderValidation.Succeeded)
            {
                this.Logger.LogError("Desk Order {OrderId} cannot be submitted: {Reason}",
                    orderDto.Id, string.Join(" ", deskOrderValidation.Errors));
                return null;
            }

            actionDto = PopulateDeskOrderDetails(orderDto, actionDto);
        }

        return actionDto;
    }

    private static void SetActionReviewDates(OrderDto orderDto, JudicialAction actionDto)
    {
        actionDto.RejectedDate = orderDto.Status == OrderStatus.Unapproved && orderDto.ProcessedDate.HasValue
            ? orderDto.ProcessedDate.Value
            : null;

        actionDto.SignedDate = orderDto.Signed && orderDto.ProcessedDate.HasValue
            ? orderDto.ProcessedDate.Value
            : null;
    }

    private static OperationResult ValidateDeskOrder(OrderDto orderDto)
    {
        if (orderDto.Status != OrderStatus.OrderMade)
        {
            return OperationResult.Failure("Incorrect status for submitting a desk order.");
        }

        if (orderDto.Signed && string.IsNullOrWhiteSpace(orderDto.DocumentData))
        {
            return OperationResult.Failure("Desk Order is signed but has no document data.");
        }

        if (!orderDto.Signed)
        {
            if (orderDto?.OrderRequest?.Referral?.CourtListTypeCd == CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE)
            {
                return OperationResult.Failure("Small Claims Desk Order cannot be submitted unsigned.");
            }

            if (string.IsNullOrWhiteSpace(orderDto.SupportingDocumentData))
            {
                return OperationResult.Failure("Family Desk Order is not signed but has no supporting document data.");
            }
        }

        return OperationResult.Success();
    }

    private JudicialAction PopulateDeskOrderDetails(OrderDto orderDto, JudicialAction actionDto)
    {
        if (orderDto.Signed)
        {
            // Desk Order (PSM/PFM) is signed. Nothing else to do.
            return actionDto;
        }

        // Family Desk order is not signed, extract directions and terms from the supporting document.
        actionDto.Document = [];

        var bytes = Convert.FromBase64String(orderDto.SupportingDocumentData);
        using var stream = new MemoryStream(bytes);

        var deskOrderDetails = _deskOrderDetailsExtractor.Extract(stream);

        this.Logger.LogInformation("Reasons for Rejection, Directions and Order Terms extracted successfully for Order {OrderId}.", orderDto.Id);

        var sanitizedDirections = _csoTextSanitizer.Sanitize(deskOrderDetails.Directions);
        var sanitizedRejectionReasons = _csoTextSanitizer.Sanitize(deskOrderDetails.ReasonsForRejection);
        var commentParts = new[]
        {
            actionDto.Comment,
            sanitizedRejectionReasons,
            sanitizedDirections,
            deskOrderDetails.IsClerkToSign ? NOTE_TO_APPEND_IF_CLERK_DESIGNATED : ""
        };

        actionDto.Comment = _csoTextSanitizer.Sanitize(
            string.Join(". ", commentParts.Where(p => !string.IsNullOrWhiteSpace(p))));
        actionDto.OrderTerms = [.. deskOrderDetails.OrderTerms.Select(term => new OrderTerm
            {
                SequenceNumber = term.SequenceNumber,
                Text = _csoTextSanitizer.Sanitize(term.Text),
                DisplaySortNumber = term.DisplaySortNumber
            })];

        return actionDto;
    }

    #endregion Private Methods
}
