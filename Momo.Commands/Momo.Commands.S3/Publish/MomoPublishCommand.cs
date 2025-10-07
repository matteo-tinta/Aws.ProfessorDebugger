using Amazon.S3.Model;
using Momo.Commands.S3.Publish.Dto;
using Momo.Commands.S3.Publish.Extensions;

namespace Momo.Commands.S3.Publish;

internal class MomoPublishCommand(MomoS3PublishCommandDto dto): IMomoCommand
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var response = await dto.S3Client.PutObjectAsync(new PutObjectRequest()
        {
            BucketName = dto.BucketName,
            FilePath = dto.SourceFilePath,
            Key = dto.DestinationCompleteFileKey
        }, cancellationToken);
        
        response.EnsureSuccessStatusCode();
    }

    public override string ToString() => "Publish";
}