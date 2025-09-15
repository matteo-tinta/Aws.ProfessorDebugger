using Amazon.Lambda;
using Core.Cache;
using Core.Cache.Models;
using Core.Cache.Providers;
using Core.Facade;
using Core.Models;

namespace Core
{
    public record CreateResourceResolverOptions
    {
        public int? MaxLevel { get; set; }
        public required ICacheProvider<AwsResourceGraph> GraphCacheProvider { get; set; }
        public required ICacheProvider<SerializableAwsCache> CacheProvider { get; set; }
        public bool IgnoreCacheAndOverride { get; internal set; }
    }

    public static class AwsClientFactory
    {
        public static async Task<IAwsClientResourceResolver> Create(CreateResourceResolverOptions options)
        {
            //Facaded to hide internal implementation
            var resolver = await CreateResourceResolverAsync(options);
            return new AwsClientResourceResolverFacade(
                resolver,
                options.CacheProvider);
        }
        
        private static async Task<AwsClientResourceResolver> CreateResourceResolverAsync(CreateResourceResolverOptions options)
        {
            var lambdaClient = new AmazonLambdaClient();
            var sqsClient = new Amazon.SQS.AmazonSQSClient();
            var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
            var s3Client = new Amazon.S3.AmazonS3Client();
            var iamClient = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();
            var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient();

            var cache = new AwsResourceSingleFlightCache(
                s3Client,
                lambdaClient,
                ssmClient,
                snsClient,
                iamClient,
                sqsClient);
            
            // var graphCacheProvider = CreateCacheProviderForAwsGraph(options.CacheType);
            // var cacheProvider = CreateCacheProviderForAwsCache(options.CacheType);
            
            //Get Cache or build new cache
            AwsResourceGraph graph = null;
            if (!options.IgnoreCacheAndOverride)
            {
                try
                {
                    graph = await options.GraphCacheProvider.GetAsync();
                }
                catch (Exception ex)
                {
                    //ignored
                    //TODO: Add feedback
                }
            }
            
            await AwsResourceCache.InitializeAsync(options.CacheProvider, new AwsResourceCacheInitOptions()
            {
                IgnoreCacheAndOverride = options.IgnoreCacheAndOverride
            });

            return new AwsClientResourceResolver(graph ?? new AwsResourceGraph(), cache, options.MaxLevel);
        }
    }
}
