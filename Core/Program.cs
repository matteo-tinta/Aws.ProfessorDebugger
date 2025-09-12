using Core;
using Core.Cache;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var cli = new CliHandler();
            var options = cli.ParseArguments(args);

            var cacheProvider = AwsClientFactory.CreateCacheProviderForAwsCache(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            var graphCacheProvider = AwsClientFactory.CreateCacheProviderForAwsGraph(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            await AwsResourceCache.InitializeAsync(cacheProvider, new AwsResourceCacheInitOptions()
            {
                IgnoreCacheAndOverride = options.Value.IgnoreCache
            });

            var explorer = await AwsClientFactory.CreateResourceResolverAsync(new CreateResourceResolverOptions()
            {
                MaxLevel = options.Value.MaxLevel,
                CacheProvider = graphCacheProvider
            });

            await explorer.TraverseAsync(options.Value.AwsArn);

            await AwsResourceCache.SaveToDiskAsync(cacheProvider);

            
            await graphCacheProvider.SaveAsync(explorer.Graph);

            AwsClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
                Type = options.Value.OutputAs
            }).Print(explorer.Graph, explorer.Graph.GetOrCreateNode(options.Value.AwsArn));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}
