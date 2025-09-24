using Momo.Steps;
using ResourceArn = Models.Arn;

namespace Momo.Expectations;

public class MomoAwsExpectation: IMomoExpectation
{
    public string Arn { get; set; }
    public Dictionary<string, string> Match { get; set; }
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        var service = ResourceArn.ParseArn(Arn).Service;

        return service.ToLower().Trim() switch
        {
            "sns" => new SnsStepHandler(options.sqsClient, options.snsClient),

            "s3" => new S3StepHandler(options.s3Client),

            "sqs"=> throw new InvalidOperationException("To match SQS queues, provide its SNS. If no SNS are available, skip the node and check downstream resources (eg. Lambdas, S3)"),

            _ => throw new InvalidOperationException($"This type of arn ({Arn} -> {service}) is not recognized yet")
        };
    }
}