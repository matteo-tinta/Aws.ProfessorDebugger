using System.Net;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using NSubstitute;

namespace Momo.Expectations.SNS.Tests.Test.Extensions;

internal static class SQSExtensions
{
    internal static void WithMessages(this Task<ReceiveMessageResponse> task, params object[] messages)
    {
        task.Returns(new ReceiveMessageResponse()
        {
            HttpStatusCode = HttpStatusCode.OK,
            Messages = messages.Select(m => new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = JsonSerializer.Serialize(new { Message = m })
            }).ToList()
        });
    }
}