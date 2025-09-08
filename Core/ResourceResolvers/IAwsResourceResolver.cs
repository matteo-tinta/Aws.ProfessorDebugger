
namespace Core.ResourceResolvers
{
    internal interface IAwsResourceResolver
    {
        Task<List<string>> GetDownstreamResourcesAsync();
        Task<List<string>> GetUpstreamResourcesAsync();
    }
}
