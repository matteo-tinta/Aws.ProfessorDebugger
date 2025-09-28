using System.Net;
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
    private (string Content, MetadataCollection Metadata)? _file;
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
        
        await DownloadFileWithMetadataAsync(_arn.ResourceName, config.File, cancellationToken);
        
        foreach (var expectation in config.Match)
        {
            if (pattern.Match(expectation.Key).Success)
            {
                var newKey = pattern.Replace(expectation.Key, "___MomoContent");
                var token = ParseFileToJson().Content.SelectToken(newKey);
                        
                if (token is not null && string.Equals(token.ToString(), expectation.Value, StringComparison.OrdinalIgnoreCase))
                    return true;

                throw new AssertException($"A file with the current key was found on bucket \"{_arn.ResourceName}\" but the \"{expectation.Key}\" didn't match the expectation");
            }
        }

        return true;
    }

    public Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private (JObject Content, MetadataCollection Metadata) ParseFileToJson()
    {
        var jsonResult = JsonConvert.DeserializeObject<JObject>(_file!.Value.Content);
        if (jsonResult is null)
        {
            throw new AssertException("Invalid format (json expected)",
                new JsonException("Content was not a json object"));
        }

        //moving all the content inside __MomoContent so that if it's an array or an object does not make any difference
        jsonResult["___MomoContent"] = jsonResult;

        return (jsonResult, _file.Value.Metadata);

    }

    private async Task DownloadFileWithMetadataAsync(string bucketName, MomoAwsS3FileModel file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        
        var fileKey = file.Key;
        if (file.Prefix is not null)
        {
            try
            {
                var searchRegex = new Regex(file.Key);
                var searchingResponse = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request()
                {
                    BucketName = bucketName,
                    MaxKeys = 50,
                    Prefix = file.Prefix,
                }, cancellationToken);
            
                var fileResults = searchingResponse.S3Objects
                    .Where(o => searchRegex.IsMatch(o.Key))
                    .Select(o => o.Key)
                    .ToList();

                if (fileResults.Count != 1)
                {
                    throw new AssertException($"Unable to locate a single file from your search", 
                        fileResults.Count == 0 
                            ? new AssertException($"No files were found in {bucketName}")
                            : new AssertException($"Multiple files were found in {bucketName}:\n{JsonConvert.SerializeObject(fileResults, Formatting.Indented)}").BreakWhenRaised());
                }

                fileKey = fileResults.ElementAt(0);
            }
            catch (Exception e)
            {
                throw new AssertException($"An error occurred searching your files", e);
            }
        }
        
        try
        {
            var response = await _s3Client.GetObjectAsync(bucketName, fileKey, cancellationToken);
            
            using var stream = response.ResponseStream;
            using var reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync();

            _file = (content, response.Metadata);
        }
        catch (AmazonS3Exception e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            throw new AssertException($"File key {fileKey} was not found in the specified bucket {bucketName}");
        }
    }


    
}