using Core.Cache;
using Core.ResourceResolvers;
using Models;

namespace Core
{
    public interface IAwsClientResourceResolver
    {
        public AwsResourceGraph Graph { get; }

        Task TraverseAsync(string arn);
    }

    internal class AwsClientResourceResolver
    {
        
        private readonly AwsResourceSingleFlightCache cache;
        private readonly int? maxLevel;
        public AwsResourceGraph Graph { get; }

        public AwsClientResourceResolver(AwsResourceGraph graph,
            AwsResourceSingleFlightCache cache,
            int? maxLevel = null)
        {
            this.cache = cache;
            this.maxLevel = maxLevel;
            this.Graph = graph;
        }

        public async Task TraverseAsync(string arn, int currentLevel = 0)
        {
            if (maxLevel != null && currentLevel > maxLevel)
                return;

            if (Graph.Nodes.ContainsKey(arn))
                return;

            var resolver = GetResolverByArn(arn);
            var node = Graph.GetOrCreateNode(arn);

            var parentsArn = await resolver.GetUpstreamResourcesAsync();
            var parentTasks = parentsArn.Select(parentArn => TraverseParents(parentArn, currentLevel, node));

            var childrenArn = await resolver.GetDownstreamResourcesAsync();
            var childrenTasks = childrenArn.Select(childArn => TraverseChildren(childArn, currentLevel, node));

            await Task.WhenAll(parentTasks.Concat(childrenTasks));
        }

        private async Task TraverseParents(string parentArn, int level, AwsResourceNode node)
        {
            node.Parents.Add(parentArn);
            await TraverseAsync(parentArn, level + 1);
        }

        private async Task TraverseChildren(string childArn, int level, AwsResourceNode node)
        {
            node.Children.Add(childArn);
            await TraverseAsync(childArn, level + 1);
        }

        private IAwsResourceResolver GetResolverByArn(string arn)
            => arn.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
            {
                (var arn2) when arn2.Contains(":lambda:") => new AwsResourceLambdaResolver(arn, cache),
                (var arn2) when arn2.Contains(":sqs:") => new AwsResourceSQSResolver(arn, cache),
                (var arn2) when arn2.Contains(":sns:") => new AwsResourceSNSResolver(arn, cache),
                (var arn2) when arn2.Contains(":::") => new AwsResourceS3Resolver(arn, cache),
                _ => throw new NotImplementedException(),
            };
    }
}
