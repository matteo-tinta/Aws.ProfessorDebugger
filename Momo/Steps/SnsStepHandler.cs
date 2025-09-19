using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Models;
using Momo.Commands;
using Momo.Exceptions;
using Momo.Models;
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

    public async Task<bool> WaitForMatchAsync(MomoExpectation config, int timeout, CancellationToken cancellationToken)
    {
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
            
            var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = timeout,
                MessageAttributeNames = ["All"],
                VisibilityTimeout = 1
            }, cancellationToken);

            if (response.Messages is not null && response.Messages.Count > 0)
            {
                return response.Messages.Any(message => MessageMatches(message.Body, config.Match));
            }
            
            throw new AssertException($"No messages were found on SQS {command.GetQueueUrl()} on Subscription {command.GetSubscriptionArn()}");
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
                if (token == null)
                    throw new AssertException($"Token {kvp.Key} was not found in expected path", 
                        MessageAssertException.CreateExceptionWithMessageBody("Message was invalid", messageBody));

                if (!string.Equals(token.ToString(), kvp.Value, StringComparison.OrdinalIgnoreCase))
                    throw new AssertException($"Token {kvp.Key} was found in expectation, but actual value {kvp.Value} didn't matched expectation {token}", 
                        MessageAssertException.CreateExceptionWithMessageBody("Message was invalid", messageBody));
            }

            return true;
        }
        catch (AssertException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw MessageAssertException.CreateExceptionWithMessageBody(
                "Message was malformed thus cannot be checked",
                messageBody, e);
        }
    }
}