using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
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

    public JudicialBinderProcessor(
        FileServicesClient filesClient,
        ClaimsPrincipal currentUser,
        IValidator<BinderDto> basicValidator,
        BinderDto dto) : base(currentUser, dto, basicValidator)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
    }

    public override async Task PreProcessAsync()
    {
        await base.PreProcessAsync();

        var fileId = this.Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var fileDetail = await _filesClient.FilesCivilGetAsync(
            this.CurrentUser.AgencyCode(),
            this.CurrentUser.ParticipantId(),
            this.CurrentUser.ApplicationCode(),
            fileId);

        // Add labels specific to Judicial Binder
        this.Binder.Labels.Add(LabelConstants.COURT_CLASS_CD, fileDetail.CourtClassCd.ToString());
        this.Binder.Labels.Add(LabelConstants.JUDGE_ID, this.CurrentUser.UserId());
    }

    public override async Task<OperationResult> ValidateAsync()
    {
        var result = await base.ValidateAsync();
        if (!result.Succeeded)
        {
            return result;
        }

        var errors = new List<string>();

        // Validate current user is accessing own binder
        var judgeId = this.Binder.Labels.GetValue(LabelConstants.JUDGE_ID);
        if (judgeId != this.CurrentUser.UserId())
        {
            errors.Add("Current user does not have access to this binder.");
            return OperationResult.Failure([.. errors]);
        }

        var fileId = this.Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var fileDetail = await _filesClient.FilesCivilGetAsync(
            this.CurrentUser.AgencyCode(),
            this.CurrentUser.ParticipantId(),
            this.CurrentUser.ApplicationCode(),
            fileId);

        var courtSummaryIds = fileDetail.Appearance.Select(a => a.AppearanceId);
        var civilDocIds = fileDetail.Document.Select(d => d.CivilDocumentId);

        // Validate that all document ids from Dto exist in Civil Case Detail documents
        var docIdsFromDto = this.Binder.Documents.Select(d => d.DocumentId);
        if (!docIdsFromDto.All(id => courtSummaryIds.Concat(civilDocIds).Contains(id)))
        {
            errors.Add("Found one or more invalid Document IDs.");
        }

        return errors.Count != 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }
}
