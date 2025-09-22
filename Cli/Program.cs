using Cli.FileLoader;
using Core;
using Core.Cache.Providers;
using Momo.Exceptions;
using Momo.Models;

namespace Cli;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var cli = new CliHandler();
            var parsed = cli.ParseArguments(args);
            
            switch (parsed)
            {
                case { Command: CommandType.Momo, Options: MomoOptions opts }:
                    await ExecuteMomo(opts);
                    break;

                case { Command: CommandType.Graph, Options: GraphOptions opts }:
                    await ExecuteGraphAsync(opts);
                    break;
            }
        }
        catch (Exception ex)
        {
            PrintException(ex);
        }
    }

    static void PrintException(Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        if (ex is MessageAssertException messageAssertException)
        {
            Console.Error.WriteLine(messageAssertException.MessageBody);
        }
        
        if (ex.InnerException is not null)
        {
            PrintException(ex.InnerException);
        }

        if (ex is not AssertException)
        {
            Console.Error.WriteLine($"{ex.StackTrace}");
        }
    }

    static async Task ExecuteMomo(MomoOptions options)
    {
        var client = MomoClientFactory.FeedMomo(new MomoClientFactoryOptions()
        {
            ExpectationFile = await MomoFileLoader.LoadAsync(options.InputFile)
        });

        await client.MatchExpectations(CancellationToken.None);
    }
    
    static async Task ExecuteGraphAsync(GraphOptions options)
    {
        var cacheProvider = CacheProviderFactory.CreateCacheProviderForAwsCache(new CreateCacheProviderOptions()
        {
            CacheType = options.CacheType
        });

        //TODO: This belongs to CLI not CORE!
        var graphCacheProvider = CacheProviderFactory.CreateCacheProviderForAwsGraph(new CreateCacheProviderOptions()
        {
            CacheType = options.CacheType
        });

        var explorer = await AwsClientFactory.Create(new CreateResourceResolverOptions()
        {
            CacheProvider = cacheProvider,
            GraphCacheProvider = graphCacheProvider,
            MaxLevel = options.MaxLevel
        });

        await explorer.TraverseAsync(options.AwsArn);

        await graphCacheProvider.SaveAsync(explorer.Graph);

        CliClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
            Type = options.OutputAs
        }).Print(explorer.Graph, explorer.Graph.GetOrCreateNode(options.AwsArn));
    }
}