using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;


public interface IBinderService : ICrudService<BinderDto>
{
    Task OnBeforeBinderSaveAsync(BinderDto dto);
    Task<List<BinderDto>> GetByLabels(Dictionary<string, string> labels);
}

public class BinderService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserService> logger,
    IRepositoryBase<Binder> binderRepo
    ) : CrudServiceBase<IRepositoryBase<Binder>, Binder, BinderDto>(
        cache,
        mapper,
        logger,
        binderRepo), IBinderService
{
    public override string CacheName => nameof(BinderService);

    public override Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto, bool isEdit = false)
    {
        throw new System.NotImplementedException();
    }

    public virtual Task OnBeforeBinderSaveAsync(BinderDto dto)
    {
        // Clear all labels to prevent non-system generated label to be saved
        dto.Labels.Clear();

        // Ensure sorting order is correct
        dto.Documents = dto.Documents
            .OrderBy(d => d.Order)
            .Select((doc, index) => { doc.Order = index; return doc; })
            .ToList();

        return Task.CompletedTask;
    }

    public override async Task<OperationResult<BinderDto>> AddAsync(BinderDto dto)
    {
        await this.OnBeforeBinderSaveAsync(dto);

        return await base.AddAsync(dto);
    }

    public override async Task<OperationResult<BinderDto>> UpdateAsync(BinderDto dto)
    {
        await this.OnBeforeBinderSaveAsync(dto);

        return await base.UpdateAsync(dto);
    }

    public async Task<List<BinderDto>> GetByLabels(Dictionary<string, string> labels)
    {
        var filterBuilder = Builders<Binder>.Filter;
        var filter = FilterDefinition<Binder>.Empty;

        foreach (var label in labels)
        {
            var key = $"Labels.{label.Key}";
            filter &= filterBuilder.Eq(key, label.Value);
        }

        var entities = await this.Repo.FindAsync(CollectionNameConstants.BINDERS, filter);

        return this.Mapper.Map<List<BinderDto>>(entities);
    }
}
