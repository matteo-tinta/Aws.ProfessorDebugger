using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Momo.Expectations.S3.Expectations;
using NJsonSchema;

namespace Cli.FileLoader.Models;

public class MomoAwsS3ExpectationJsonModel
{
    public string Arn { get; set; }
    public MomoAwsS3FileJsonModel File { get; set; }
    public JsonSchema? Match { get; set; }

    public MomoAwsS3Expectation Build()
    {
        var s3Client = Services.Provider.GetRequiredService<IAmazonS3>();
        
        return new MomoAwsS3Expectation(s3Client)
        {
            Arn = Arn,
            File = new MomoAwsS3FileModel
            {
                Key = File.Key,
                Prefix = File.Prefix
            },
            Match = Match
        };
    }
}

public record MomoAwsS3FileJsonModel
{
    public string Prefix { get; set; }
    public string Key { get; set; }
}