using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Models;
using Momo.Exceptions;
using Momo.Expectations.S3.Expectations;
using Momo.Steps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Momo.Expectations.S3.Steps;

internal partial class MomoAwsS3StepHandler(IAmazonS3 s3Client): IStepHandler
{
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
    private Dictionary<string, object> _validations = new();
    private Arn? _arn;

    [GeneratedRegex(@"\bContent(?=[\.\[])", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ExpectationContentRegex();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task PrepareAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoAwsS3Expectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(MomoAwsS3StepHandler)} is invalid, expected MomoExpectation");
        }
        
        _arn = Arn.ParseArn(config.Arn); 
        return Task.CompletedTask;
    }

    public async Task<bool> CheckAsync(IMomoExpectation baseConfig, int timeout, CancellationToken cancellationToken)
    {
        if (_arn is null)
        {
            throw new InvalidOperationException($"Please call {nameof(PrepareAsync)}() before calling {nameof(CheckAsync)}");
        }
        
        var config = (MomoAwsS3Expectation)baseConfig;
        var pattern = ExpectationContentRegex();

        _validations = new Dictionary<string, object>();
        foreach (var expectation in config.Match)
        {
            switch (expectation.Key.ToLower().Trim())
            {
                case "filename":
                    await DownloadJsonWithMetadataAsync(
                        _arn.ResourceName, 
                        expectation.Value,
                        cancellationToken);
                    break;
            }

            if (pattern.Match(expectation.Key).Success)
            {
                var file = GetFileFromValidation();
                        
                var newKey = pattern.Replace(expectation.Key, "___MomoContent");
                var token = file.Content.SelectToken(newKey);
                        
                if (token is not null && string.Equals(token.ToString(), expectation.Value, StringComparison.OrdinalIgnoreCase))
                    return true;

                throw new AssertException(
                    $"A file with the current key was found on bucket \"{_arn.ResourceName}\" but the \"{expectation.Key}\" didn't match the expectation");
            }
        }

        return true;
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
        //TODO: add assertion exception file not found

        using var stream = response.ResponseStream;
        using var reader = new StreamReader(stream);
        string content = await reader.ReadToEndAsync();

        AddFileToValidations((content, response.Metadata));
    }


    
}