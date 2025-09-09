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
            var explorer = AwsClientFactory.CreateResourceResolver();
            var cacheProvider = AwsClientFactory.CreateCacheProvider(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            await AwsResourceCache.InitializeAsync(cacheProvider, new AwsResourceCacheInitOptions()
            {
                IgnoreCacheAndOverride = options.Value.IgnoreCache
            });

            var graph = await explorer.TraverseAsync(options.Value.AwsArn);

            await AwsResourceCache.SaveToDiskAsync(cacheProvider);

            AwsClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
                Type = options.Value.OutputAs
            }).Print(graph);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}
