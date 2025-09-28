using System.Text.Json;
using Newtonsoft.Json;
using NJsonSchema;

namespace Cli.FileLoader;

public class JsonSchemaConverter(Formatting formatting = Formatting.None) : System.Text.Json.Serialization.JsonConverter<JsonSchema>
{
    public override JsonSchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var rawJson = jsonDoc.RootElement.GetRawText();
        return JsonSchema.FromJsonAsync(rawJson).GetAwaiter().GetResult();
    }

    public override void Write(Utf8JsonWriter writer, JsonSchema value, JsonSerializerOptions options)
    {
        // Get raw JSON as string
        var jsonString = value.ToJson(formatting);
        using var document = JsonDocument.Parse(jsonString);
        document.RootElement.WriteTo(writer);
    }
}