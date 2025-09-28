using System.Text.Json;
using System.Text.Json.Serialization;
using Momo.Models;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Cli.FileLoader;

public static class MomoFileLoader
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters =
        {
            new IMomoExpectationConverter(),
            new JsonSchemaConverter(Formatting.Indented),
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
    
    public static async Task<MomoExpectationFile> LoadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Input file was not found at path: {path}");

        var content = await File.ReadAllTextAsync(path);

        var result = JsonSerializer.Deserialize<MomoExpectationFile>(content, _jsonOptions);

        if (result is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        return result;
    }
    
    public static async Task SaveAsync(MomoExpectationFile file, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Input file was not found at path: {path}");

        var serializedFile = JsonSerializer.Serialize(file, _jsonOptions);

        if (serializedFile is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        await File.WriteAllTextAsync(path, serializedFile);
    }
}