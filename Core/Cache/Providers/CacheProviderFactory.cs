using Core.Cache.Models;
using Models;

namespace Core.Cache.Providers;

public record CreateCacheProviderOptions
{
    public CacheType CacheType { get; set; } = CacheType.JsonFile;
}

public enum CacheType
{
    JsonFile,
    //TODO: Redis?
}

public static class CacheProviderFactory
{
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
}