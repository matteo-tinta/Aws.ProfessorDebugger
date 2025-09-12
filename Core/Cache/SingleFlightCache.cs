using System.Collections.Concurrent;

namespace Core.Cache;

public class SingleFlightCache
{
    private readonly ConcurrentDictionary<string, IInflight> _inflight = new();

    public Task<T> GetAsync<T>(string key, Func<Task<T>> factory)
    {
        // Deduplicate using key
        var inflight = (Inflight<T>)_inflight.GetOrAdd(key, _ =>
        {
            var task = FetchAndCleanup(key, factory);
            return new Inflight<T>(task);
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