using Momo.Commands.S3.Publish.Dto;

namespace Momo.Commands.S3;

public record MomoS3CommandDto
{
    public MomoS3PublishCommandDto? PublishOptions { get; private set; }

    public MomoS3CommandDto SetPublishOptions(MomoS3PublishCommandDto dto)
    {
        PublishOptions = dto;
        return this;
    }
}