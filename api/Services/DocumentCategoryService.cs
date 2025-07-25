using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using PCSSCommon.Clients.ConfigurationServices;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Services;

public interface IDocumentCategoryService
{
    Task<IEnumerable<DocumentCategoryDto>> GetAllAsync();
}

public class DocumentCategoryService(
    IAppCache cache,
    IMapper mapper,
    ConfigurationServicesClient configClient)
    : IDocumentCategoryService
{
    private readonly IAppCache _cache = cache;
    private readonly ConfigurationServicesClient _configClient = configClient;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<DocumentCategoryDto>> GetAllAsync()
    {
        // The long term solution is to pull the categories from the MongoDb
        // that is synced via the SyncDocumentCategoriesJob. Temporarily, these
        // categories are pulled directly from external source until the db
        // Emerald is configured.
        var configData = await _cache.GetOrAddAsync(
            "ExternalConfig",
            async () => await _configClient.GetAllAsync());

        var externalDocumentCategories = configData
            .Where(c => DocumentCategory.ALL_DOCUMENT_CATEGORIES.Contains(c.Key));

        return _mapper.Map<List<DocumentCategoryDto>>(externalDocumentCategories);
    }
}
