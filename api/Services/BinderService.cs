using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnsClient.Internal;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Scv.Api.Documents;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Api.Models.Document;
using Scv.Api.Processors;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IBinderService : ICrudService<BinderDto>
{
    Task<OperationResult<List<BinderDto>>> GetByLabels(Dictionary<string, string> labels);
    Task<OperationResult<DocumentBundleResponse>> CreateDocumentBundle(List<Dictionary<string, string>> contexts);
}

public class BinderService(
    IAppCache cache,
    IMapper mapper,
    ILogger<BinderService> logger,
    IRepositoryBase<Binder> binderRepo,
    IBinderFactory binderFactory,
    IDocumentMerger documentMerger) : CrudServiceBase<IRepositoryBase<Binder>, Binder, BinderDto>(
        cache,
        mapper,
        logger,
        binderRepo), IBinderService
{
    private readonly IBinderFactory _binderFactory = binderFactory;
    private readonly IDocumentMerger _documentMerger = documentMerger;

    public override string CacheName => nameof(BinderService);

    public async Task<OperationResult<List<BinderDto>>> GetByLabels(Dictionary<string, string> labels)
    {
        var binderProcessor = _binderFactory.Create(labels);

        var processorValidation = await binderProcessor.ValidateAsync();
        if (!processorValidation.Succeeded)
        {
            return OperationResult<List<BinderDto>>
                .Failure([.. processorValidation.Errors]);
        }

        await binderProcessor.PreProcessAsync();

        var filterBuilder = Builders<Binder>.Filter;
        var filter = FilterDefinition<Binder>.Empty;

        foreach (var label in binderProcessor.Binder.Labels)
        {
            var key = $"Labels.{label.Key}";
            filter &= filterBuilder.Eq(key, label.Value);
        }

        var entities = await this.Repo.FindAsync(CollectionNameConstants.BINDERS, filter);

        var data = this.Mapper.Map<List<BinderDto>>(entities);

        return OperationResult<List<BinderDto>>.Success(data);
    }

    public override async Task<OperationResult<BinderDto>> AddAsync(BinderDto dto)
    {
        var binderProcessor = _binderFactory.Create(dto);

        await binderProcessor.PreProcessAsync();

        var processorValidation = await binderProcessor.ValidateAsync();
        if (!processorValidation.Succeeded)
        {
            return OperationResult<BinderDto>.Failure([.. processorValidation.Errors]);
        }

        return await base.AddAsync(binderProcessor.Binder);
    }

    public override async Task<OperationResult<BinderDto>> UpdateAsync(BinderDto dto)
    {
        var binderProcessor = _binderFactory.Create(dto);

        var processorValidation = await binderProcessor.ValidateAsync();
        if (!processorValidation.Succeeded)
        {
            return OperationResult<BinderDto>.Failure([.. processorValidation.Errors]);
        }

        // Since business rules passed, prep binder
        await binderProcessor.PreProcessAsync();

        return await base.UpdateAsync(binderProcessor.Binder);
    }

    public override async Task<OperationResult> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return OperationResult.Failure("Invalid ID.");
        }

        var binderToDelete = await base.GetByIdAsync(id);
        var binderProcessor = _binderFactory.Create(binderToDelete);

        // Ensure that current user can only delete his own binder
        var processorValidation = await binderProcessor.ValidateAsync();
        if (!processorValidation.Succeeded)
        {
            return OperationResult<BinderDto>.Failure([.. processorValidation.Errors]);
        }

        return await base.DeleteAsync(id);
    }

    public override Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto, bool isEdit = false)
    {
        throw new NotImplementedException("Binder validations are executed via BinderProcessors");
    }

    public async Task<OperationResult<DocumentBundleResponse>> CreateDocumentBundle(List<Dictionary<string, string>> contexts)
    {
        var correlationId = Guid.NewGuid();
        this.Logger.LogInformation("Starting document bundling/merging process. CorrelationId: {CorreclationId}", correlationId);

        try
        {
            var binders = await InitializeBinders(contexts);
            if (binders.Count == 0)
            {
                this.Logger.LogWarning("Something went wrong while initializing the binders. CorrelationId: {CorreclationId}", correlationId);
                return OperationResult<DocumentBundleResponse>.Failure("No binders to process.");
            }

            var requests = GeneratePdfDocumentRequests(binders, correlationId);
            if (requests.Length == 0)
            {
                this.Logger.LogWarning("No binders to merge. CorrelationId: {CorreclationId}", correlationId);
                return OperationResult<DocumentBundleResponse>.Failure("No documents found to merge.");
            }

            var response = await _documentMerger.MergeDocuments(requests);

            return OperationResult<DocumentBundleResponse>.Success(new DocumentBundleResponse
            {
                Binders = binders,
                PdfResponse = response
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Something went wrong during the document bundling/merging process: {Error}", ex.Message);
            return OperationResult<DocumentBundleResponse>.Failure("Something went wrong during the document bundling/merging process.");
        }
    }

    #region Helpers

    private async Task<List<BinderDto>> InitializeBinders(List<Dictionary<string, string>> contexts)
    {
        var binders = new List<BinderDto>();
        foreach (var context in contexts)
        {
            // See if binder(s) already exists for the given context
            var bindersResult = await this.GetByLabels(context);
            if (!bindersResult.Succeeded)
            {
                this.Logger.LogWarning("Failed to get binder(s) for the current context: {Error}", string.Join(",", bindersResult.Errors));
                continue;
            }

            if (bindersResult.Payload.Count == 0)
            {
                var binder = await ProcessNewBinder(context);
                if (binder != null)
                {
                    binders.Add(binder);
                }
            }
            else
            {
                binders.AddRange(await this.ProcessExistingBinders(bindersResult.Payload));
            }
        }
        return binders;
    }

    private async Task<BinderDto> ProcessNewBinder(Dictionary<string, string> context)
    {
        var processor = _binderFactory.Create(context);

        var result = await processor.ProcessAsync();
        if (!result.Succeeded)
        {
            this.Logger.LogWarning("Failed to process binder for the current context: {Error}", string.Join(",", result.Errors));
            return null;
        }

        var newBinderResult = await this.AddAsync(processor.Binder);
        if (!newBinderResult.Succeeded)
        {
            this.Logger.LogWarning("Failed to create binder for the current context: {Error}", string.Join(",", newBinderResult.Errors));
            return null;
        }

        return newBinderResult.Payload;
    }

    private async Task<List<BinderDto>> ProcessExistingBinders(List<BinderDto> existingBinders)
    {
        var binders = new List<BinderDto>();

        foreach (var binder in existingBinders)
        {
            var processor = _binderFactory.Create(binder);

            var result = await processor.ProcessAsync();
            if (!result.Succeeded)
            {
                this.Logger.LogWarning("Failed to process binder:{Id} - {Error}", binder.Id, string.Join(",", result.Errors));
                continue;
            }

            // Determine if the binder has changed
            if (JsonConvert.SerializeObject(binder) != JsonConvert.SerializeObject(processor.Binder))
            {
                this.Logger.LogInformation("Updating binder:{Id}", binder.Id);
                var updateResult = await this.UpdateAsync(processor.Binder);
                binders.Add(updateResult.Payload);
            }
            else
            {
                binders.Add(processor.Binder);
            }
        }

        return binders;
    }

    private static PdfDocumentRequest[] GeneratePdfDocumentRequests(List<BinderDto> binders, Guid correlationId)
    {
        var bundleRequests = new List<PdfDocumentRequest>();
        foreach (var binder in binders)
        {
            var binderDocRequests = binder.Documents
                // Excludes DocumentType.File documents where the FileName = DocumentId.
                // This means that there is no document to view.
                .Where(d => d.DocumentType != DocumentType.File || d.DocumentId != null)
                .Select(d => new PdfDocumentRequest
                {
                    Type = d.DocumentType,
                    Data = new PdfDocumentRequestDetails
                    {
                        PartId = binder.Labels.GetValue(LabelConstants.PARTICIPANT_ID),
                        ProfSeqNo = binder.Labels.GetValue(LabelConstants.PROF_SEQ_NUMBER),
                        CourtLevelCd = binder.Labels.GetValue(LabelConstants.COURT_LEVEL_CD),
                        CourtClassCd = binder.Labels.GetValue(LabelConstants.COURT_CLASS_CD),
                        FileId = binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID),
                        AppearanceId = binder.Labels.GetValue(LabelConstants.APPEARANCE_ID),
                        IsCriminal = true,
                        CorrelationId = correlationId.ToString(),
                        DocumentId = d.DocumentType == DocumentType.File
                            ? WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(d.DocumentId))
                            : d.DocumentId
                    }
                });
            bundleRequests.AddRange(binderDocRequests);
        }
        return [.. bundleRequests];
    }

    #endregion Helpers
}
