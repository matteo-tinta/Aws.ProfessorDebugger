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

            var cacheProvider = AwsClientFactory.CreateCacheProvider(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            await AwsResourceCache.InitializeAsync(cacheProvider, new AwsResourceCacheInitOptions()
            {
                IgnoreCacheAndOverride = options.Value.IgnoreCache
            });

            var explorer = AwsClientFactory.CreateResourceResolver(new CreateResourceResolverOptions()
            {
                EnableParallelExecution = false /*AwsResourceCache.CacheHasBeenInitialized*/,
                MaxLevel = options.Value.MaxLevel
            });

            await explorer.TraverseAsync(options.Value.AwsArn);

            await AwsResourceCache.SaveToDiskAsync(cacheProvider);

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
