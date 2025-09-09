using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
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
        private Dictionary<string, AwsResourceNode> visited = [];

        public AwsResourceResolver(
            AmazonLambdaClient lambdaClient,
            AmazonSQSClient sqsClient,
            AmazonSimpleNotificationServiceClient snsClient,
            AmazonS3Client s3Client,
            IAmazonIdentityManagementService iamClient,
            IAmazonSimpleSystemsManagement ssmClient,
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

            if(canUseParallelExecution)
            {
                Console.WriteLine("[DEBUG]: Parallel execution is enabled!");
            }
        }

        public async Task<AwsResourceGraph> TraverseAsync(string arn, int currentLevel = 0)
        {
            if(maxLevel != null && currentLevel > maxLevel)
            {
                return new AwsResourceGraph(); //Exiting
            }
            if (visited.TryGetValue(arn, out AwsResourceNode? value))
            {
                return new AwsResourceGraph(); //Temporary!
            }

            Console.WriteLine($"-> TRAVERSING {arn} ...");
            var resolver = GetResolverByArn(arn);
            AwsResourceGraph result = new AwsResourceGraph();
            var node = result.GetOrCreateNode(arn, GeTypeByArn(arn));

            List<string> parentsArn = await resolver.GetUpstreamResourcesAsync();

            if (canUseParallelExecution)
            {
                var tasks = parentsArn.Select(async parentArn => await TraverseParents(parentArn, currentLevel, node));
                await Task.WhenAll(tasks);
            }
            else
            {
                foreach (var parentArn in parentsArn)
                {
                    await TraverseParents(parentArn, currentLevel, node);
                }
            }

            visited.Add(arn, node);
            return result;
        }

        private async Task TraverseParents(string parentArn, int level, AwsResourceNode node)
        {
            var traversed = await TraverseAsync(parentArn, level + 1);

            foreach (var foundNode in traversed.GetAllNodes())
            {
                node.Parents.Add(foundNode);
            }
        }

        private IAwsResourceResolver GetResolverByArn(string arn)
            => arn.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
            {
                (var arn2) when arn2.Contains(":lambda:") => new AwsResourceLambdaResolver(arn, lambdaClient),
                (var arn2) when arn2.Contains(":sqs:") => new AwsResourceSQSResolver(arn, sqsClient, lambdaClient, ssmClient, iamClient),
                (var arn2) when arn2.Contains(":sns:") => new AwsResourceSNSResolver(arn, s3Client, lambdaClient, iamClient),
                (var arn2) when arn2.Contains(":::") => new AwsResourceS3Resolver(arn),
                _ => throw new NotImplementedException(),
            };

        private string GeTypeByArn(string arn)
            => arn.ToLower(System.Globalization.CultureInfo.CurrentCulture) switch
            {
                (var arn2) when arn2.Contains(":lambda:") => "lambda",
                (var arn2) when arn2.Contains(":sqs:") => "sqs",
                (var arn2) when arn2.Contains(":sns:") => "sns",
                (var arn2) when arn2.Contains(":::") => "s3",
                _ => "",
            };
    }
}
