using System;
using System.Collections.Generic;
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
    Task<List<BinderDto>> GetByLabels(Dictionary<string, string> labels);
}

public class BinderService(
    IAppCache cache,
    IMapper mapper,
    ILogger<BinderService> logger,
    IRepositoryBase<Binder> binderRepo
    ) : CrudServiceBase<IRepositoryBase<Binder>, Binder, BinderDto>(
        cache,
        mapper,
        logger,
        binderRepo), IBinderService
{
    public override string CacheName => nameof(BinderService);

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

    public override Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto, bool isEdit = false)
    {
        throw new NotImplementedException("Binder validations are executed via BinderProcessors");
    }
}
