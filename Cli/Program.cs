using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using Cli.FileLoader;
using Core;
using Core.Cache.Providers;
using Microsoft.Extensions.DependencyInjection;
using Momo;
using Momo.Exceptions;
using Momo.Models;

namespace Cli;

class Program
{
    private static MomoCliHandler? _momoCliHandler;
    
    static async Task Main(string[] args)
    {
        try
        {
            Services.Install();
            
            var cli = new CliHandler();
            var parsed = cli.ParseArguments(args);
            
            //Preventing resource dangling on CTRL+C or process exit...
            Console.CancelKeyPress += async (sender, e) =>
            {
                e.Cancel = true; // prevents immediate termination
                
                await CleanupAsync();
                Environment.Exit(2);
            };

            AppDomain.CurrentDomain.ProcessExit += async (sender, e) =>
            {
                await CleanupAsync();
            };

            AppDomain.CurrentDomain.UnhandledException += async (sender, e) =>
            {
                await CleanupAsync();
                Environment.Exit(1);
            };
            
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

    static async Task ExecuteMomo(MomoOptions options)
    {
        _momoCliHandler = new MomoCliHandler(new MomoClientFactoryOptions()
        {
            ExpectationFile = await MomoFileLoader.LoadAsync(options.InputFile),
            AutoMode = options.Auto
        }, options);
        
        await _momoCliHandler.ExecuteMomo();
    }

    static async Task CleanupAsync()
    {
        if (_momoCliHandler is not null)
        {
            await _momoCliHandler.DisposeAsync();
        }
    }

    static void PrintException(Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        if (ex is MessageAssertException messageAssertException)
        {
            Console.Error.WriteLine(messageAssertException.MessageBody);
        }

        if (ex is MomoFileValidationException momoFileValidationException)
        {
            Console.Error.WriteLine($"Invalid Property: {momoFileValidationException.PropertyName}");
        }

        if (ex is not AssertException)
        {
            Console.Error.WriteLine($"{ex.StackTrace}\n{new string('-', 30)}");
        }
        
        if (ex.InnerException is not null)
        {
            PrintException(ex.InnerException);
        }
    }
    
    static async Task ExecuteGraphAsync(GraphOptions options)
    {
        //clients
        var sqsClient = Services.Provider.GetRequiredService<IAmazonSQS>();
        var snsClient = Services.Provider.GetRequiredService<IAmazonSimpleNotificationService>();
        var iamClient = new AmazonIdentityManagementServiceClient();
        var lambdaClient = new AmazonLambdaClient();
        var ssmClient = new AmazonSimpleSystemsManagementClient();
        
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
            MaxLevel = options.MaxLevel,
            SQSClient = sqsClient,
            IamClient = iamClient,
            LambdaClient = lambdaClient,
            S3Client = Services.Provider.GetRequiredService<IAmazonS3>(),
            SNSClient = snsClient,
            SsmClient = ssmClient
        });

        await explorer.TraverseAsync(options.AwsArn);

        await graphCacheProvider.SaveAsync(explorer.Graph);

        CliClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
            Type = options.OutputAs,
            OutputPath = options.OutputTo
        }).Print(explorer.Graph, explorer.Graph.GetOrCreateNode(options.AwsArn));
    }
}