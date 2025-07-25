using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PCSSCommon.Clients.ConfigurationServices;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Jobs;

public class SyncDocumentCategoriesJob(
    IConfiguration configuration,
    IAppCache cache,
    IMapper mapper,
    ILogger<SyncDocumentCategoriesJob> logger,
    ConfigurationServicesClient configClient,
    IRepositoryBase<DocumentCategory> dcRepo)
    : IRecurringJob
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IAppCache _cache = cache;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<SyncDocumentCategoriesJob> _logger = logger;
    private readonly ConfigurationServicesClient _configClient = configClient;
    private readonly IRepositoryBase<DocumentCategory> _dcRepo = dcRepo;

    public const string DEFAULT_SCHEDULE = "0 7 * * *"; // 7AM UTC / 12AM PST

    public string JobName => nameof(SyncDocumentCategoriesJob);

    public string CronSchedule => _configuration.GetValue<string>("JobSchedule:SyncDocumentCategories") ?? DEFAULT_SCHEDULE;

    public async Task Execute()
    {
        try
        {
            _logger.LogInformation("Starting to sync document categories.");

            // Pulled this logic out of DocumentCategoryService because it depends on IRepositoryBase,
            // which requires MongoDB credentials. That complicates conditional registration
            // during startup. Once MongoDB is properly set up, this should go back into
            // DocumentCategoryService so all related transactions live in one place.

            var configData = await _cache.GetOrAddAsync(
                "ExternalConfig",
                async () => await _configClient.GetAllAsync());

            var externalDocumentCategories = configData
                .Where(c => DocumentCategory.ALL_DOCUMENT_CATEGORIES.Contains(c.Key));

            var categories = _mapper.Map<List<DocumentCategory>>(externalDocumentCategories);

            foreach (var category in categories)
            {
                var categoryEntity = (await _dcRepo.FindAsync(dc => dc.Name == category.Name)).FirstOrDefault();
                if (categoryEntity == null)
                {
                    await _dcRepo.AddAsync(category);
                    _logger.LogInformation("{Name} category added.", category.Name);
                    continue;
                }

                // Update the document category if there is only a mismatch
                if (categoryEntity.Value != category.Value)
                {
                    category.Adapt(categoryEntity);
                    await _dcRepo.UpdateAsync(categoryEntity);
                    _logger.LogInformation("{Name} category updated.", category.Name);
                }
            }

            _logger.LogInformation("Document categories has been synced successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while syncing the document categories.");
            throw;
        }
    }
}
