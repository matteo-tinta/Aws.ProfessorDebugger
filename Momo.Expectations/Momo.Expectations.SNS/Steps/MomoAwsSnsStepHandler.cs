using System.Collections.Concurrent;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Models;
using Momo.Exceptions;
using Momo.Expectations.SNS.Commands;
using Momo.Expectations.SNS.Exceptions;
using Momo.Expectations.SNS.Expectations;
using Momo.Steps;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Momo.Expectations.SNS.Steps;

internal class MomoAwsSnsStepHandler(
    IAmazonSQS sqsClient,
    IAmazonSimpleNotificationService snsClient) : IStepHandler
{
    private readonly IAmazonSQS _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    private readonly IAmazonSimpleNotificationService _snsClient = snsClient ?? throw new ArgumentNullException(nameof(snsClient));
    private string? _queueUrl;
    private string? _subscriptionArn;
    private ConcurrentDictionary<string, Message> _checkedMessages = [];
    private CreateSqsAndSubscribeCommand _command;

    public async Task PrepareAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoAwsSnsExpectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(IMomoExpectation)} is invalid, expected MomoExpectation");
        }
        
        if (config.Arn == null)
            throw new ArgumentException("SNS Arn must be provided.");

        var arn = Arn.ParseArn(config.Arn);
        
        if(arn.Service != "sns")
            throw new ArgumentException("Invalid SNS Arn.");

        _command = new CreateSqsAndSubscribeCommand(
            _sqsClient,
            _snsClient,
            queueName: $"debug-momo-queue-{DateTime.UtcNow.Ticks}",
            snsTopicArn: arn.ResourceArn
        );

        await _command.ExecuteAsync(cancellationToken);

        _queueUrl = _command.GetQueueUrl();
        _subscriptionArn = _command.GetSubscriptionArn();
        _checkedMessages = new ConcurrentDictionary<string, Message>();
    }

    public async Task<bool> CheckAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        var config = (MomoAwsSnsExpectation)baseConfig;

        var messages = await CheckMessages(message => MessageMatches(message.ToString(), config.Match), cancellationToken);
        
        return messages.Any(c => c);
    }

    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        var config = (MomoAwsSnsExpectation)baseConfig;
        
        var schemas = await CheckMessages(message =>
        {
            var json = JsonConvert.SerializeObject(message, Formatting.None);
            return JsonSchema.FromSampleJson(json);
        }, cancellationToken);

        return new MomoAwsSnsExpectation(_sqsClient, _snsClient)
        {
            Arn = config.Arn,
            Match = schemas.ElementAt(0)
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _command.UndoAsync(CancellationToken.None);
    }

    private async Task<List<TOut>> CheckMessages<TOut>(
        Func<JToken, TOut> onMessageRead,
        CancellationToken cancellationToken)
    {
        var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 20,
            MessageAttributeNames = ["All"],
            VisibilityTimeout = 60
        }, cancellationToken);

        foreach (var message in response?.Messages ?? [])
            _checkedMessages[message.MessageId] = message;
        
        if (_checkedMessages.IsEmpty)
            throw new AssertException($"No messages were matched on SQS {_queueUrl} on Subscription {_subscriptionArn}");

        var list = new List<TOut>();
                
        foreach (var message in response?.Messages ?? [])
        {
            var root = JObject.Parse(message.Body);

            var messageContent = root["Message"]!.ToString();
            var parsedMessage = JToken.Parse(messageContent);
            root["Message"] = parsedMessage;

            if (root["Message"] is null)
                throw new JsonException("Message was empty");
                    
            list.Add(onMessageRead(root["Message"]));  
        }

        return list;
    } 
    
    private bool MessageMatches(string messageBody, JsonSchema schema)
    {
        try
        {
            var validationErrors = schema.Validate(messageBody);
            if (validationErrors.Count > 0)
            {
                throw new SchemaValidationException(validationErrors);
            }
            
            return validationErrors.Count == 0;
        }
        catch (Exception e)
        {
            throw MessageAssertException.CreateExceptionWithMessageBody("Message were not validated", messageBody, e);
        }
    }
}