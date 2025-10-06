using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo.Expectations.SNS.Expectations;
using Tests.Shared.Builders;

namespace Momo.Expectations.SNS.Tests.Test.Builders;

internal static class SNSExpectationBuilder
{
    internal static ModelBuilder<MomoAwsSnsExpectation> Generate(IAmazonSQS sqs, IAmazonSimpleNotificationService sns)
    {
        return new ModelBuilder<MomoAwsSnsExpectation>(() => new MomoAwsSnsExpectation(sqs, sns)
        {
            Arn = "arn:aws:sns:us-east-1:123456789012:my-topic-name",
            Match = null
        });
    }
}