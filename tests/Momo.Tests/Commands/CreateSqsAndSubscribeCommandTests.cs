using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Momo.Commands;
using NSubstitute;
using NSubstitute.Extensions;

namespace Momo.Tests.Commands;

[SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method")]
public class CreateSqsAndSubscribeCommandTests
{
    private IAmazonSQS _sqsClient;
    private IAmazonSimpleNotificationService _snsClient;

    [SetUp]
    public void Setup()
    {
        _sqsClient = Substitute.For<IAmazonSQS>();
        _snsClient = Substitute.For<IAmazonSimpleNotificationService>();
    }
    
    [Test]
    public async Task CommandCreateQueryAndSubscription()
    {
        //Arrange
        ConfigureDependencies(out var topicArn, out var subscriptionArn, out var queueName, out var queueUrl, out var expectedPolicy);

        var sut = new CreateSqsAndSubscribeCommand(
            _sqsClient, _snsClient, queueName, topicArn);
        
        //Act
        await sut.ExecuteAsync(TestContext.CurrentContext.CancellationToken);
        
        await Assert.MultipleAsync(async () =>
        {
            //Assert query url and subscriptions
            Assert.That(sut.GetQueueUrl(), Is.EqualTo(queueUrl));
            Assert.That(sut.GetSubscriptionArn(), Is.EqualTo(subscriptionArn));
            
            //Assert sqs received the updated policies
            await _sqsClient.Received().SetQueueAttributesAsync(
                Arg.Is<SetQueueAttributesRequest>(req => req.QueueUrl == queueUrl && req.Attributes["Policy"] == expectedPolicy),
                TestContext.CurrentContext.CancellationToken);
        });

    }
    
    [Test]
    public async Task CommandCreateQueryAndSubscription_Undo_ShouldUndo()
    {
        //Arrange
        ConfigureDependencies(out var topicArn, out var subscriptionArn, out var queueName, out var queueUrl, out var expectedPolicy);

        var sut = new CreateSqsAndSubscribeCommand(
            _sqsClient, _snsClient, queueName, topicArn);
        
        //Act
        await sut.ExecuteAsync(TestContext.CurrentContext.CancellationToken);
        await sut.UndoAsync(TestContext.CurrentContext.CancellationToken);
        
        await Assert.MultipleAsync(async () =>
        {
            await _snsClient.Received().UnsubscribeAsync(subscriptionArn, TestContext.CurrentContext.CancellationToken);
            await _sqsClient.Received().DeleteQueueAsync(queueUrl, TestContext.CurrentContext.CancellationToken);
        });

    }

    #region 

    private void ConfigureDependencies(out string topicArn, out string subscriptionArn, out string queueName,
        out string queueUrl, out string expectedPolicy)
    {
        topicArn = "sns_topic_arn";
        subscriptionArn = $"arn:aws:subscription:us-east-1:111122223333:{topicArn}";
        queueName = "test_queue_name";
        string queueArn = $"arn:aws:sqs:us-east-1:111122223333:{queueName}";
        queueUrl = $"https://{queueName}";
        
        expectedPolicy = $$"""
                           {
                                       "Version":"2012-10-17",
                                       "Statement":[
                                           {
                                               "Effect":"Allow",
                                               "Principal":{"Service":"sns.amazonaws.com"},
                                               "Action":"sqs:SendMessage",
                                               "Resource":"{{queueArn}}",
                                               "Condition":{
                                                   "ArnEquals":{"aws:SourceArn":"{{topicArn}}"}
                                               }
                                           }
                                       ]
                                   }
                           """;


        string _topicArn = topicArn;
        string _queueName = queueName;
        string _queueUrl = queueUrl;
        
        _sqsClient.CreateQueueAsync(Arg.Is<CreateQueueRequest>(request => request.QueueName == _queueName))
            .Returns(new CreateQueueResponse()
            {
                QueueUrl = queueUrl
            });
        
        _sqsClient
            .GetQueueAttributesAsync(Arg.Is<GetQueueAttributesRequest>(request => 
                request.QueueUrl == _queueUrl &&
                request.AttributeNames.SequenceEqual(new List<string> { "QueueArn" })))
            .Returns(new GetQueueAttributesResponse()
            {
                Attributes = new Dictionary<string, string>()
                {
                    { "QueueArn", queueArn }
                }
            });

        _snsClient.SubscribeAsync(Arg.Is<SubscribeRequest>(req => 
                req.TopicArn == _topicArn && req.Protocol == "sqs" && req.Endpoint == queueArn))
            .Returns(new SubscribeResponse()
            {
                SubscriptionArn = subscriptionArn
            });
    }

    #endregion

    
}