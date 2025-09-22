using System.Text.Json;
using System.Text.Json.Serialization;
using Momo.Models;

namespace Cli.FileLoader;

public class IMomoExpectationConverter : JsonConverter<IMomoExpectation>
{
    public override IMomoExpectation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        // Heuristic: Decide which type based on presence of specific properties
        if (root.TryGetProperty("connectionString", out _))
        {
            return JsonSerializer.Deserialize<MomoMongoExpectation>(root.GetRawText(), options);
        }
        else if (root.TryGetProperty("arn", out _))
        {
            return JsonSerializer.Deserialize<MomoAwsExpectation>(root.GetRawText(), options);
        }

        throw new JsonException("Unknown IMomoExpectation implementation.");
    }

    public override void Write(Utf8JsonWriter writer, IMomoExpectation value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case MomoMongoExpectation dbExp:
                JsonSerializer.Serialize(writer, dbExp, options);
                break;
            case MomoAwsExpectation exp:
                JsonSerializer.Serialize(writer, exp, options);
                break;
            default:
                throw new JsonException("Unknown IMomoExpectation implementation.");
        }
    }
}