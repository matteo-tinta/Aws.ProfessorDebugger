using Amazon.S3;

namespace Momo.Commands.S3.Publish.Dto;

public record MomoS3PublishCommandDto
{
    public required IAmazonS3 S3Client { get; init; }
    public required string BucketName { get; init; }
    public required string DestinationCompleteFileKey { get; init; }
    public required string SourceFilePath { get; init; }
}