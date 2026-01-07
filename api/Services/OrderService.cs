using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Order;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IOrderService : ICrudService<OrderDto>
{
    Task<OperationResult<OrderDto>> UpsertAsync(OrderDto dto);
}

public class OrderService : CrudServiceBase<IRepositoryBase<Order>, Order, OrderDto>, IOrderService
{
    private readonly FileServicesClient _filesClient;
    private readonly IConfiguration _configuration;
    private readonly IDashboardService _dashboardService;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;

    public override string CacheName => "GetOrdersAsync";

    public OrderService(
        IAppCache cache,
        IMapper mapper,
        ILogger<OrderService> logger,
        IRepositoryBase<Order> orderRepo,
        FileServicesClient filesClient,
        IConfiguration configuration,
        IDashboardService dashboardService
    ) : base(
            cache,
            mapper,
            logger,
            orderRepo)
    {
        _configuration = configuration;
        _dashboardService = dashboardService;
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
        _requestPartId = configuration.GetNonEmptyValue("Request:PartId");
    }

    public override async Task<OperationResult<OrderDto>> ValidateAsync(OrderDto dto, bool isEdit = false)
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

        if (errors.Count > 0)
        {
            return OperationResult<OrderDto>.Failure([.. errors]);
        }

        // Validate judge existence
        var judges = await _dashboardService.GetJudges();
        if (!judges.Any(j => j.PersonId == dto.Referral.SentToPartId))
        {
            errors.Add($"Judge with id: {dto.Referral.SentToPartId} is not found.");
            return OperationResult<OrderDto>.Failure([.. errors]);
        }

        return OperationResult<OrderDto>.Success(dto);
    }

    public async Task<OperationResult<OrderDto>> UpsertAsync(OrderDto dto)
    {
        // Determine if the order already exists. If it is, this is an edit request. Otherwise, create a new one.
        var fileId = dto.CourtFile.PhysicalFileId;
        var existingOrders = await this.Repo
            .FindAsync(o => o.CourtFile.PhysicalFileId == fileId
                && o.Referral.SentToPartId == dto.Referral.SentToPartId
                && o.Referral.ReferredDocumentId == dto.Referral.ReferredDocumentId);

        var existingOrder = existingOrders?.FirstOrDefault();

        if (existingOrder != null)
        {
            this.Logger.LogInformation("Updating existing order for fileId: {fileId}, sentToPartId: {sentToPartId}, referredDocumentId: {referredDocumentId} ",
                fileId, dto.Referral.SentToPartId, dto.Referral.ReferredDocumentId);
            return await this.UpdateAsync(dto);
        }
        else
        {
            this.Logger.LogInformation("Creating new order for fileId: {fileId}, sentToPartId: {sentToPartId}, referredDocumentId: {referredDocumentId} ",
                fileId, dto.Referral.SentToPartId, dto.Referral.ReferredDocumentId);
            return await this.AddAsync(dto);
        }
    }
}
