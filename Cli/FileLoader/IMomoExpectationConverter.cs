using System.Text.Json;
using Cli.FileLoader.Models;
using Momo.Expectations;
using Momo.Expectations.Mongo.Expectations;
using Momo.Expectations.Parallel.Expectations;
using Momo.Expectations.S3.Expectations;
using Momo.Expectations.SNS.Expectations;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Cli.FileLoader;

public class IMomoExpectationConverter : System.Text.Json.Serialization.JsonConverter<IMomoExpectation>
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
        
        if (root.TryGetProperty("arn", out JsonElement arn))
        {
            if (arn.ToString().Contains(":sns:"))
            {
                var model = JsonSerializer.Deserialize<MomoAwsSnsExpectationJsonModel>(root.GetRawText(), options);
                return model?.Build() ?? throw new InvalidOperationException("Invalid sns mapping");
            }
            
            if (arn.ToString().Contains(":s3:"))
            {
                var model = JsonSerializer.Deserialize<MomoAwsS3ExpectationJsonModel>(root.GetRawText(), options);
                return model?.Build() ?? throw new InvalidOperationException("Invalid s3 mapping");
            }

            if (arn.ToString().Contains(":sqs:"))
            {
                throw new InvalidOperationException(
                    "To match SQS queues, provide its SNS. If no SNS are available, skip the node and check downstream resources (eg. Lambdas, S3)");
            }

            throw new InvalidOperationException($"This type of arn ({arn}) is not recognized yet");
        }
        
        if (root.TryGetProperty("parallelExpectations", out _))
        {
            return JsonSerializer.Deserialize<MomoParallelExpectation>(root.GetRawText(), options);
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
            case MomoAwsSnsExpectation dbExp:
                JsonSerializer.Serialize(writer, dbExp, options);
                break;
            case MomoAwsS3Expectation dbExp:
                JsonSerializer.Serialize(writer, dbExp, options);
                break;
            case MomoParallelExpectation exp:
                JsonSerializer.Serialize(writer, exp, options);
                break;
            default:
                throw new JsonException("Unknown IMomoExpectation implementation.");
        }
    }
}