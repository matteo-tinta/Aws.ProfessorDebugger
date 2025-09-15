using Core;
using Core.Cache.Providers;

namespace Cli;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var cli = new CliHandler();
            var options = cli.ParseArguments(args);

            var cacheProvider = CacheProviderFactory.CreateCacheProviderForAwsCache(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            //TODO: This belongs to CLI not CORE!
            var graphCacheProvider = CacheProviderFactory.CreateCacheProviderForAwsGraph(new CreateCacheProviderOptions()
            {
                CacheType = options.Value.CacheType
            });

            var explorer = await AwsClientFactory.Create(new CreateResourceResolverOptions()
            {
                CacheProvider = cacheProvider,
                GraphCacheProvider = graphCacheProvider,
                MaxLevel = options.Value.MaxLevel
            });

            await explorer.TraverseAsync(options.Value.AwsArn);

            await graphCacheProvider.SaveAsync(explorer.Graph);

            CliClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
                Type = options.Value.OutputAs
            }).Print(explorer.Graph, explorer.Graph.GetOrCreateNode(options.Value.AwsArn));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}