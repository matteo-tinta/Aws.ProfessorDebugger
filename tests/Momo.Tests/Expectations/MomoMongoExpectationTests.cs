using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo.Expectations;
using Momo.Models;
using Momo.Steps;
using NSubstitute;

namespace Momo.Tests.Expectations;

public class MomoMongoExpectationTests
{
    private MomoClientFactoryOptions _options = new()
    {
        ExpectationFile = null,
        s3Client = Substitute.For<IAmazonS3>(),
        snsClient = Substitute.For<IAmazonSimpleNotificationService>(),
        sqsClient = Substitute.For<IAmazonSQS>()
    };
    
    [Test]
    [TestCase(typeof(MongoStepHandler))]
    public void GetStepHandler_Returns_TheCorrectStepHandler(Type expectedType)
    {
        //Arrange
        var expectation = new MomoMongoExpectation()
        {
            ConnectionString = "mongodb://testuser:testpass@localhost:27017/testdb"
        };

        //Act
        var stepHandler = expectation.GetStepHandler(_options);

        //Assert
        Assert.That(stepHandler, Is.TypeOf(expectedType));
    }

    [Test]
    [TestCase("mongo://:@localhost:27017/")]
    [TestCase("")]
    [TestCase(null)]
    public void GetStepHandler_Throws_WhenConnectionStringIsNotAMongoOnes(string? connectionString)
    {
        //Arrange
        var expectation = new MomoMongoExpectation()
        {
            ConnectionString = connectionString!
        };

        //Act
        Assert.Throws<InvalidOperationException>(() => expectation.GetStepHandler(_options),
            "Not a mongo connection string");
    }
}