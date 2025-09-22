using System.Text.Json;

namespace Momo.Models;

public interface IMomoExpectation { }

public record MomoAwsExpectation: IMomoExpectation
{
    public string Arn { get; set; }
    public Dictionary<string, string> Match { get; set; }
}

public record MomoMongoQueryExpectation: IMomoExpectation
{
    public JsonElement Query { get; set; }
    public Dictionary<string, string> Match { get; set; }
}

public record MomoMongoExpectation: IMomoExpectation
{
    public string ConnectionString { get; set; }
    public List<MomoMongoQueryExpectation> Match { get; set; }
}