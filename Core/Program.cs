using System.Text.RegularExpressions;
using Amazon.Lambda;
using Core;
using Core.AwsEventGraphBuilder;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure AWS clients
        var lambdaClient = new AmazonLambdaClient();
        var sqsClient = new Amazon.SQS.AmazonSQSClient();
        var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
        var s3Client = new Amazon.S3.AmazonS3Client();

        var explorer = new AwsServiceExplorer(lambdaClient, sqsClient, snsClient, s3Client);

        Console.Write("Enter the Lambda Function ARN (e.g., arn:aws:lambda:us-east-1:123456789012:function): ");
        var lambdaArn = Console.ReadLine();

        if (string.IsNullOrEmpty(lambdaArn) || !IsValidArn(lambdaArn))
        {
            Console.WriteLine("Invalid ARN provided. Exiting.");
            return;
        }

        // Start the graph traversal from the Lambda function
        var graph = await explorer.BuildGraphAsync(lambdaArn);
        Console.Clear();
        Console.WriteLine("============ Printing the graph ===========\r\n");
        Console.WriteLine("===========================================\r\n");
        graph.PrintGraph();

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }

    private static bool IsValidArn(string arn)
    {
        var pattern = @"^arn:aws:lambda:[a-z0-9-]+:\d{12}:function:[a-zA-Z0-9-_]+$";
        return Regex.IsMatch(arn, pattern);
    }
}