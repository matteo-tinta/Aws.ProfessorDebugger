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
        
        return MomoClient.ValidateAndCreate(sqsClient, snsClient, options.ExpectationFile);
    }
}

