using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Scv.Db.Contexts;
using Scv.Db.Models;

namespace Scv.Db.Repositories;

public interface IRepositoryBase<TEntity> where TEntity : EntityBase
{
    Task<TEntity> GetByIdAsync(string id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> FindAsync(string collectionName, FilterDefinition<TEntity> filterDefinition);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}

public class RepositoryBase<TEntity>(JasperDbContext context, IMongoDatabase mongoDb) : IRepositoryBase<TEntity> where TEntity : EntityBase
{
    protected readonly JasperDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    protected readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();
    protected readonly IMongoDatabase _mongoDb = mongoDb;

    public virtual async Task<TEntity> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// FilterDefinition is MongoDb specific to construct and perform queries using native driver.
    /// This allows for dynamic field name queries (e.g. "Labels.{key}") and values at runtime.
    /// </summary>
    /// <param name="filterDefinition"></param>
    /// <returns></returns>
    public virtual async Task<IEnumerable<TEntity>> FindAsync(string collectionName, FilterDefinition<TEntity> filterDefinition)
    {
        var collection = _mongoDb.GetCollection<TEntity>(collectionName);

        return await collection.Find(filterDefinition).ToListAsync();
    }

    public virtual async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(TEntity entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
