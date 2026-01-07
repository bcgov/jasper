using FluentValidation;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using MapsterMapper;
using Scv.Api.Documents;
using Scv.Api.Helpers;
using Scv.Api.Services;
using LazyCache;
using System;
using Scv.Db.Models;
using Scv.Core.Helpers.ContractResolver;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Contants;
using Scv.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Processors;

public class JudicialBinderProcessor : BinderProcessorBase
{
    private readonly FileServicesClient _filesClient;
    private readonly IAppCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IDarsService _darsService;
    private readonly IMapper _mapper;

    public JudicialBinderProcessor(
        FileServicesClient filesClient,
        ClaimsPrincipal currentUser,
        IValidator<BinderDto> basicValidator,
        BinderDto dto,
        IAppCache cache,
        IMapper mapper,
        IConfiguration configuration,
        IDarsService darsService) : base(currentUser, dto, basicValidator)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _configuration = configuration;
        _darsService = darsService;
        _cache = cache;
        _mapper = mapper;
    }

    public override async Task PreProcessAsync()
    {
        await base.PreProcessAsync();

        var fileId = this.Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var fileDetail = await _filesClient.FilesCivilGetAsync(
            this.CurrentUser.AgencyCode(),
            this.CurrentUser.ParticipantId(),
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            fileId);

        // Add labels specific to Judicial Binder
        Binder.Labels.Add(LabelConstants.COURT_CLASS_CD, fileDetail.CourtClassCd.ToString());
        Binder.Labels.Add(LabelConstants.JUDGE_ID, this.CurrentUser.UserId());
        Binder.Labels.Add(LabelConstants.IS_CRIMINAL, "false");
    }

    public override async Task<OperationResult> ProcessAsync()
    {
        if (this.Binder.Id != null)
        {
            var documents = await GetDocuments();
            Binder.Documents = documents;
            return OperationResult.Success();
        }
        return OperationResult.Failure("Binder does not exist.");
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
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            fileId);

        var courtSummaryIds = fileDetail.Appearance.Select(a => a.AppearanceId);
        var civilDocIds = fileDetail.Document.Select(d => d.CivilDocumentId);
        var referenceDocIds = fileDetail.ReferenceDocument.Select(r => r.ReferenceDocumentId);

        var transcriptDocs = this.Binder.Documents
            .Where(d => d.DocumentType == DocumentType.Transcript)
            .ToList();
        if (transcriptDocs.Count > 0)
        {
            try
            {
                var transcriptsResponse = await _darsService.GetCompletedDocuments(
                    physicalFileId: fileId,
                    mdocJustinNo: null,
                    returnChildRecords: true);

                if (transcriptsResponse == null)
                {
                    errors.Add("Unable to retrieve transcripts for validation.");
                }
                else
                {
                    var completedTranscripts = transcriptsResponse.ToList();

                    foreach (var transcriptDoc in transcriptDocs)
                    {
                        if (string.IsNullOrWhiteSpace(transcriptDoc.OrderId))
                        {
                            errors.Add($"Transcript document {transcriptDoc.DocumentId} is missing OrderId.");
                            continue;
                        }

                        if (!int.TryParse(transcriptDoc.OrderId, out var orderId))
                        {
                            errors.Add($"Invalid OrderId format for transcript document {transcriptDoc.DocumentId}.");
                            continue;
                        }

                        if (!int.TryParse(transcriptDoc.DocumentId, out var transcriptId))
                        {
                            errors.Add($"Invalid DocumentId format for transcript {transcriptDoc.DocumentId}.");
                            continue;
                        }

                        var matchingTranscript = completedTranscripts.FirstOrDefault(t =>
                            t.OrderId == orderId && t.Id == transcriptId);

                        if (matchingTranscript == null)
                        {
                            errors.Add($"Transcript with OrderId {orderId} and DocumentId {transcriptId} not found in completed transcripts.");
                        }
                    }
                }
            }
            catch (DARSCommon.Clients.TranscriptsServices.ApiException ex)
            {
                errors.Add($"Error validating transcripts: {ex.Message}");
            }
        }

        // Get non-transcript document IDs
        var nonTranscriptDocIds = this.Binder.Documents
            .Where(d => d.DocumentType != DocumentType.Transcript)
            .Select(d => d.DocumentId);

        // Validate that all non-transcript document ids exist in Civil Case Detail documents or reference documents
        if (nonTranscriptDocIds.Any() &&
            !nonTranscriptDocIds.All(id => courtSummaryIds.Concat(civilDocIds).Concat(referenceDocIds).Contains(id)))
        {
            errors.Add("Found one or more invalid Document IDs.");
        }

        return errors.Count != 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }

    private async Task<List<BinderDocumentDto>> GetDocuments()
    {
        var fileId = Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
        var agencyCode = CurrentUser.AgencyCode();
        var participantId = CurrentUser.ParticipantId();
        var applicationCd = _configuration.GetNonEmptyValue("Request:ApplicationCd");

        // Fetch file details and content in parallel
        var fileDetails = _cache.GetOrAddAsync(
            $"CivilFileDetail-{fileId}-{agencyCode}-{participantId}-{applicationCd}",
            () => _filesClient.FilesCivilGetAsync(agencyCode, participantId, applicationCd, fileId));
        var fileContent = _cache.GetOrAddAsync(
            $"CivilFileContent-{fileId}-{agencyCode}-{participantId}-{applicationCd}",
            () => _filesClient.FilesCivilFilecontentAsync(agencyCode, participantId, applicationCd, null, null, null, null, fileId));

        await Task.WhenAll(fileDetails, fileContent);

        var existingBinderDocs = Binder.Documents ?? [];
        var existingDocIds = existingBinderDocs.Select(doc => doc.DocumentId).ToHashSet();
        var csrDocs = PopulateDetailCsrsDocuments([.. (fileDetails.Result.Appearance ?? []).Where(a => existingDocIds.Contains(a.AppearanceId))]);
        var referenceDocs = PopulateDetailReferenceDocuments([.. (fileDetails.Result.ReferenceDocument ?? []).Where(rd => existingDocIds.Contains(rd.ReferenceDocumentId))]);
        var civilDocs = (fileContent.Result.CivilFile ?? [])
            .SelectMany(cf => cf.Document ?? [])
            .Where(doc => existingDocIds.Contains(doc.DocumentId));

        // Map and preserve order
        var mappedDocuments = _mapper.Map<List<BinderDocumentDto>>(civilDocs.Concat(csrDocs).Concat(referenceDocs));
        foreach (var doc in mappedDocuments)
        {
            var existingDoc = existingBinderDocs.FirstOrDefault(ed => ed.DocumentId == doc.DocumentId);
            if (existingDoc != null)
            {
                doc.Order = existingDoc.Order;
            }
        }

        return [.. mappedDocuments.OrderBy(d => d.Order)];
    }

    private static IEnumerable<CvfcDocument> PopulateDetailReferenceDocuments(ICollection<CvfcRefDocument3> referenceDocuments)
    {
        //Add in Reference Documents.
        return referenceDocuments.Select(referenceDocument => new CvfcDocument()
        {
            DocumentTypeCd = DocumentCategory.LITIGANT,
            DocumentTypeDescription = "Litigant Document",
            DocumentId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(referenceDocument.ObjectGuid)),
            ImageId = referenceDocument.ObjectGuid,
        });
    }

    private static IEnumerable<CvfcDocument> PopulateDetailCsrsDocuments(ICollection<CvfcAppearance> appearances)
    {
        //Add in CSRs.
        return appearances.Select(appearance => new CvfcDocument()
        {
            DocumentTypeCd = DocumentCategory.CSR,
            DocumentTypeDescription = "Court Summary",
            DocumentId = appearance.AppearanceId,
            ImageId = appearance.AppearanceId,
        });
    }
}
