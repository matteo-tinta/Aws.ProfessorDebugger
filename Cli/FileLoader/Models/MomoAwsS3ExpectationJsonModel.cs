using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Momo.Expectations.S3.Expectations;

namespace Cli.FileLoader.Models;

public class MomoAwsS3ExpectationJsonModel
{
    public string Arn { get; set; }
    public Dictionary<string, string> Match { get; set; }

    public MomoAwsS3Expectation Build()
    {
        var s3Client = Services.Provider.GetRequiredService<IAmazonS3>();
        
        return new MomoAwsS3Expectation(s3Client)
        {
            Arn = Arn,
            Match = Match
        };
    }
}