using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Momo.Expectations.SNS.Expectations;
using Momo.Validators;
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