using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Momo.Expectations.SNS.Expectations;
using Momo.Validators;

namespace Cli.FileLoader.Models;

public class MomoAwsSnsExpectationJsonModel
{
    public string Arn { get; set; }
    public Dictionary<string, ValueJsonExpectation> Match { get; set; }

    public MomoAwsSnsExpectation Build()
    {
        var sqsClient = Services.Provider.GetRequiredService<IAmazonSQS>();
        var snsClient = Services.Provider.GetRequiredService<IAmazonSimpleNotificationService>();
        
        return new MomoAwsSnsExpectation(sqsClient,snsClient)
        {
            Arn = Arn,
            Match = Match.ToDictionary(c => c.Key,  c => new Value()
            {
                Match = c.Value.Match is not null ? new Regex(c.Value.Match) : null,
                Contains = c.Value.Contains,
                Gt = c.Value.Gt,
                Ls = c.Value.Ls,
                ValueEquals = c.Value.Convert(c.Value.ValueEquals)
            })
        };
    }
}

public record ValueJsonExpectation
{
    public string? Contains { get; set; }
    
    public int? Gt { get; set; }
    
    public int? Ls { get; set; }
    
    public string? Match { get; set; }
    
    [JsonPropertyName("equals")]
    public JsonElement ValueEquals { get; set; }
    
    public object? Convert(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String  => element.GetString(),
        JsonValueKind.Number  => element.GetInt64(),
        JsonValueKind.True    => true,
        JsonValueKind.False   => false,
        JsonValueKind.Null    => null,
        JsonValueKind.Undefined => null,
        _ => element.GetRawText() // fallback (arrays/objects stay as JSON text)
    };
}