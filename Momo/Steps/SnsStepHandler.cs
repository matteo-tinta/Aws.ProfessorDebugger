using System.Collections.Concurrent;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Models;
using Momo.Commands;
using Momo.Exceptions;
using Momo.Helpers;
using Momo.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Momo.Steps;

internal class SnsStepHandler(
    IAmazonSQS sqsClient,
    IAmazonSimpleNotificationService snsClient)
    : IStepHandler
{
    private readonly IAmazonSQS _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    private readonly IAmazonSimpleNotificationService _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
    private string? _queueUrl;

    public async Task<bool> WaitForMatchAsync(IMomoExpectation baseConfig, int timeout, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoAwsExpectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(S3StepHandler)} is invalid, expected MomoExpectation");
        }
        
        if (config.Arn == null)
            throw new ArgumentException("SNS Arn must be provided.");

        var arn = Arn.ParseArn(config.Arn);
        
        if(arn.Service != "sns")
            throw new ArgumentException("Invalid SNS Arn.");

        var command = new CreateSqsAndSubscribeCommand(
            _sqsClient,
            _snsClient,
            queueName: $"momo-debug-queue-{new DateTime().Ticks}",
            snsTopicArn: arn.ResourceArn
        );
        
        try
        {
            await command.ExecuteAsync(cancellationToken);

            _queueUrl = command.GetQueueUrl();
            ConcurrentDictionary<string, Message> checkedMessages = [];

            return await RetryHelper.RetryAsync(
                async () =>
                {
                    var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = timeout > 20 ? 20 : timeout,
                        MessageAttributeNames = ["All"],
                        VisibilityTimeout = 1
                    }, cancellationToken);

                    foreach (var message in response.Messages ?? [])
                        checkedMessages[message.MessageId] = message;
                    
                    if (response.Messages is not null && response.Messages.Any(message => MessageMatches(message.Body, config.Match)))
                    {
                        return true;
                    }
                                        
                    throw new AssertException($"No messages were matched on SQS {command.GetQueueUrl()} on Subscription {command.GetSubscriptionArn()}", 
                        new AssertException($"Current expectation failed:\n{JsonConvert.SerializeObject(config, Formatting.Indented)}", 
                            new AssertException($"Checked messages:\n{JsonConvert.SerializeObject(checkedMessages, Formatting.Indented)}")));
                }, TimeSpan.FromSeconds(timeout), cancellationToken);
        }
        finally
        {
            await command.UndoAsync(cancellationToken);
        }
    }
    
    private bool MessageMatches(string messageBody, Dictionary<string, string> matchRules)
    {
        try
        {
            var root = JObject.Parse(messageBody);

            var messageContent = root["Message"]!.ToString();
            var parsedMessage = JToken.Parse(messageContent);
            root["Message"] = parsedMessage;
            
            foreach (var kvp in matchRules)
            {
                var token = root.SelectToken(kvp.Key);
                
                //Cannot assume here that the message we are reading at this moment is the message we want to read
                //so false is returned, message did not match.
                //In the exception output there will be all the messages that are being received during this time
                if (token == null || !string.Equals(token.ToString(), kvp.Value, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        catch (Exception e)
        {
            throw MessageAssertException.CreateExceptionWithMessageBody(
                "Message was malformed thus cannot be checked",
                messageBody, e);
        }
    }
}