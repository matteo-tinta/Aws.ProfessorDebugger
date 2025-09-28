using System.Text.Json;
using System.Text.Json.Nodes;
using Momo.Expectations.Mongo.Steps;
using Momo.Steps;
using MongoDB.Driver;
using NJsonSchema;

namespace Momo.Expectations.Mongo.Expectations;

public class MomoMongoExpectation: IMomoExpectation
{
    public string ConnectionString { get; set; }
    public List<MomoMongoQueryExpectation> Match { get; set; }

    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        try
        {
            _ = new MongoUrl(ConnectionString);
            return new MomoMongoStepHandler();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Not a mongo connection string", e);
        }
    }

    public override string ToString() => ConnectionString;
}

public record MomoMongoQueryExpectation
{
    public JsonNode Query { get; set; }
    public JsonSchema Match { get; set; }
}