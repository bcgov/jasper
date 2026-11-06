using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AppDocument;

namespace Scv.Api.Services;

public interface IAppDocumentService : ICrudService<AppDocumentDto>
{
    Task<OperationResult<AppDocumentDto>> GetAppDocumentByKeyAsync(string key);
}

public class AppDocumentService(
    IAppCache cache,
    IMapper mapper,
    ILogger<AppDocumentService> logger,
    IRepositoryBase<AppDocument> repository) : CrudServiceBase<IRepositoryBase<AppDocument>, AppDocument, AppDocumentDto>(cache, mapper, logger, repository), IAppDocumentService
{
    public override string CacheName => nameof(AppDocumentService);

    public override Task<OperationResult<AppDocumentDto>> ValidateAsync(AppDocumentDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<AppDocumentDto>.Success(dto));

    public async Task<OperationResult<AppDocumentDto>> GetAppDocumentByKeyAsync(string key)
    {
        var appDocument = (await this.Repo.FindAsync(x => x.Key == key))?.FirstOrDefault();
        if (appDocument is null)
        {
            this.Logger.LogWarning("App document with key: {Key} is not found", key.SanitizeForLog());
            return OperationResult<AppDocumentDto>.Failure($"App document with key '{key}' not found.");
        }
        var dto = Mapper.Map<AppDocumentDto>(appDocument);
        return OperationResult<AppDocumentDto>.Success(dto);
    }
}
