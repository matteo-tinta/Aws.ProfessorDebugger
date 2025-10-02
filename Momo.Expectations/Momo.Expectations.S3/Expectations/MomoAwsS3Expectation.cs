using Amazon.S3;
using Momo.Exceptions;
using Momo.Expectations.S3.Steps;
using Momo.Steps;
using NJsonSchema;
using ResourceArn = Models.Arn;

namespace Momo.Expectations.S3.Expectations;

public class MomoAwsS3Expectation(IAmazonS3 s3Client) : IMomoExpectation
{
    public required string Arn { get; set; }
    public required MomoAwsS3FileModel File { get; set; }
    public JsonSchema? Match { get; set; }
    
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        var service = ResourceArn.ParseArn(Arn).Service;

        return service.ToLower().Trim() switch
        {
            "s3" => new MomoAwsS3StepHandler(s3Client),
            _ => throw new InvalidOperationException($"This type of arn ({Arn} -> {service}) is not an S3 valid format")
        };
    }

    public void Validate()
    {
        _ = !ResourceArn.ParseArn(Arn).Service.Equals("s3", StringComparison.CurrentCultureIgnoreCase) 
            ? throw new MomoFileValidationException(nameof(Arn), "Arn was invalid. Only S3 is allowed is allowed for S3 blocks") 
            : true;

        if (File.Prefix != null && File.Prefix.StartsWith("/"))
        {
            throw new MomoFileValidationException(nameof(File.Prefix), "File prefix must not start with '/'.");
        }
    }

    public override string ToString() => Arn;
}

public record MomoAwsS3FileModel
{
    public string? Prefix { get; set; }
    public string Key { get; set; }
}