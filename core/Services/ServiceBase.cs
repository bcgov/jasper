using LazyCache;

namespace Scv.Core.Services;

public abstract class ServiceBase(IAppCache cache)
{
    private readonly IAppCache _cache = cache;

    public abstract string CacheName { get; }

    /// <summary>
    /// Gets data from cache or fetches it using the provided function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="fetchFunction">The function to fetch data if not present in cache.</param>
    /// <param name="cacheExpiration">The optional cache expiration timespan. If not provided, the default cache duration which is configured in Startup.cs is used.</param>
    /// <returns>The cached or fetched data of type <typeparamref name="T"/>.</returns>
    protected async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction, TimeSpan? cacheExpiration = null)
    {
        if (cacheExpiration.HasValue)
        {
            return await _cache.GetOrAddAsync<T>(key, async () => await fetchFunction.Invoke(), cacheExpiration.Value);
        }

        return await _cache.GetOrAddAsync<T>(key, async () => await fetchFunction.Invoke());
    }

    /// <summary>
    /// Invalidates the cache entry for the specified key.
    /// </summary>
    /// <param name="key">The cache key to invalidate.</param>
    protected void InvalidateCache(string key)
    {
        _cache.Remove(key);
    }
}
