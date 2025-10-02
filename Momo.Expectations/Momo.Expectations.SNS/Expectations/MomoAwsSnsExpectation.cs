using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Momo.Exceptions;
using Momo.Expectations.SNS.Steps;
using Momo.Steps;
using NJsonSchema;
using ResourceArn = Models.Arn;

namespace Momo.Expectations.SNS.Expectations;

public class MomoAwsSnsExpectation(IAmazonSQS sqsClient, IAmazonSimpleNotificationService snsClient) : IMomoExpectation
{
    public required string Arn { get; set; }
    public required JsonSchema Match { get; set; }
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        var service = ResourceArn.ParseArn(Arn).Service;

        return service.ToLower().Trim() switch
        {
            "sns" => new MomoAwsSnsStepHandler(sqsClient, snsClient),
            _ => throw new InvalidOperationException($"This type of arn ({Arn} -> {service}) is not recognized yet")
        };
    }

    public void Validate()
    {
        _ = !ResourceArn.ParseArn(Arn).Service.Equals("sns", StringComparison.CurrentCultureIgnoreCase) 
            ? throw new MomoFileValidationException(nameof(Arn), "Arn was invalid. Only SNS is allowed for SNS blocks") 
            : true;
    }

    public override string ToString() => Arn;
}