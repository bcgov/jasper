using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Core.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Core.Services;

namespace Scv.Api.Services;

public interface ICourtFileService
{
    Task<OperationResult> ValidateFileExistsAsync(string courtClassCd, string fileId);
    Task<CriminalFileDetailResponse> GetCriminalFileAsync(string fileId);
    Task<CivilFileDetailResponse> GetCivilFileAsync(string fileId);
}

public class CourtFileService : ServiceBase, ICourtFileService
{
    private readonly FileServicesClient _filesClient;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;

    public override string CacheName => nameof(CourtFileService);

    public CourtFileService(
        IAppCache cache,
        FileServicesClient filesClient,
        IConfiguration configuration) : base(cache)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
        _requestPartId = configuration.GetNonEmptyValue("Request:PartId");
    }

    public static bool IsCriminalCourtClass(CourtClassCd courtClass) =>
        courtClass is CourtClassCd.A or CourtClassCd.Y or CourtClassCd.T;

    public static bool IsCivilCourtClass(CourtClassCd courtClass) =>
        courtClass is CourtClassCd.C or CourtClassCd.F or CourtClassCd.L or CourtClassCd.M;

    public async Task<OperationResult> ValidateFileExistsAsync(string courtClassCd, string fileId)
    {
        var errors = new List<string>();

        if (!Enum.TryParse<CourtClassCd>(courtClassCd, true, out var courtClass))
        {
            errors.Add($"Invalid CourtClassCd: {courtClassCd}");
        }
        else if (IsCriminalCourtClass(courtClass))
        {
            CriminalFileDetailResponse criminalFile;
            try
            {
                criminalFile = await GetCriminalFileAsync(fileId);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return OperationResult.Failure($"Criminal file with id: {fileId} is not found.");
            }
            if (criminalFile == null)
            {
                errors.Add($"Criminal file with id: {fileId} is not found.");
            }
        }
        else if (IsCivilCourtClass(courtClass))
        {
            CivilFileDetailResponse civilFile;
            try
            {
                civilFile = await GetCivilFileAsync(fileId);
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return OperationResult.Failure($"Civil file with id: {fileId} is not found.");
            }
            if (civilFile == null || string.IsNullOrWhiteSpace(civilFile.PhysicalFileId))
            {
                errors.Add($"Civil file with id: {fileId} is not found.");
            }
        }
        else
        {
            errors.Add($"Unsupported CourtClassCd: {courtClass}.");
        }

        return errors.Count > 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }

    public Task<CriminalFileDetailResponse> GetCriminalFileAsync(string fileId) =>
        GetDataFromCache(
            $"{CacheName}-CriminalFile-{_requestAgencyIdentifierId}-{_requestPartId}-{fileId}",
            () => _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId));

    public Task<CivilFileDetailResponse> GetCivilFileAsync(string fileId) =>
        GetDataFromCache(
            $"{CacheName}-CivilFile-{_requestAgencyIdentifierId}-{_requestPartId}-{fileId}",
            () => _filesClient.FilesCivilGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId));
}
