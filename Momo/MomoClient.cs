using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Models;
using Momo.Models;
using Momo.Steps;
using Momo.Steps.Decorations;

namespace Momo;

public class MomoClient
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly MomoExpectationFile _expectationFile;

    private MomoClient(
        IAmazonSQS sqsClient,
        IAmazonSimpleNotificationService snsClient,
        MomoExpectationFile expectationFile)
    {
        _sqsClient = sqsClient;
        _snsClient = snsClient;
        _expectationFile = expectationFile;
    }

    public async Task MatchExpectations(CancellationToken cancellationToken)
    {
        foreach (var expectation in _expectationFile.Expectations)
        {
            var step = GetStepHandler(expectation);
            var result = await step.WaitForMatchAsync(expectation, _expectationFile.Timeout, cancellationToken);
            if (!result)
            {
                throw new Exception($"Step {expectation.Arn} failed");
            }
        }
    }

    private IStepHandler GetStepHandler(MomoExpectation expectation)
    {
        var arn = Arn.ParseArn(expectation.Arn);

        return arn.Service switch
        {
            "sns" => new StepHandlerLoggingDecorated(new SnsStepHandler(_sqsClient, _snsClient)),
            "sqs" => throw new InvalidOperationException("To match SQS queues, provide its SNS. If no SNS are available, skip the node and check downstream resources (eg. Lambdas, S3)"),
            _ => throw new InvalidOperationException("This type of arn is not recognized yet")
        };
    }
    
    public static MomoClient ValidateAndCreate(
        IAmazonSQS sqsClient,
        IAmazonSimpleNotificationService snsClient,
        MomoExpectationFile expectationFile)
    {
        if (expectationFile.Timeout > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(expectationFile.Timeout), "Must be >= 0 and <= 20, if provided");
        }
        
        return new MomoClient(
            sqsClient,
            snsClient,
            expectationFile);
    }
}