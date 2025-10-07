using System.Text.Json.Serialization;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Momo.Commands;
using Momo.Commands.S3;
using Momo.Commands.S3.Publish.Dto;
using Momo.Exceptions;

namespace Cli.FileLoader.Models.Commands;

[MomoRestCommandType("s3")]
public class MomoS3CommandJsonModel: BaseMomoCommandBuildable
{
    [JsonPropertyName("operation")]
    public required string OperationType { get; set; }
    
    public required string BucketName { get; set; }
    
    public required string DestinationCompleteFileKey { get; set; }
    
    public string? SourceFilePath { get; set; }

    private static string AllowedOperations => String.Join(" - ", Enum.GetNames(typeof(MomoS3CommandType)).Select(c => c));
    
    public IMomoCommand Build(string jsonFilePath)
    {
        var s3Client = Services.Provider.GetRequiredService<IAmazonS3>();
        
        if(!Enum.TryParse(OperationType, out MomoS3CommandType type))
            throw new InvalidOperationException($"{OperationType} is not a valid operation type (Allowed types are: {AllowedOperations})");

        var dto = new MomoS3CommandDto();

        switch (type)
        {
            case MomoS3CommandType.Publish:
                var completeSourceFilePath = Path.Join(jsonFilePath, SourceFilePath ?? throw new MomoFileValidationException(nameof(SourceFilePath), "sourcePath is mandatory when using publish operation"));
                if (!File.Exists(completeSourceFilePath))
                {
                    throw new InvalidOperationException($"Source file not found in {completeSourceFilePath}");
                }
                
                dto.SetPublishOptions(new MomoS3PublishCommandDto()
                {
                    BucketName = BucketName,
                    DestinationCompleteFileKey = DestinationCompleteFileKey,
                    SourceFilePath = completeSourceFilePath,
                    S3Client = s3Client, 
                });
                break;
            
            default:
                //ignored
                break;
        }

        return new MomoS3Command(type, dto);
    }

}