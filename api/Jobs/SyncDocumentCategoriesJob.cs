using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Services;

namespace Scv.Api.Jobs;

public class SyncDocumentCategoriesJob(
    ILogger<SyncDocumentCategoriesJob> logger,
    IDocumentCategoryService service,
    IConfiguration configuration) : IRecurringJob
{
    private readonly ILogger<SyncDocumentCategoriesJob> _logger = logger;
    private readonly IDocumentCategoryService _service = service;
    private readonly IConfiguration _configuration = configuration;

    public const string DEFAULT_SCHEDULE = "0 7 * * *"; // 7AM UTC / 12AM PST

    public string JobName => nameof(SyncDocumentCategoriesJob);

    public string CronSchedule => _configuration.GetValue<string>("JobSchedule:SyncDocumentCategories") ?? DEFAULT_SCHEDULE;

    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting to sync document categories.");

            await _service.RefreshDocumentCategoriesAsync();

            _logger.LogInformation("Document categories has been synced successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error occured while syncing the document categories: {Message}", ex.Message);
        }
    }
}
