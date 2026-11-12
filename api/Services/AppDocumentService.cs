using System;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Processors;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Core.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AppDocument;

namespace Scv.Api.Services;

public interface IAppDocumentService
{
    Task<AppDocumentDto> GetAppDocumentByKeyAsync(string key);
    Task<OperationResult<AppDocumentDto>> UpdateAsync(string key, UploadedFile file);
}

public class AppDocumentService(
    IAppCache cache,
    IMapper mapper,
    ILogger<AppDocumentService> logger,
    IRepositoryBase<AppDocument> repository) : ServiceBase(cache), IAppDocumentService
{
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<AppDocumentService> _logger = logger;
    private readonly IRepositoryBase<AppDocument> _repo = repository;

    public override string CacheName => nameof(AppDocumentService);

    public async Task<AppDocumentDto> GetAppDocumentByKeyAsync(string key)
        => await GetDataFromCache(GetCacheKey(key), async () =>
        {
            var appDocument = await FindByKeyAsync(key);
            if (appDocument is null)
            {
                _logger.LogWarning("App document with key: {Key} is not found", key.SanitizeForLog());
                return null;
            }

            return _mapper.Map<AppDocumentDto>(appDocument);
        });

    public async Task<OperationResult<AppDocumentDto>> UpdateAsync(string key, UploadedFile file)
    {
        try
        {
            var existing = await FindByKeyAsync(key);
            if (existing == null)
            {
                return OperationResult<AppDocumentDto>.Failure("App document not found.");
            }
            else
            {
                _mapper.Map(file, existing);
                await _repo.UpdateAsync(existing);
            }

            InvalidateCache(GetCacheKey(key));

            var dto = _mapper.Map<AppDocumentDto>(existing);
            dto.Data = null; // Do not return the file content

            return OperationResult<AppDocumentDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating app document with key: {Key}", key.SanitizeForLog());
            return OperationResult<AppDocumentDto>.Failure("An error occurred while updating the app document.");
        }
    }

    private string GetCacheKey(string key) => $"{CacheName}:{key}";

    private async Task<AppDocument> FindByKeyAsync(string key)
        => (await _repo.FindAsync(x => x.Key == key))?.FirstOrDefault();
}
