using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SQS;
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
        
        public required AmazonLambdaClient LambdaClient { get; set; }
        public required IAmazonSQS SQSClient { get; set; }
        public required IAmazonSimpleNotificationService SNSClient { get; set; }
        public required IAmazonS3 S3Client { get; set; }
        public required IAmazonIdentityManagementService IamClient { get; set; }
        public required IAmazonSimpleSystemsManagement SsmClient { get; set; }
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
            var cache = new AwsResourceSingleFlightCache(
                options.S3Client,
                options.LambdaClient,
                options.SsmClient,
                options.SNSClient,
                options.IamClient,
                options.SQSClient);
            
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
