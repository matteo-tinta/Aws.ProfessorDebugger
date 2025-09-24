using System.Text.Json;
using Momo.Steps;

namespace Momo.Expectations;

public class MomoMongoExpectation: IMomoExpectation
{
    public string ConnectionString { get; set; }
    public List<MomoMongoQueryExpectation> Match { get; set; }
    
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options) 
        => new MongoStepHandler();
}

public record MomoMongoQueryExpectation
{
    public JsonElement Query { get; set; }
    public Dictionary<string, string> Match { get; set; }

}