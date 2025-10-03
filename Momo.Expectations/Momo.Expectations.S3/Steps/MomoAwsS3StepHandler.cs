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
using NJsonSchema;

namespace Momo.Expectations.S3.Steps;

internal class MomoAwsS3StepHandler(IAmazonS3 s3Client): IStepHandler
{
    private Arn? _arn;
    private DateTime _since = DateTime.MinValue;
    private readonly IAmazonS3 _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task PrepareAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoAwsS3Expectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(MomoAwsS3StepHandler)} is invalid, expected MomoExpectation");
        }

        _since = DateTime.UtcNow.AddMinutes(-1); //since 1 minute this tool has started...
        _arn = Arn.ParseArn(config.Arn); 
        return Task.CompletedTask;
    }

    public async Task<bool> CheckAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        if (_arn is null)
        {
            throw new InvalidOperationException($"Please call {nameof(PrepareAsync)}() before calling {nameof(CheckAsync)}");
        }
        
        var config = (MomoAwsS3Expectation)baseConfig;

        var file = await DownloadFileWithMetadataAsync(_arn.ResourceName, config.File, cancellationToken);
        
        config.File.Prefix = null;
        config.File.Key = file.FileKey;

        if (config.Match is not null)
        {
            try
            {
                var content = ParseFileToJson(file);
                var validationErrors = config.Match.Validate(content.Content);
                return validationErrors.Count == 0;
            }
            catch (JsonException e)
            {
                throw new AssertException(
                    "A schema was provided, but the file was not a json format. Only json format files can be matched", e);
            }
        }


        return true;
    }

    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        var config = (MomoAwsS3Expectation)baseConfig;

        var newExpectation = new MomoAwsS3Expectation(_s3Client)
        {
            File = config.File,
            Arn = config.Arn,
        };
            
        var file = await DownloadFileWithMetadataAsync(_arn!.ResourceName, config.File, cancellationToken);

        try
        {
            var content = ParseFileToJson(file);

            var schema = JsonSchema.FromSampleJson(content.Content.ToString());
            newExpectation.Match = schema;
        }
        catch (JsonException)
        {
            Console.WriteLine("[WARNING]: File was not in json format. Only json format files can be matched");
        }

        return newExpectation;
    }

    private (JObject Content, MetadataCollection Metadata) ParseFileToJson(
        (string Content, string FileKey, MetadataCollection Metadata) file)
    {
        var jsonResult = JsonConvert.DeserializeObject<JObject>(file.Content);
        if (jsonResult is null)
        {
            throw new JsonException("Content was not a json object");
        }

        return (jsonResult, file.Metadata);
    }

    private async Task<(string Content, string FileKey, MetadataCollection Metadata)> DownloadFileWithMetadataAsync(string bucketName, MomoAwsS3FileModel file, CancellationToken cancellationToken)
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
                    .Where(o => searchRegex.IsMatch(o.Key) && o.LastModified >= _since)
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

            return (content, fileKey, response.Metadata);
        }
        catch (AmazonS3Exception e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            throw new AssertException($"File key {fileKey} was not found in the specified bucket {bucketName}");
        }
    }


    
}