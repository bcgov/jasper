using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IBinderService : ICrudService<BinderDto>
{
    Task<OperationResult<List<BinderDto>>> GetByLabels(Dictionary<string, string> labels);
}

public class BinderService(
    IAppCache cache,
    IMapper mapper,
    ILogger<BinderService> logger,
    IRepositoryBase<Binder> binderRepo,
    IBinderFactory binderFactory) : CrudServiceBase<IRepositoryBase<Binder>, Binder, BinderDto>(
        cache,
        mapper,
        logger,
        binderRepo), IBinderService
{
    private readonly IBinderFactory _binderFactory = binderFactory;

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
}
