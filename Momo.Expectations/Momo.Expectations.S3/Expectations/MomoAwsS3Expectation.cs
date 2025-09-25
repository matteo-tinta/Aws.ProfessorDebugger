using Amazon.S3;
using Momo.Expectations.S3.Steps;
using Momo.Steps;
using ResourceArn = Models.Arn;

namespace Momo.Expectations.S3.Expectations;

public class MomoAwsS3Expectation(IAmazonS3 s3Client) : IMomoExpectation
{
    public required string Arn { get; set; }
    public required Dictionary<string, string> Match { get; set; }
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        var service = ResourceArn.ParseArn(Arn).Service;

        return service.ToLower().Trim() switch
        {
            "s3" => new MomoAwsS3StepHandler(s3Client),
            _ => throw new InvalidOperationException($"This type of arn ({Arn} -> {service}) is not recognized yet")
        };
    }
}