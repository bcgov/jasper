using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Api.Models.Civil.Detail;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public class JudicialBinderService : BinderService
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;

    public JudicialBinderService(
        IAppCache cache,
        IMapper mapper,
        ILogger<JudicialBinderService> logger,
        IRepositoryBase<Binder> binderRepo,
        FileServicesClient filesClient,
        ClaimsPrincipal currentUser
    ) : base(cache, mapper, logger, binderRepo)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _currentUser = currentUser;
    }

    public override async Task OnBeforeBinderSaveAsync(BinderDto dto)
    {
        if (!dto.Labels.TryGetValue(LabelConstants.PHYSICAL_FILE_ID, out var fileId))
        {
            this.Logger.LogError("Case File Id not found.");
            return;
        }

        var fileDetail = await _filesClient.FilesCivilGetAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _currentUser.ApplicationCode(),
            fileId);

        await base.OnBeforeBinderSaveAsync(dto);

        // Add system-generated labels for judicial binder
        dto.Labels.Add(LabelConstants.PHYSICAL_FILE_ID, fileId);
        dto.Labels.Add(LabelConstants.JUDGE_ID, _currentUser.UserId());
        dto.Labels.Add(LabelConstants.COURT_CLASS_CD, fileDetail.CourtClassCd.ToString());
    }

    public override async Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        if (isEdit && await this.Repo.GetByIdAsync(dto.Id) == null)
        {
            errors.Add("Binder ID is not found.");
        }

        if (!dto.Labels.TryGetValue(LabelConstants.PHYSICAL_FILE_ID, out var fileId))
        {
            this.Logger.LogError("Case File Id not found.");
            return OperationResult<BinderDto>.Failure([.. errors]);
        }

        var fileDetail = await _filesClient.FilesCivilGetAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _currentUser.ApplicationCode(),
            fileId);

        var detail = this.Mapper.Map<RedactedCivilFileDetailResponse>(fileDetail);

        // This pattern is based from CivilFilesService.PopulateDetailCsrsDocuments that
        // is called when Civil File Detail is retrieved and these Court Summaries are added as 'Document'
        var courtSummaryIds = fileDetail.Appearance.Select(a => a.AppearanceId);
        var civilDocIds = detail.Document.Select(d => d.CivilDocumentId);

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
