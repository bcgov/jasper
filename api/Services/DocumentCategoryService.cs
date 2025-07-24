using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Microsoft.Extensions.Logging;
using PCSSCommon.Clients.ConfigurationServices;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IDocumentCategoryService
{
    Task<IEnumerable<DocumentCategory>> GetAllAsync();
    Task RefreshDocumentCategoriesAsync();
}

public class DocumentCategoryService(
    IAppCache cache,
    ILogger<DocumentCategoryService> logger,
    IRepositoryBase<DocumentCategory> dcRepo,
    ConfigurationServicesClient configClient) : IDocumentCategoryService
{
    private readonly IAppCache _cache = cache;
    private readonly ILogger<DocumentCategoryService> _logger = logger;
    private readonly IRepositoryBase<DocumentCategory> _dcRepo = dcRepo;
    private readonly ConfigurationServicesClient _configClient = configClient;

    public async Task<IEnumerable<DocumentCategory>> GetAllAsync()
    {
        // Pull the DocumentCategories from the database.
        // Commented for now until MongoDb in Emerald is configured
        //    await _cache.GetOrAddAsync(
        //        "DocumentCategories",
        //        async () => await _dcRepo.GetAllAsync());

        // Pull the data from PCSS directly
        var config = await _cache.GetOrAddAsync(
            "ExternalDocumentCategories",
            async () => await _configClient.GetAllAsync());

        var categories = config
            .Where(c => DocumentCategory.ALL_DOCUMENT_CATEGORIES.Contains(c.Key));

        return categories.Select(c => new DocumentCategory
        {
            Name = c.Key,
            Value = c.Value
        });
    }

    public async Task RefreshDocumentCategoriesAsync()
    {
        var config = await _cache.GetOrAddAsync(
            "ExternalDocumentCategories",
            async () => await _configClient.GetAllAsync());

        var categories = config
            .Where(c => DocumentCategory.ALL_DOCUMENT_CATEGORIES.Contains(c.Key));

        foreach (var category in categories)
        {
            var categoryEntity = (await _dcRepo.FindAsync(dc => dc.Name == category.Key)).SingleOrDefault();
            if (categoryEntity == null)
            {
                await _dcRepo.AddAsync(new DocumentCategory
                {
                    Name = category.Key,
                    Value = category.Value,
                    ExternalId = category.PcssConfigurationId.GetValueOrDefault()
                });
                _logger.LogInformation("{Key} category added.", category.Key);
                continue;
            }

            // Update the document category if there is only a mismatch
            if (categoryEntity.Value != category.Value)
            {
                categoryEntity.Value = category.Value;
                categoryEntity.ExternalId = category.PcssConfigurationId.GetValueOrDefault();
                await _dcRepo.UpdateAsync(categoryEntity);
                _logger.LogInformation("{Key} category updated.", category.Key);
            }
        }
    }
}
