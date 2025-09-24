using Core.Cache;

namespace Core.Tests.Cache;

public class SingleFlightCacheTests
{
    [Fact]
    public async Task SingleFlightCache_Should_Deduplicate_Concurrent_Calls()
    {
        var cache = new SingleFlightCache();
        int actualCalls = 0;

        var tasks = Enumerable.Range(0, 2000).Select(_ =>
            cache.GetAsync("test-key", async () =>
            {
                Interlocked.Increment(ref actualCalls);
                await Task.Delay(100); // simulate AWS call
                return "hello";
            })
        ).ToList();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, actualCalls); // should only call factory once
        Assert.True(results.All(r => r == "hello"));
    }
}