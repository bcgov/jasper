using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using JCCommon.Clients.FileServices;
using LazyCache;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Jobs;
using Scv.Api.Models.Order;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Api.Repositories;

namespace Scv.Api.Services;

public interface IOrderService : ICrudService<OrderDto>
{
    Task<OperationResult> ValidateOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult<OrderDto>> ProcessOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult> ReviewOrder(string id, OrderReviewDto orderReview);
    Task<IEnumerable<OrderViewDto>> GetJudgeOrdersAsync(int judgeId);
    Task<OperationResult> SubmitOrder(string id);
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
    private readonly ICsoClient _csoClient;
    private readonly IUserService _userService;

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
        ICsoClient csoClient,
        IUserService userService
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
        _csoClient = csoClient;
        _userService = userService;
    }

    public async Task<OperationResult> ValidateOrderRequestAsync(OrderRequestDto dto)
    {
        var errors = new List<string>();

        // Validate file existence based on court class
        var fileId = dto.CourtFile.PhysicalFileId;
        if (!Enum.TryParse<CourtClassCd>(dto.CourtFile.CourtClassCd, true, out var courtClass))
        {
            errors.Add($"Invalid CourtClassCd: {dto.CourtFile.CourtClassCd}");
            return OperationResult<OrderDto>.Failure([.. errors]);
        }

        switch (courtClass)
        {
            case CourtClassCd.A or CourtClassCd.Y or CourtClassCd.T:
                var criminalFile = await _filesClient.FilesCriminalFilecontentAsync(
                    _requestAgencyIdentifierId,
                    _requestPartId,
                    _applicationCode,
                    null, null, null, null,
                    fileId.ToString());
                if (criminalFile == null || criminalFile.AccusedFile.Count == 0)
                {
                    errors.Add($"Criminal file with id: {fileId} is not found.");
                }
                break;

            case CourtClassCd.C or CourtClassCd.F or CourtClassCd.L or CourtClassCd.M:
                var civilFile = await _filesClient.FilesCivilFilecontentAsync(
                    _requestAgencyIdentifierId,
                    _requestPartId,
                    _applicationCode,
                    null, null, null, null,
                    fileId.ToString());
                if (civilFile == null || civilFile.CivilFile.Count == 0)
                {
                    errors.Add($"Civil file with id: {fileId} is not found.");
                }
                break;

            default:
                errors.Add($"Unsupported CourtClassCd: {courtClass}.");
                break;
        }

        // Validate judge existence
        var judges = await _judgeService.GetJudges();
        if (!judges.Any(j => j.PersonId == dto.Referral.SentToPartId))
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
            var fileId = dto.CourtFile.PhysicalFileId;
            var existingOrders = await this.Repo
                .FindAsync(o => o.OrderRequest.CourtFile.PhysicalFileId == fileId
                    && o.OrderRequest.Referral.SentToPartId == dto.Referral.SentToPartId
                    && o.OrderRequest.Referral.ReferredDocumentId == dto.Referral.ReferredDocumentId);

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

                var result = await this.UpdateAsync(orderDto);
                if (!result.Succeeded)
                {
                    return result;
                }
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

                var result = await this.AddAsync(orderDto);
                if (!result.Succeeded)
                {
                    return result;
                }

                if (orderDto.Status == OrderStatus.Approved || orderDto.Status == OrderStatus.Unapproved)
                {
                    _backgroundJobClient.Enqueue<SendOrderNotificationJob>(job => job.Execute(dto));
                }
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
        var order = await Repo.GetByIdAsync(id);

        if (order is null)
        {
            return OperationResult.Failure("Order not found");
        }

        var assignedJudgeId = order.OrderRequest.Referral.SentToPartId;
        var judgeId = _httpContextAccessor.HttpContext.User.JudgeId();

        if (assignedJudgeId != judgeId)
        {
            return OperationResult.Failure("Judge is not assigned to review this Order.");
        }
        var orderDto = Mapper.Map<OrderDto>(order);
        orderReview.Adapt(orderDto);

        var result = await UpdateAsync(orderDto);

        if (!result.Succeeded)
        {
            return result;
        }

        _backgroundJobClient.Enqueue<SubmitOrderJob>(job => job.Execute(id));

        return OperationResult.Success();
    }

    public override Task<OperationResult<OrderDto>> ValidateAsync(OrderDto dto, bool isEdit = false)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<OrderViewDto>> GetJudgeOrdersAsync(int judgeId)
    {
        var judgeOrders = await this.Repo.FindAsync(o => o.OrderRequest.Referral.SentToPartId == judgeId);
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

            var success = await _csoClient.SendOrderAsync(actionDto);
            if (!success)
            {
                this.Logger.LogWarning("Order {OrderId} submission to CSO failed.", id);
                orderDto.SubmitStatus = SubmitStatus.Error;
                var failedStatusResult = await UpdateAsync(orderDto);
                if (!failedStatusResult.Succeeded)
                {
                    return failedStatusResult;
                }

                return OperationResult.Failure("Failed to submit order to CSO.");
            }

            // Cleanup the successful, submitted order to remove potentially private document data and comments.
            orderDto.DocumentData = null;
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

    private async Task<OrderActionDto> MapToOrderAction(OrderDto orderDto)
    {
        var referral = orderDto.OrderRequest?.Referral;
        if (referral?.ReferredDocumentId == null)
        {
            this.Logger.LogError("Order {OrderId} is invalid and cannot be submitted.", orderDto.Id);
            return null;
        }

        var userGuid = _httpContextAccessor.HttpContext?.User?.ProvjudUserGuid();
        if (string.IsNullOrWhiteSpace(userGuid))
        {
            if (!referral.SentToPartId.HasValue)
            {
                this.Logger.LogError("Order {OrderId} is missing SentToPartId for submitter lookup.", orderDto.Id);
                return null;
            }

            var user = await _userService.GetByJudgeIdAsync(referral.SentToPartId.Value);
            userGuid = user?.NativeGuid;
        }

        if (string.IsNullOrWhiteSpace(userGuid))
        {
            this.Logger.LogError("Order {OrderId} is missing a submitter user guid.", orderDto.Id);
            return null;
        }

        var actionDto = Mapper.Map<OrderActionDto>(orderDto);
        actionDto.UserGuid = userGuid;
        actionDto.OrderTerms ??= Array.Empty<OrderTerm>();
        if (orderDto.Status == OrderStatus.Unapproved && orderDto.ProcessedDate.HasValue)
        {
            actionDto.RejectedDt = orderDto.ProcessedDate.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            actionDto.RejectedDt = null;
        }

        if (orderDto.Signed && orderDto.ProcessedDate.HasValue)
        {
            actionDto.SignedDt = orderDto.ProcessedDate.Value.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            actionDto.SignedDt = null;
        }

        return actionDto;
    }
}
