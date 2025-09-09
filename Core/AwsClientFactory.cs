using Amazon.Lambda;
using Core.Cache.Providers;
using Core.Printers;

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
        Json
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
    }

    internal static class AwsClientFactory
    {
        public static AwsResourceResolver CreateResourceResolver(CreateResourceResolverOptions options)
        {
            var lambdaClient = new AmazonLambdaClient();
            var sqsClient = new Amazon.SQS.AmazonSQSClient();
            var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
            var s3Client = new Amazon.S3.AmazonS3Client();
            var iamClient = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();
            var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient();

            return new AwsResourceResolver(lambdaClient, sqsClient, snsClient, s3Client, iamClient, ssmClient, options.MaxLevel, options.EnableParallelExecution);
        }

        public static ICacheProvider CreateCacheProvider(CreateCacheProviderOptions options) => options.CacheType switch
        {
            CacheType.JsonFile => new JsonFileCacheProvider(".aws-cache.json"),
            //CacheType.Redis => TODO,
            _ => throw new NotImplementedException(),
        };

        public static IGraphPrinter CreateGraphPrinter(CreateGraphPrinterOptions options) => options.Type switch
        {
            GraphPrinterType.Cli => new CliGraphPrinter(),
            GraphPrinterType.Json => new JsonGraphPrinter(),
            _ => throw new NotImplementedException(),
        };

        public static IGraphPrinter CreateGraphPrinter() => CreateGraphPrinter(new CreateGraphPrinterOptions());
    }
}
