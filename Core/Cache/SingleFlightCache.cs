using System.Collections.Concurrent;
using Core.Cache.Models;

namespace Core.Cache;

public class SingleFlightCache
{
    private readonly ConcurrentDictionary<string, IInflight> _inflight = new();

    public Task<T> GetAsync<T>(string key, Func<Task<T>> factory)
    {
        string invariantCacheKey = key.ToLowerInvariant();
        var inflight = (Inflight<T>)_inflight.GetOrAdd(invariantCacheKey, _ =>
        {
            var lazyTask = new Lazy<Task<T>>(() => FetchAndCleanup(invariantCacheKey, factory), LazyThreadSafetyMode.ExecutionAndPublication);
            return new Inflight<T>(lazyTask);
        });

        return inflight.TypedTask;
    }

    private async Task<T> FetchAndCleanup<T>(string key, Func<Task<T>> factory)
    {
        try
        {
            return await factory();
        }
        finally
        {
            _inflight.TryRemove(key, out _);
        }
    }
}