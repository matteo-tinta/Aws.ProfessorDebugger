using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Core.Cache;
using Core.Cache.Providers;
using Core.ResourceResolvers;
using Models;

namespace Core
{
    internal class AwsResourceResolver
    {
        private readonly AmazonLambdaClient lambdaClient;
        private readonly AmazonSQSClient sqsClient;
        private readonly AmazonSimpleNotificationServiceClient snsClient;
        private readonly AmazonS3Client s3Client;
        private readonly IAmazonIdentityManagementService iamClient;
        private readonly IAmazonSimpleSystemsManagement ssmClient;
        private readonly bool canUseParallelExecution;
        private readonly int? maxLevel;
        public AwsResourceGraph Graph { get; private set; }

        public AwsResourceResolver(
            AmazonLambdaClient lambdaClient,
            AmazonSQSClient sqsClient,
            AmazonSimpleNotificationServiceClient snsClient,
            AmazonS3Client s3Client,
            IAmazonIdentityManagementService iamClient,
            IAmazonSimpleSystemsManagement ssmClient,
            AwsResourceGraph graph,
            int? maxLevel = null,
            bool canUseParallelExecution = false)
        {
            this.lambdaClient = lambdaClient;
            this.sqsClient = sqsClient;
            this.snsClient = snsClient;
            this.s3Client = s3Client;
            this.iamClient = iamClient;
            this.ssmClient = ssmClient;
            this.canUseParallelExecution = canUseParallelExecution;
            this.maxLevel = maxLevel;
            this.Graph = graph;

            if (canUseParallelExecution)
            {
                Console.WriteLine("[DEBUG]: Parallel execution is enabled!");
            }
        }

        public async Task TraverseAsync(string arn, int currentLevel = 0)
        {
            if (maxLevel != null && currentLevel > maxLevel)
                return;

            if (Graph.Nodes.ContainsKey(arn))
                return;

            Console.WriteLine($"-> TRAVERSING {arn} ...");
            var resolver = GetResolverByArn(arn);
            var node = Graph.GetOrCreateNode(arn);

            List<string> parentsArn = await resolver.GetUpstreamResourcesAsync();
            List<string> childrenArn = await resolver.GetDownstreamResourcesAsync();

            if (canUseParallelExecution)
            {
                var parentTasks = parentsArn.Select(async parentArn => await TraverseParents(parentArn, currentLevel, node));
                var childrenTasks = childrenArn.Select(async parentArn => await TraverseParents(parentArn, currentLevel, node));

                await Task.WhenAll(parentTasks.Concat(childrenTasks));
            }
            else
            {
                foreach (var parentArn in parentsArn)
                {
                    await TraverseParents(parentArn, currentLevel, node);
                }

                foreach (var childArn in childrenArn)
                {
                    await TraverseChildren(childArn, currentLevel, node);
                }
            }
        }

        private async Task TraverseParents(string parentArn, int level, AwsResourceNode node)
        {
            //var parentNode = Graph.GetOrCreateNode(parentArn);
            //parentNode.Children.Add(node.Arn);
            node.Parents.Add(parentArn);
            await TraverseAsync(parentArn, level + 1);
        }

        private async Task TraverseChildren(string childArn, int level, AwsResourceNode node)
        {
            //var childNode = Graph.GetOrCreateNode(childArn);
            //childNode.Parents.Add(node.Arn);
            node.Children.Add(childArn);
            await TraverseAsync(childArn, level + 1);
        }

        private IAwsResourceResolver GetResolverByArn(string arn)
            => arn.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
            {
                (var arn2) when arn2.Contains(":lambda:") => new AwsResourceLambdaResolver(arn, lambdaClient, iamClient),
                (var arn2) when arn2.Contains(":sqs:") => new AwsResourceSQSResolver(arn, sqsClient, lambdaClient, ssmClient, iamClient),
                (var arn2) when arn2.Contains(":sns:") => new AwsResourceSNSResolver(arn, s3Client, lambdaClient, iamClient, snsClient),
                (var arn2) when arn2.Contains(":::") => new AwsResourceS3Resolver(arn, s3Client),
                _ => throw new NotImplementedException(),
            };
    }
}
