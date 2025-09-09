using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
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

        private Dictionary<string, AwsResourceNode> visited = [];
        
        public AwsResourceResolver(
            AmazonLambdaClient lambdaClient,
            AmazonSQSClient sqsClient,
            AmazonSimpleNotificationServiceClient snsClient,
            AmazonS3Client s3Client,
            IAmazonIdentityManagementService iamClient,
            IAmazonSimpleSystemsManagement ssmClient)
        {
            this.lambdaClient = lambdaClient;
            this.sqsClient = sqsClient;
            this.snsClient = snsClient;
            this.s3Client = s3Client;
            this.iamClient = iamClient;
            this.ssmClient = ssmClient;
        }

        public async Task<AwsResourceGraph> TraverseAsync(string arn)
        {
            Console.WriteLine($"-> TRAVERSING {arn} ...");
            if (visited.TryGetValue(arn, out AwsResourceNode? value))
            {
                Console.WriteLine($"-> Already done");
                return new AwsResourceGraph(); //Temporary!
            }
            

            //find the correct resolver
            var resolver = GetResolverByArn(arn);
            AwsResourceGraph result = new AwsResourceGraph();
            var node = result.GetOrCreateNode(arn, "");

            List<string> parentsArn = await resolver.GetUpstreamResourcesAsync();
            foreach (string parentArn in parentsArn)
            {
                var traversed = await TraverseAsync(parentArn);

                foreach (var foundNode in traversed.GetAllNodes())
                {
                    node.Parents.Add(foundNode);
                }
            }

            //List<string> childrenArn = await resolver.GetDownstreamResourcesAsync();
            //foreach (string parentArn in parentsArn)
            //{
            //    var traversed = await TraverseAsync(parentArn);

            //    foreach (var foundNode in traversed.GetAllNodes())
            //    {
            //        node.Children.Add(foundNode);
            //    }
            //}

            visited.Add(arn, node);
            return result;
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
    }
}
