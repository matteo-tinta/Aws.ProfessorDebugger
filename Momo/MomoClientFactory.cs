using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo;
using Momo.Models;

public class MomoClientFactoryOptions
{
    public MomoExpectationFile ExpectationFile { get; set; }
}

public static class MomoClientFactory
{
    /// <summary>
    /// Entry point for feeding Momo with a new file, allowing it to analyze and match all relevant requests.
    /// </summary>
    public static MomoClient FeedMomo(MomoClientFactoryOptions options)
    {
        //clients
        var sqsClient = new AmazonSQSClient();
        var snsClient = new AmazonSimpleNotificationServiceClient();
        var s3Client = new AmazonS3Client();
        
        return MomoClient.ValidateAndCreate(sqsClient, snsClient, s3Client, options.ExpectationFile);
    }
}

