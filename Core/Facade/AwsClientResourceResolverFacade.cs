using Core.Cache;
using Core.Cache.Models;
using Core.Cache.Providers;
using Models;

namespace Core.Facade;

internal class AwsClientResourceResolverFacade: IAwsClientResourceResolver
{
    private readonly AwsClientResourceResolver _resolver;
    private readonly ICacheProvider<SerializableAwsCache> _cacheProvider;

    public AwsClientResourceResolverFacade(
        AwsClientResourceResolver resolver,
        ICacheProvider<SerializableAwsCache> cacheProvider)
    {
        _resolver = resolver;
        _cacheProvider = cacheProvider;
    }
        
    public AwsResourceGraph Graph => _resolver.Graph;
    public async Task TraverseAsync(string arn)
    {
        await _resolver.TraverseAsync(arn);

        await AwsResourceCache.SaveToDiskAsync(_cacheProvider);
    }
}