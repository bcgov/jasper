using System;
using System.Threading.Tasks;
using LazyCache;

namespace Scv.Api.Services;

public abstract class ServiceBase(IAppCache cache)
{
    private readonly IAppCache _cache = cache;

    protected async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
    {
        return await _cache.GetOrAddAsync<T>(key, async () => await fetchFunction.Invoke());
    }
}
