using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface ICrudService<TDto> where TDto : BaseDto
{
    Task<List<TDto>> GetAllAsync();
    Task<TDto> GetByIdAsync(string id);
    Task<OperationResult<TDto>> AddAsync(TDto dto);
    Task<OperationResult<TDto>> AddRangeAsync(List<TDto> dtos);
    Task<OperationResult<TDto>> ValidateAsync(TDto dto, bool isEdit = false);
    Task<OperationResult<TDto>> UpdateAsync(TDto dto);
    Task<OperationResult> DeleteAsync(string id);
    Task<OperationResult> DeleteRangeAsync(List<string> ids);
}

public abstract class CrudServiceBase<TRepo, TEntity, TDto>(
    IAppCache cache,
    IMapper mapper,
    ILogger logger,
    TRepo repo) : ServiceBase(cache), ICrudService<TDto>
    where TRepo : IRepositoryBase<TEntity>
    where TEntity : EntityBase
    where TDto : BaseDto
{
    public IMapper Mapper { get; } = mapper;
    public ILogger Logger { get; } = logger;
    public TRepo Repo { get; } = repo;

    public virtual async Task<List<TDto>> GetAllAsync() =>
        await this.GetDataFromCache(
            this.CacheName,
            async () =>
            {
                var entities = (await this.Repo.GetAllAsync());
                return this.Mapper.Map<List<TDto>>(entities);
            });

    public virtual async Task<TDto> GetByIdAsync(string id)
    {
        var entity = await this.Repo.GetByIdAsync(id);

        return this.Mapper.Map<TDto>(entity);
    }

    public virtual async Task<OperationResult<TDto>> AddAsync(TDto dto)
    {
        try
        {
            var entity = this.Mapper.Map<TEntity>(dto);

            await this.Repo.AddAsync(entity);

            this.InvalidateCache(this.CacheName);

            return OperationResult<TDto>.Success(this.Mapper.Map<TDto>(entity));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error when adding data: {message}", ex.Message);
            return OperationResult<TDto>.Failure("Error when adding data.");
        }
    }

    public virtual async Task<OperationResult<TDto>> AddRangeAsync(List<TDto> dtos)
    {
        try
        {
            var entities = Mapper.Map<List<TEntity>>(dtos);

            await Repo.AddRangeAsync(entities);

            InvalidateCache(CacheName);

            return OperationResult<TDto>.Success(Mapper.Map<TDto>(entities));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when adding data: {Message}", ex.Message);
            return OperationResult<TDto>.Failure("Error when adding data.");
        }
    }

    public abstract Task<OperationResult<TDto>> ValidateAsync(TDto dto, bool isEdit = false);

    public virtual async Task<OperationResult<TDto>> UpdateAsync(TDto dto)
    {
        try
        {
            var entity = await this.Repo.GetByIdAsync(dto.Id);

            this.Mapper.Map(dto, entity);

            await this.Repo.UpdateAsync(entity);

            this.InvalidateCache(this.CacheName);

            return OperationResult<TDto>.Success(this.Mapper.Map<TDto>(entity));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error when updating data: {message}", ex.Message);
            return OperationResult<TDto>.Failure("Error when updating data.");
        }
    }

    public virtual async Task<OperationResult> DeleteAsync(string id)
    {
        try
        {
            var existingEntity = await this.Repo.GetByIdAsync(id);
            if (existingEntity == null)
            {
                var err = new List<string> { $"Entity not found." };
                return OperationResult.Failure([.. err]);
            }

            await this.Repo.DeleteAsync(existingEntity);

            this.InvalidateCache(this.CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error deleting data: {message}", ex.Message);
            return OperationResult.Failure("Error when deleting data.");
        }
    }

    public virtual async Task<OperationResult> DeleteRangeAsync(List<string> ids)
    {
        try
        {
            var existingEntities = new List<TEntity>();
            var tasks = ids.Select(Repo.GetByIdAsync);
            var entities = await Task.WhenAll(tasks);
            existingEntities.AddRange(entities.Where(ent => ent != null));

            if (existingEntities.Count == 0)
            {
                var err = new List<string> { $"No entities found." };
                return OperationResult.Failure([.. err]);
            }

            await Repo.DeleteRangeAsync(existingEntities);

            InvalidateCache(CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting data: {Message}", ex.Message);
            return OperationResult.Failure("Error when deleting data.");
        }
    }
}
