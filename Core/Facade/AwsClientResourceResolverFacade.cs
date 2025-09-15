using Core.Cache;
using Core.Cache.Models;
using Core.Cache.Providers;
using Core.Models;

namespace Core.Facade;

internal class AwsClientResourceResolverFacade(
    AwsClientResourceResolver resolver,
    ICacheProvider<SerializableAwsCache> cacheProvider)
    : IAwsClientResourceResolver
{
    public AwsResourceGraph Graph => resolver.Graph;
    public async Task TraverseAsync(string arn)
    {
        await resolver.TraverseAsync(arn);

        await AwsResourceCache.SaveToDiskAsync(cacheProvider);
    }
}