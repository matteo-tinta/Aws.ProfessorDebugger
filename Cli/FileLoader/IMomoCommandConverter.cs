using System.Text.Json;
using Cli.FileLoader.Models;
using Cli.FileLoader.Models.Commands;
using Momo.Commands.Rest;
using Momo.Expectations;
using Momo.Expectations.Mongo.Expectations;
using Momo.Expectations.Parallel.Expectations;
using Momo.Expectations.S3.Expectations;
using Momo.Expectations.SNS.Expectations;
using Momo.Models;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Cli.FileLoader;

public class IMomoCommandConverter(string filePath) : System.Text.Json.Serialization.JsonConverter<MomoClientCommand>
{
    private string CommandJson { get; set; }
    
    public override MomoClientCommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        // Heuristic: Decide which type based on presence of specific properties
        if (!root.TryGetProperty("_type", out JsonElement type) && type.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("'_type' is mandatory and must be a string in command to identify the type of command.");
        }
        
        var assembly = typeof(IMomoCommandBuildable).Assembly;
        
        var commandTypes = assembly.GetTypes()
            .Where(t => typeof(IMomoCommandBuildable).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => new
            {
                Type = t,
                Attribute = t.GetCustomAttributes(typeof(MomoRestCommandTypeAttribute), false)
                    .Cast<MomoRestCommandTypeAttribute>()
                    .FirstOrDefault()
            })
            .Where(x => x.Attribute != null)
            .ToDictionary(x => x.Attribute!.Type, x => x.Type, StringComparer.Ordinal);

        // Get the _type string from JSON
        var typeName = type.GetString();

        // Check if the _type matches a registered command
        if (!commandTypes.TryGetValue(typeName ?? "", out var concreteType))
            throw new JsonException("Unknown IMomoCommand implementation.");
        
        
        CommandJson = root.GetRawText();
        var undoCommandJson = root.TryGetProperty("_undo", out JsonElement undoCommandElement) 
            ? undoCommandElement.GetRawText()
            : null;
            
        var command = JsonSerializer.Deserialize(CommandJson, concreteType, options) as IMomoCommandBuildable;
        var undoCommand = undoCommandJson is not null ? JsonSerializer.Deserialize(undoCommandJson, concreteType, options) : null;

        var cleanFilePath = Path.Join(filePath, "..");
        
        return command switch
        {
            MomoS3CommandJsonModel s3CommandJsonModel => new MomoClientCommand()
            {
                Command = s3CommandJsonModel.Build(cleanFilePath) ?? throw new JsonException("Format is unexpected."),
                Undo = (undoCommand as MomoS3CommandJsonModel)?.Build(cleanFilePath)
            },
            MomoRestCommandJsonModel restCommandJsonModel => new MomoClientCommand()
            {
                Command = restCommandJsonModel.Build() ?? throw new JsonException("Format is unexpected."),
                Undo = (undoCommand as MomoRestCommandJsonModel)?.Build()
            },
            _ => throw new ArgumentOutOfRangeException(typeName, new InvalidOperationException("Not recognized"))
        };
    }
    public override void Write(Utf8JsonWriter writer, MomoClientCommand value, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.Parse(CommandJson);
        doc.RootElement.WriteTo(writer);
    }
}