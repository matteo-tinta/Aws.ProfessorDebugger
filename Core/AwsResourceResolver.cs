using Core.Cache;
using Core.Models;
using Core.ResourceResolvers;

namespace Core
{
    public interface IAwsClientResourceResolver
    {
        public AwsResourceGraph Graph { get; }

        Task TraverseAsync(string arn);
    }

    internal class AwsClientResourceResolver(
        AwsResourceGraph graph,
        AwsResourceSingleFlightCache cache,
        int? maxLevel = null)
    {
        public AwsResourceGraph Graph { get; } = graph;

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
                (var arn2) when arn2.Contains(":sqs:") => new AwsResourceSqsResolver(arn, cache),
                (var arn2) when arn2.Contains(":sns:") => new AwsResourceSnsResolver(arn, cache),
                (var arn2) when arn2.Contains(":::") => new AwsResourceS3Resolver(arn, cache),
                _ => throw new NotImplementedException(),
            };
    }
}
