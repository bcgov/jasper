using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Documents;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Documents;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public class KeyDocumentsBinderProcessor : BinderProcessorBase
{
    private readonly FileServicesClient _filesClient;
    private readonly IAppCache _cache;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IDocumentConverter _documentConverter;
    private readonly IConfiguration _configuration;

    public KeyDocumentsBinderProcessor(
        FileServicesClient filesClient,
        ClaimsPrincipal currentUser,
        IValidator<BinderDto> basicValidator,
        BinderDto dto,
        IAppCache cache,
        ILogger logger,
        IConfiguration configuration,
        IDocumentConverter documentConverter,
        IMapper mapper) : base(currentUser, dto, basicValidator)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _documentConverter = documentConverter;
        _mapper = mapper;
    }

    public override Task PreProcessAsync()
    {
        // Key Documents Binder are generated in the backend so we have full control
        // which data are saved. Overriding this method so Labels aren't cleared.
        return Task.CompletedTask;
    }

    public override async Task<OperationResult> ValidateAsync()
    {
        var result = await base.ValidateAsync();
        if (!result.Succeeded)
        {
            return result;
        }

        var errors = new List<string>();

        var requiredKeys = new[]
        {
            LabelConstants.PARTICIPANT_ID,
            LabelConstants.COURT_CLASS_CD,
            LabelConstants.APPEARANCE_ID,
            LabelConstants.PHYSICAL_FILE_ID
        };

        var labels = this.Binder.Labels ?? [];

        errors.AddRange(requiredKeys
            .Where(key => !labels.ContainsKey(key))
            .Select(key => $"Missing label: {key}"));

        return errors.Count != 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }

    public override async Task<OperationResult> ProcessAsync()
    {
        if (this.Binder.Id == null)
        {
            var documents = await GetDocuments();
            this.Binder.Documents = documents;
            return OperationResult.Success();
        }

        // For existing binders, check if we need to refresh the documents
        var refreshHoursConfig = _configuration.GetNonEmptyValue("KEY_DOCS_BINDER_REFRESH_HOURS");
        int.TryParse(refreshHoursConfig, out int refreshHours);

        var age = DateTime.UtcNow - this.Binder.UpdatedDate.GetValueOrDefault();
        if (age.TotalHours >= refreshHours)
        {
            this.Binder.Documents = await GetDocuments();
        }
        return OperationResult.Success();
    }

    private async Task<List<BinderDocumentDto>> GetDocuments()
    {
        var fileId = this.Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var participantId = this.Binder.Labels.GetValue(LabelConstants.PARTICIPANT_ID);

        async Task<CriminalFileContent> FileContent() => await _filesClient.FilesCriminalFilecontentAsync(
                this.CurrentUser.AgencyCode(),
                this.CurrentUser.ParticipantId(),
                _configuration.GetNonEmptyValue("Request:ApplicationCd"),
                null,
                null,
                null,
                null,
                fileId);
        var fileContentTask = _cache.GetOrAddAsync($"CriminalFileContent-{fileId}-{this.CurrentUser.AgencyCode()}", FileContent);
        var fileContent = await fileContentTask;

        var accusedFile = fileContent?.AccusedFile.FirstOrDefault(af => af.MdocJustinNo == fileId && af.PartId == participantId);
        if (accusedFile == null)
        {
            _logger.LogWarning("No accused file found for fileId {FileId} and participantId {ParticipantId}.", fileId, participantId);
            return [];
        }

        // Prepare Key Documents
        var allDocuments = await _documentConverter.GetCriminalDocuments(accusedFile);
        var keyDocuments = _mapper.Map<List<BinderDocumentDto>>(KeyDocumentResolver.GetCriminalKeyDocuments(allDocuments));

        this.Binder.Labels.TryAdd(LabelConstants.PROF_SEQ_NUMBER, accusedFile.ProfSeqNo);
        this.Binder.Labels.TryAdd(LabelConstants.COURT_LEVEL_CD, accusedFile.CourtLevelCd);
        this.Binder.Labels.TryAdd(LabelConstants.COURT_CLASS_CD, accusedFile.CourtClassCd);

        return keyDocuments;
    }

}
