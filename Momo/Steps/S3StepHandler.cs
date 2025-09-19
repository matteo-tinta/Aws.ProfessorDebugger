using Amazon.S3;
using Amazon.S3.Model;
using Models;
using Momo.Helpers;
using Momo.Models;

namespace Momo.Steps;

internal class S3StepHandler(IAmazonS3 s3Client): IStepHandler
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private Dictionary<string, object> _validations = new();

    public async Task<bool> WaitForMatchAsync(MomoExpectation config, int timeout, CancellationToken cancellationToken)
    {
        var s3BucketArn = Arn.ParseArn(config.Arn);

        return await RetryHelper.RetryAsync(async () =>
            {
                foreach (var expectation in config.Match)
                {
                    switch (expectation.Key.ToLower().Trim())
                    {
                        case "filename":
                            await DownloadJsonWithMetadataAsync(
                                s3BucketArn.ResourceName, 
                                expectation.Value,
                                cancellationToken);
                            break;
                    }
                }

                return true;
            },
            TimeSpan.FromMilliseconds(timeout), cancellationToken);
    }

    private (string Content, MetadataCollection Metadata) GetFileFromValidation()
    {
        if (_validations.TryGetValue("filename", out var value) &&
            value is (string Content, MetadataCollection Metadata))
        {
            return (Content, Metadata);
        }

        throw new ArgumentException(
            "you need to specify a \"filename\" expectation as first expectation in order to do expectation to the file");
    }

    private void AddFileToValidations((string Content, MetadataCollection Metadata) file)
    {
        try
        {
            _validations.Add("filename", (file.Content, file.Metadata));
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException(
                "Filename was already added, for each S3 bucket you need to create a new expectation node", e);
        }
    }

    private async Task
        DownloadJsonWithMetadataAsync(string bucketName, string key, CancellationToken cancellationToken)
    {
        var response = await _s3Client.GetObjectAsync(bucketName, key, cancellationToken);

        using var stream = response.ResponseStream;
        using var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();

        AddFileToValidations((content, response.Metadata));
    }
}