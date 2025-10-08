using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Momo.Commands.S3.Publish.Dto;
using NSubstitute;

namespace Momo.Commands.S3.Tests;

public class S3TestingContext
{
    protected IAmazonS3 S3Client { get; set; } = Substitute.For<IAmazonS3>();
} 

public class MomoS3CommandsTest: S3TestingContext
{
    public MomoS3CommandsTest()
    {
        
    }
    
    [Fact]
    public async Task Creating_A_Command_With_Publish_Return_PublishCommand()
    {
        //Arrange
        var dto = new MomoS3CommandDto()
            .SetPublishOptions(new MomoS3PublishCommandDto()
            {
                S3Client = S3Client,
                BucketName = "test-bucket-name",
                DestinationCompleteFileKey = "destination-file-key",
                SourceFilePath = "source-file-path"
            });
        
        S3Client.PutObjectAsync(Arg.Any<PutObjectRequest>()).Returns(Task.FromResult(new PutObjectResponse()
        {
            HttpStatusCode = HttpStatusCode.OK
        }));

        var sut = CreateSut(MomoS3CommandType.Publish, dto);
        
        //Act
        await sut.ExecuteAsync(CancellationToken.None);

        //Assert
        await S3Client.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(x => 
            x.BucketName == "test-bucket-name" &&
            x.Key == "destination-file-key" &&
            x.FilePath == "source-file-path"));
    }
    
    [Fact]
    public async Task Publish_Command_Throws_When_Put_Fails()
    {
        //Arrange
        var dto = new MomoS3CommandDto()
            .SetPublishOptions(new MomoS3PublishCommandDto()
            {
                S3Client = S3Client,
                BucketName = "test-bucket-name",
                DestinationCompleteFileKey = "destination-file-key",
                SourceFilePath = "source-file-path"
            });
        
        S3Client.PutObjectAsync(Arg.Any<PutObjectRequest>()).Returns(Task.FromResult(new PutObjectResponse()
        {
            HttpStatusCode = HttpStatusCode.BadRequest
        }));

        var sut = CreateSut(MomoS3CommandType.Publish, dto);
        
        //Act
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.ExecuteAsync(CancellationToken.None));
    }

    #region private

    private MomoS3Command CreateSut(MomoS3CommandType type, MomoS3CommandDto dto)
    {
        return new MomoS3Command(type, dto);
    }

    #endregion
}