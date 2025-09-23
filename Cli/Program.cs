using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
using Cli.FileLoader;
using Core;
using Core.Cache.Providers;
using Momo;
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

    static AmazonS3Client GetAmazonS3Client()
    {
        try
        {
            return new AmazonS3Client(new AmazonS3Config()
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT"),
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = Environment.GetEnvironmentVariable("AWS_REGION")
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WARNING]: unable to construct s3 client, returning default: {e}");
            return new AmazonS3Client();
        }
    }

    static async Task ExecuteMomo(MomoOptions options)
    {
        //clients
        var sqsClient = new AmazonSQSClient();
        var snsClient = new AmazonSimpleNotificationServiceClient();
        
        
        var client = MomoClientFactory.FeedMomo(new MomoClientFactoryOptions()
        {
            ExpectationFile = await MomoFileLoader.LoadAsync(options.InputFile),
            s3Client = GetAmazonS3Client(),
            snsClient = snsClient,
            sqsClient = sqsClient
        });

        await client.MatchExpectations(CancellationToken.None);
    }
    
    static async Task ExecuteGraphAsync(GraphOptions options)
    {
        //clients
        var sqsClient = new AmazonSQSClient();
        var snsClient = new AmazonSimpleNotificationServiceClient();
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
            S3Client = GetAmazonS3Client(),
            SNSClient = snsClient,
            SsmClient = ssmClient
        });

        await explorer.TraverseAsync(options.AwsArn);

        await graphCacheProvider.SaveAsync(explorer.Graph);

        CliClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
            Type = options.OutputAs
        }).Print(explorer.Graph, explorer.Graph.GetOrCreateNode(options.AwsArn));
    }
}