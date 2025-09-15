using Core.Models;

namespace Core.Tests.Models;

public class ArnTests
{
    [Theory]
    [InlineData("arn:aws:s3:::my-bucket", "aws", "s3", "", "", "my-bucket")]
    [InlineData("arn:aws:lambda:us-west-2:123456789012:function:my-function:123456789012", "aws", "lambda", "us-west-2", "123456789012", "my-function")]
    [InlineData("arn:aws:iam::123456789012:role/MyRole", "aws", "iam", "", "123456789012", "role/MyRole")]
    [InlineData("arn:aws-cn:sns:cn-north-1:123456789012:my-topic", "aws-cn", "sns", "cn-north-1", "123456789012", "my-topic")]
    public void ParseArn_Should_MapToCorrectFields(
        string inputArn,
        string expectedPartition,
        string expectedService,
        string expectedRegion,
        string expectedAccountId,
        string expectedResourceName)
    {
        // Act
        var result = Arn.ParseArn(inputArn);

        // Assert
        Assert.Equal(expectedPartition, result.Partition);
        Assert.Equal(expectedService, result.Service);
        Assert.Equal(expectedRegion, result.Region);
        Assert.Equal(expectedAccountId, result.AccountId);
        Assert.Equal(inputArn, result.ResourceArn);
        Assert.Equal(expectedResourceName, result.ResourceName);
    }
}