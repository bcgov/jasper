using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public class JudicialBinderProcessor : BinderProcessorBase
{
    private readonly FileServicesClient _filesClient;

    public JudicialBinderProcessor(FileServicesClient filesClient, ClaimsPrincipal currentUser) : base(currentUser)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
    }

    public override async Task PreProcessAsync(BinderDto dto)
    {
        await base.PreProcessAsync(dto);

        var fileId = dto.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var fileDetail = await _filesClient.FilesCivilGetAsync(
            this.CurrentUser.AgencyCode(),
            this.CurrentUser.ParticipantId(),
            this.CurrentUser.ApplicationCode(),
            fileId);

        // Add labels specific to Judicial Binder
        dto.Labels.Add(LabelConstants.COURT_CLASS_CD, fileDetail.CourtClassCd.ToString());
    }

    public override async Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto)
    {
        var errors = new List<string>();

        var result = await base.ValidateAsync(dto);
        if (!result.Succeeded)
        {
            return result;
        }


        var fileId = dto.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var fileDetail = await _filesClient.FilesCivilGetAsync(
            this.CurrentUser.AgencyCode(),
            this.CurrentUser.ParticipantId(),
            this.CurrentUser.ApplicationCode(),
            fileId);

        var courtSummaryIds = fileDetail.Appearance.Select(a => a.AppearanceId);
        var civilDocIds = fileDetail.Document.Select(d => d.CivilDocumentId);

        // Validate that all document ids from Dto exist in Civil Case Detail documents
        var docIdsFromDto = dto.Documents.Select(d => d.DocumentId);
        if (!docIdsFromDto.All(id => courtSummaryIds.Concat(civilDocIds).Contains(id)))
        {
            errors.Add("Found one or more invalid Document IDs.");
        }

        return errors.Count != 0
            ? OperationResult<BinderDto>.Failure([.. errors])
            : OperationResult<BinderDto>.Success(dto);
    }
}
