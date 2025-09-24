using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Models;
using Momo.Expectations;
using Momo.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Momo.Steps;

internal partial class S3StepHandler(IAmazonS3 s3Client): IStepHandler
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private Dictionary<string, object> _validations = new();
    
    [GeneratedRegex(@"\bContent(?=[\.\[])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ExpectationContentRegex();

    public async Task<bool> WaitForMatchAsync(IMomoExpectation baseConfig, int timeout, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoAwsExpectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(S3StepHandler)} is invalid, expected MomoExpectation");
        }
        
        var s3BucketArn = Arn.ParseArn(config.Arn); 
        var pattern = ExpectationContentRegex();

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

                    if (pattern.Match(expectation.Key).Success)
                    {
                        var file = GetFileFromValidation();
                        
                        var newKey = pattern.Replace(expectation.Key, "___MomoContent");
                        var token = file.Content.SelectToken(newKey);
                        
                        if (token == null || !string.Equals(token.ToString(), expectation.Value, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }

                return true;
            },
            TimeSpan.FromMilliseconds(timeout), cancellationToken);
    }
    
    private (JObject Content, MetadataCollection Metadata) GetFileFromValidation()
    {
        if (!_validations.TryGetValue("filename", out var value) || value is not (string Content, MetadataCollection Metadata))
            throw new ArgumentException("you need to specify a \"filename\" expectation as first expectation in order to do expectation to the file");
        
        var jsonResult = JsonConvert.DeserializeObject<JObject>(Content);
        if (jsonResult is null)
        {
            throw new JsonException("Content was not a json object");
        }

        //moving all the content inside __MomoContent so that if it's an array or an object does not make any difference
        jsonResult["___MomoContent"] = jsonResult;

        return (jsonResult, Metadata);

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