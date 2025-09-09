using System.Text.RegularExpressions;
using System.Xml.Linq;
using Amazon.Lambda;
using Core;
using Core.Cache;

class Program
{
    static async Task Main(string[] args)
    {

        // Configure AWS clients
        var lambdaClient = new AmazonLambdaClient();
        var sqsClient = new Amazon.SQS.AmazonSQSClient();
        var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient();
        var s3Client = new Amazon.S3.AmazonS3Client();
        var iamClient = new Amazon.IdentityManagement.AmazonIdentityManagementServiceClient();

        //Init cache from json file
        await AwsResourceCache.InitializeAsync();

        var explorer = new AwsResourceResolver(lambdaClient, sqsClient, snsClient, s3Client, iamClient);

        Console.Write("Enter a valid resource ARN (e.g., arn:aws:lambda:us-east-1:123456789012:resource)\r\n");
        Console.Write("== STRIP AWAY THE RESOURCE IDENTIFIER! (eg. arn:aws:sns:eu-west-1:297244223532:price-info-changes-dev) ==: \r\n");
        var lambdaArn = Console.ReadLine();
        //var lambdaArn = "arn:aws:lambda:eu-west-1:297244223532:function:visibility-upload-info-changed-dev";
        //var lambdaArn = "arn:aws:sqs:eu-west-1:297244223532:mastermind-dataloader-price-variantinfochanged-queue-dev";

        if (string.IsNullOrEmpty(lambdaArn) || !IsValidArn(lambdaArn))
        {
            Console.WriteLine("Invalid ARN provided. Exiting.");
            return;
        }

        // Start the graph traversal from the Lambda function
        var graph = await explorer.TraverseAsync(lambdaArn);

        //Save cache to disk for future usage
        await AwsResourceCache.SaveToDiskAsync();

        Console.WriteLine("\r\n===========================================\r\n");

        //PRINTING THE GRAPH
        foreach (var item in graph.GetAllNodes())
        {
            PrintChildrenGraph(item);
        }

        foreach (var item in graph.GetAllNodes())
        {
            PrintParentGraph(item);
        }

        //graph.PrintGraph();

        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }

    private static void PrintParentGraph(Models.AwsResourceNode node, int level = 1)
    {
        Console.WriteLine($"{new string('-', level)}> [{node.Type}] {node.Arn}");
        foreach (var source in node.Parents)
        {
            PrintParentGraph(source, level + 1);
        }
    }

    private static void PrintChildrenGraph(Models.AwsResourceNode node, int level = 1)
    {
        Console.WriteLine($"{new string('-', level)}> [{node.Type}] {node.Arn}");
        foreach (var source in node.Children)
        {
            PrintChildrenGraph(source, level + 1);
        }
    }

    private static bool IsValidArn(string arn)
    {
        var pattern = @"^arn:(aws|aws-cn|aws-us-gov):[a-z0-9-]+:[a-z0-9-]*:\d{0,12}:[^:\s]+(:[^:\s]+)*$";
        return Regex.IsMatch(arn, pattern);
    }
}