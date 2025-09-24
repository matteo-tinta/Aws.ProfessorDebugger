using System.Text.Json;
using Momo.Steps;
using MongoDB.Driver;

namespace Momo.Expectations;

public class MomoMongoExpectation: IMomoExpectation
{
    public string ConnectionString { get; set; }
    public List<MomoMongoQueryExpectation> Match { get; set; }

    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        try
        {
            _ = new MongoUrl(ConnectionString);
            return new MongoStepHandler();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Not a mongo connection string", e);
        }
    }
}

public record MomoMongoQueryExpectation
{
    public JsonElement Query { get; set; }
    public Dictionary<string, string> Match { get; set; }

}