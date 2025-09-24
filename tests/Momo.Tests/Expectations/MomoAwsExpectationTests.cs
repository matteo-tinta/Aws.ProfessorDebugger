using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo.Expectations;
using Momo.Models;
using Momo.Steps;
using NSubstitute;

namespace Momo.Tests.Expectations;

public class MomoAwsExpectationTests
{
    private MomoClientFactoryOptions _options = new MomoClientFactoryOptions
    {
        ExpectationFile = null,
        s3Client = Substitute.For<IAmazonS3>(),
        snsClient = Substitute.For<IAmazonSimpleNotificationService>(),
        sqsClient = Substitute.For<IAmazonSQS>()
    };
    
    [Test]
    [TestCase("arn:aws:s3:::my-test-bucket-123456", typeof(S3StepHandler))]
    [TestCase("arn:aws:sns:us-east-1:111122223333:my-test-topic", typeof(SnsStepHandler))]
    public void GetStepHandler_Returns_TheCorrectStepHandler(string arn, Type expectedType)
    {
        //Arrange
        var expectation = new MomoAwsExpectation()
        {
            Arn = arn
        };

        //Act
        var stepHandler = expectation.GetStepHandler(_options);

        //Assert
        Assert.That(stepHandler, Is.TypeOf(expectedType));
    }

    [Test]
    public void GetStepHandler_Throws_UnrecognizedArn()
    {
        //Arrange
        var expectation = new MomoAwsExpectation()
        {
            Arn = "arn:aws:lambda:us-east-1:111122223333:function:my-test-function"
        };

        //Act
        Assert.Throws<InvalidOperationException>(() => expectation.GetStepHandler(_options),
            $"This type of arn ({expectation.Arn} -> lambda) is not recognized yet");
    }
    
    [Test]
    public void GetStepHandler_Throws_WhenSqs()
    {
        //Arrange
        var expectation = new MomoAwsExpectation()
        {
            Arn = "arn:aws:sqs:us-east-1:111122223333:my-test-queue"
        };

        //Act
        Assert.Throws<InvalidOperationException>(() => expectation.GetStepHandler(_options),
            "To match SQS queues, provide its SNS. If no SNS are available, skip the node and check downstream resources (eg. Lambdas, S3)");
    }
}