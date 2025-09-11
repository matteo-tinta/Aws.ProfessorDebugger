using Amazon.Lambda;
using Core.Cache;
using Core.Cache.Providers;
using Core.Printers;
using Models;

namespace Core
{
    internal enum CacheType
    {
        JsonFile,
        //TODO: Redis?
    }

    internal enum GraphPrinterType
    {
        Cli,
        Json,
        Graph
    }

    internal record CreateCacheProviderOptions
    {
        public CacheType CacheType { get; set; } = CacheType.JsonFile;
    }

    internal record CreateGraphPrinterOptions
    {
        public GraphPrinterType Type { get; set; } = GraphPrinterType.Cli;
    }

    internal record CreateResourceResolverOptions
    {
        public bool EnableParallelExecution { get; set; } = false;
        public int? MaxLevel { get; set; }
        public required ICacheProvider<AwsResourceGraph> CacheProvider { get; set; }
    }

    internal static class AwsClientFactory
    {
        public async static Task<AwsResourceResolver> CreateResourceResolverAsync(CreateResourceResolverOptions options)
        {
            var lambdaClient = new AmazonLambdaClient();
            var sqsClient = new Amazon.SQS.AmazonSQSClient();
            var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
            var s3Client = new Amazon.S3.AmazonS3Client();
            var iamClient = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();
            var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient();

            //Get Cache or build new cache
            AwsResourceGraph graph = null;
            try
            {
                graph = await options.CacheProvider.GetAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"== GRAPH CACHE WAS NOT LOADED SUCCESSFULLY: ${ex.Message}");
            }

            return new AwsResourceResolver(lambdaClient, sqsClient, snsClient, s3Client, iamClient, ssmClient, graph ?? new AwsResourceGraph(),
                options.MaxLevel, 
                options.EnableParallelExecution);
        }

        public static ICacheProvider<SerializableAwsCache> CreateCacheProviderForAwsCache(CreateCacheProviderOptions options) => options.CacheType switch
        {
            CacheType.JsonFile => new JsonFileCacheProvider<SerializableAwsCache>(".aws-cache.json"),
            //CacheType.Redis => TODO,
            _ => throw new NotImplementedException(),
        };

        public static ICacheProvider<AwsResourceGraph> CreateCacheProviderForAwsGraph(CreateCacheProviderOptions options) => options.CacheType switch
        {
            CacheType.JsonFile => new JsonFileCacheProvider<AwsResourceGraph>(".aws-graph-cache.json"),
            //CacheType.Redis => TODO,
            _ => throw new NotImplementedException(),
        };

        public static IGraphPrinter CreateGraphPrinter(CreateGraphPrinterOptions options) => options.Type switch
        {
            GraphPrinterType.Cli => new CliGraphPrinter(),
            GraphPrinterType.Json => new JsonGraphPrinter(),
            GraphPrinterType.Graph => new GraphGraphPrinter(),
            _ => throw new NotImplementedException(),
        };

        public static IGraphPrinter CreateGraphPrinter() => CreateGraphPrinter(new CreateGraphPrinterOptions());
    }
}
