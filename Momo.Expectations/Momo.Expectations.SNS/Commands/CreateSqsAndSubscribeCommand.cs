using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Momo.Commands;

namespace Momo.Expectations.SNS.Commands;

internal class CreateSqsAndSubscribeCommand(
    IAmazonSQS sqsClient,
    IAmazonSimpleNotificationService snsClient,
    string queueName,
    string snsTopicArn)
    : ICommand
{
    private string? _queueUrl;
    private string? _subscriptionArn;
    private bool _disposed = false;

    internal string? GetQueueUrl() => _queueUrl;
    internal string? GetSubscriptionArn() => _subscriptionArn;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            throw new InvalidOperationException("This command has been already disposed");
        }
        
        // 1. Create the SQS queue
        var createQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest
        {
            QueueName = queueName
        }, cancellationToken);

        _queueUrl = createQueueResponse.QueueUrl;

        // 2. Get the ARN of the queue
        var attrs = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = _queueUrl,
            AttributeNames = ["QueueArn"]
        }, cancellationToken);

        var queueArn = attrs.Attributes["QueueArn"];

        // 3. Subscribe the queue to the SNS topic
        var subscribeResponse = await snsClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = snsTopicArn,
            Protocol = "sqs",
            Endpoint = queueArn
        }, cancellationToken);

        _subscriptionArn = subscribeResponse.SubscriptionArn;

        // 4. Allow SNS to publish to the queue (via queue policy)
        var policy = $$"""
                       {
                                   "Version":"2012-10-17",
                                   "Statement":[
                                       {
                                           "Effect":"Allow",
                                           "Principal":{"Service":"sns.amazonaws.com"},
                                           "Action":"sqs:SendMessage",
                                           "Resource":"{{queueArn}}",
                                           "Condition":{
                                               "ArnEquals":{"aws:SourceArn":"{{snsTopicArn}}"}
                                           }
                                       }
                                   ]
                               }
                       """;

        await sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = _queueUrl,
            Attributes = new Dictionary<string, string>
            {
                { "Policy", policy }
            }
        }, cancellationToken);
    }

    public async Task UndoAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }
        
        if (!string.IsNullOrEmpty(_subscriptionArn))
        {
            try
            {
                await snsClient.UnsubscribeAsync(_subscriptionArn, cancellationToken);
            }
            catch (NotFoundException)
            {
                //Ignored (subscription does not exist or already disposed)
            }
        }

        if (!string.IsNullOrEmpty(_queueUrl))
        {
            try
            {
                await sqsClient.DeleteQueueAsync(_queueUrl, cancellationToken);
            }
            catch (QueueDoesNotExistException)
            {
                //Ignored (queue does not exist or already disposed)
            }
        }

        _disposed = true;
    }
}