using Amazon.Lambda;

namespace Core
{
    internal static class AwsClientFactory
    {
        public static AwsResourceResolver CreateResourceResolver()
        {
            var lambdaClient = new AmazonLambdaClient();
            var sqsClient = new Amazon.SQS.AmazonSQSClient();
            var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
            var s3Client = new Amazon.S3.AmazonS3Client();
            var iamClient = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();

            return new AwsResourceResolver(lambdaClient, sqsClient, snsClient, s3Client, iamClient);
        }
    }
}
