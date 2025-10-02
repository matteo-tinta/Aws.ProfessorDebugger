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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
    
    public static async Task<MomoExpectationFile> LoadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Input file was not found at path: {GetFullPath(path)}");

        var content = await File.ReadAllTextAsync(path);

        var result = JsonSerializer.Deserialize<MomoExpectationFile>(content, _jsonOptions);

        if (result is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        return result;
    }
    
    public static void Print(MomoExpectationFile file)
    {
        var serializedFile = JsonSerializer.Serialize(file, _jsonOptions);

        if (serializedFile is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        Console.WriteLine(serializedFile);
    }
    
    public static async Task SaveAsync(MomoExpectationFile file, string path)
    {
        var serializedFile = JsonSerializer.Serialize(file, _jsonOptions);

        if (serializedFile is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        await File.WriteAllTextAsync(path, serializedFile);
    }
    
    private static string GetFullPath(string userPath)
    {
        if (Path.IsPathRooted(userPath))
        {
            return Path.GetFullPath(userPath);
        }

        var exeDir = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(exeDir, userPath));
    }
}