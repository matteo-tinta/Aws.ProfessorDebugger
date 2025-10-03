using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Momo.Expectations.SNS.Expectations;
using Momo.Expectations.SNS.Steps;
using Momo.Expectations.SNS.Tests.Test.Builders;
using Momo.Expectations.SNS.Tests.Test.Extensions;
using Momo.Models;
using NJsonSchema;
using NSubstitute;

namespace Momo.Expectations.SNS.Tests.Steps;

[SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method")]
public class MomoAwsSnsStepHandlerTests
{
    private IAmazonSQS _sqs;
    private IAmazonSimpleNotificationService _sns;
    private ModelBuilder<MomoClientFactoryOptions> _options;
    private ModelBuilder<MomoAwsSnsExpectation> _expectation;

    [SetUp]
    public void Setup()
    {
        _sqs = Substitute.For<IAmazonSQS>();
        _sns = Substitute.For<IAmazonSimpleNotificationService>();

        _expectation = SNSExpectationBuilder.Generate(_sqs, _sns);
        _options = new ModelBuilder<MomoClientFactoryOptions>(() => new()
        {
            ExpectationFile = new MomoExpectationFile()
            {
                Expectations = [_expectation.Build()],
                Timeout = 600,
                TraceId = "1235"
            }
        });
    }
    
    [Theory]
    [TestCase("string", JsonObjectType.String)]
    [TestCase(33, JsonObjectType.Integer)]
    [TestCase(33.333, JsonObjectType.Number)]
    [TestCase("2025-10-03T14:32:00Z", JsonObjectType.String, "date-time")]
    [TestCase("2025-10-03", JsonObjectType.String, "date")]
    public async Task Generating_A_float_number_should_generate_the_correct_schema(object value, JsonObjectType expectedType, string? format = null)
    {
        //Arrange
        _sqs.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .WithMessages(new { this_is_float = value });

        var expectation = _expectation.Build();
        var stepHandler = expectation.GetStepHandler(_options.Build());

        //Act
        var result = await stepHandler.GenerateExpectationAsync(expectation, TestContext.CurrentContext.CancellationToken) as MomoAwsSnsExpectation;
        
        //Assert
        result!.Match.Properties["this_is_float"].ShouldMatchTypeAndFormat(expectedType, format);
    }
}