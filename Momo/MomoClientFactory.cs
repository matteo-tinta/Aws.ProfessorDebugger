using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo.Models;

namespace Momo;

public class MomoClientFactoryOptions
{
    public required MomoExpectationFile ExpectationFile { get; set; }
    public required IAmazonS3 s3Client { get; set; }
    public required IAmazonSimpleNotificationService snsClient { get; set; }
    public required IAmazonSQS sqsClient { get; set; }
}

public static class MomoClientFactory
{
    /// <summary>
    /// Entry point for feeding Momo with a new file, allowing it to analyze and match all relevant requests.
    /// </summary>
    public static MomoClient FeedMomo(MomoClientFactoryOptions options)
    {
        return MomoClient.ValidateAndCreate(
            options.sqsClient, 
            options.snsClient, 
            options.s3Client, 
            options.ExpectationFile);
    }
}