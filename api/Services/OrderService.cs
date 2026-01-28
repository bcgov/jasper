using System;
using System.Collections.Generic;
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

namespace Scv.Api.Services;

public interface IOrderService : ICrudService<OrderDto>
{
    Task<OperationResult> ValidateOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult<OrderDto>> ProcessOrderRequestAsync(OrderRequestDto dto);
    Task<OperationResult> ReviewOrder(string id, OrderReviewDto orderReview);
    Task<IEnumerable<OrderViewDto>> GetJudgeOrdersAsync(int judgeId);
}

public class OrderService : CrudServiceBase<IRepositoryBase<Order>, Order, OrderDto>, IOrderService
{
    private readonly FileServicesClient _filesClient;
    private readonly IDashboardService _dashboardService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public override string CacheName => "GetOrdersAsync";

    public OrderService(
        IAppCache cache,
        IMapper mapper,
        ILogger<OrderService> logger,
        IRepositoryBase<Order> orderRepo,
        FileServicesClient filesClient,
        IConfiguration configuration,
        IDashboardService dashboardService,
        IBackgroundJobClient backgroundJobClient,
        IHttpContextAccessor httpContextAccessor
    ) : base(
            cache,
            mapper,
            logger,
            orderRepo)
    {
        _dashboardService = dashboardService;
        _filesClient = filesClient;
        _backgroundJobClient = backgroundJobClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
        _requestPartId = configuration.GetNonEmptyValue("Request:PartId");
        _httpContextAccessor = httpContextAccessor;
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
        var judges = await _dashboardService.GetJudges();
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
                };

                var result = await this.AddAsync(orderDto);
                if (!result.Succeeded)
                {
                    return result;
                }

                _backgroundJobClient.Enqueue<SendOrderNotificationJob>(job => job.Execute(dto));
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
}
