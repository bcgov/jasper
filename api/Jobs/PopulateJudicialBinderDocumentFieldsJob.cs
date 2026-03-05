using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Models;
using Scv.Api.Models.Binder;
using Scv.Api.Services;
using Scv.Api.Services.Files;
using Scv.Db.Contants;

namespace Scv.Api.Jobs;

/// <summary>
/// Manually-triggered Hangfire job that repopulates judicial binder documents with additional fields from CivilDocument.
/// This is a long-running job that processes all judicial binders.
/// </summary>
public class PopulateJudicialBinderDocumentFieldsJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<PopulateJudicialBinderDocumentFieldsJob> logger,
    IBinderService binderService,
    FilesService filesService
    ) : RecurringJobBase<PopulateJudicialBinderDocumentFieldsJob>(configuration, cache, mapper, logger)
{
    private readonly IBinderService _binderService = binderService;
    private readonly CivilFilesService _civilFilesService = filesService.Civil;

    public override string JobName => nameof(PopulateJudicialBinderDocumentFieldsJob);

    public override string CronSchedule => Cron.Never(); // Manual trigger only

    public override async Task Execute()
    {
        try
        {
            this.Logger.LogInformation("Starting PopulateJudicialBinderDocumentFieldsJob for all judicial binders.");

            // Search for all judicial binders (where JUDGE_ID exists in Labels)
            var searchCriteria = new SearchBindersCriteria
            {
                LabelKeysExist = [LabelConstants.JUDGE_ID],
                UpdatedBefore = DateTime.UtcNow.AddHours(-1), // Skip recently updated binders to avoid reprocessing
                Limit = null
            };

            var judicialBinders = await _binderService.SearchBinders(searchCriteria);
            var totalCount = judicialBinders.Count;

            if (totalCount == 0)
            {
                this.Logger.LogInformation("No judicial binders found. Job completed.");
                return;
            }

            var processedCount = 0;
            var errorCount = 0;
            var skippedCount = 0;

            foreach (var binder in judicialBinders)
            {
                try
                {
                    var result = await ProcessBinderAsync(binder);

                    if (result == ProcessResult.Success)
                    {
                        processedCount++;
                        this.Logger.LogInformation(
                            "Progress: {Current}/{Total} - Processed binder {BinderId} successfully.",
                            processedCount + errorCount + skippedCount,
                            totalCount,
                            binder.Id);
                    }
                    else if (result == ProcessResult.Skipped)
                    {
                        skippedCount++;
                        this.Logger.LogDebug(
                            "Progress: {Current}/{Total} - Skipped binder {BinderId}.",
                            processedCount + errorCount + skippedCount,
                            totalCount,
                            binder.Id);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    this.Logger.LogError(
                        ex,
                        "Progress: {Current}/{Total} - Error processing binder {BinderId}: {Message}",
                        processedCount + errorCount + skippedCount,
                        totalCount,
                        binder.Id,
                        ex.Message);
                }

                // Add a small delay every 10 binders to avoid overwhelming external services
                if ((processedCount + errorCount + skippedCount) % 10 == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            this.Logger.LogInformation(
                "PopulateJudicialBinderDocumentFieldsJob completed. Total: {Total}, Processed: {Processed}, Skipped: {Skipped}, Errors: {Errors}",
                totalCount,
                processedCount,
                skippedCount,
                errorCount);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Fatal error in PopulateJudicialBinderDocumentFieldsJob: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<ProcessResult> ProcessBinderAsync(BinderDto binder)
    {
        if (!binder.Labels.TryGetValue(LabelConstants.PHYSICAL_FILE_ID, out string fileId))
        {
            this.Logger.LogWarning("Binder {BinderId} has no physical file ID. Skipping.", binder.Id);
            return ProcessResult.Skipped;
        }

        var documentIds = binder.Documents.Select(d => d.DocumentId).ToList();

        if (documentIds.Count == 0)
        {
            this.Logger.LogDebug("Binder {BinderId} has no documents. Skipping.", binder.Id);
            return ProcessResult.Skipped;
        }

        var civilDocuments = await _civilFilesService.GetDocumentsByIds(fileId, documentIds);
        var enrichedDocuments = this.Mapper.Map<List<BinderDocumentDto>>(civilDocuments);
        binder.Documents = enrichedDocuments;

        var updateResult = await _binderService.InternalUpdateAsync(binder);

        if (!updateResult.Succeeded)
        {
            this.Logger.LogError(
                "Failed to update binder {BinderId}: {Errors}",
                binder.Id,
                string.Join(", ", updateResult.Errors));
            return ProcessResult.Error;
        }

        return ProcessResult.Success;
    }

    private enum ProcessResult
    {
        Success,
        Skipped,
        Error
    }
}