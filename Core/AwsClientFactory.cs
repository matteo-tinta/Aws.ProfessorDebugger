using Amazon.Lambda;
using Core.Printers;

namespace Core
{
    internal enum GraphPrinterType
    {
        Cli,
        Json
    }

    internal record CreateGraphPrinterOptions
    {
        public GraphPrinterType Type { get; set; } = GraphPrinterType.Cli;
    }

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

        public static IGraphPrinter CreateGraphPrinter(CreateGraphPrinterOptions options) => options.Type switch
        {
            GraphPrinterType.Cli => new CliGraphPrinter(),
            GraphPrinterType.Json => new JsonGraphPrinter(),
            _ => throw new NotImplementedException(),
        };

        public static IGraphPrinter CreateGraphPrinter() => CreateGraphPrinter(new CreateGraphPrinterOptions());
    }
}
