using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Momo.Expectations.SNS.Expectations;
using NJsonSchema;

namespace Cli.FileLoader.Models;

public class MomoAwsSnsExpectationJsonModel
{
    public string Arn { get; set; }
    public JsonElement Match { get; set; }

    public MomoAwsSnsExpectation Build()
    {
        var sqsClient = Services.Provider.GetRequiredService<IAmazonSQS>();
        var snsClient = Services.Provider.GetRequiredService<IAmazonSimpleNotificationService>();
        
        return new MomoAwsSnsExpectation(sqsClient,snsClient)
        {
            Arn = Arn,
            Match = JsonSchema.FromJsonAsync(Match.ToString()).Result,
        };
    }
}