using System.Text.Json;
using Momo.Models;

namespace Cli.FileLoader;

public static class MomoFileLoader
{
    public static async Task<MomoExpectationFile> LoadAsync(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Input file was not found at path: {path}");

        var content = await File.ReadAllTextAsync(path);
        
        var options = new JsonSerializerOptions
        {
            Converters = { new IMomoExpectationConverter() },
            PropertyNameCaseInsensitive = true
        };

        var result = JsonSerializer.Deserialize<MomoExpectationFile>(content, options);

        if (result is null)
            throw new InvalidDataException("Failed to deserialize input file into correct format. Check readme");

        return result;
    }
}