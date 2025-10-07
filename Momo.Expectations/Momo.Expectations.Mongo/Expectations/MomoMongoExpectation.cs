using System.Text.Json.Nodes;
using Momo.Exceptions;
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

    public void Validate()
    {
        try
        {
            var mongoUrl = new MongoUrl(ConnectionString);
            var client = new MongoClient(mongoUrl);
            
            _ = client.GetDatabase(mongoUrl.DatabaseName ?? throw new ArgumentException("Connection string must contain database name"));
        }
        catch (Exception)
        {
            throw new MomoFileValidationException(nameof(ConnectionString), "Connection String is not a valid mongo connection string");
        }

        var someQueryIsNull = Match?.Any(c => c?.Query is null) ?? true;
        if (someQueryIsNull)
        {
            throw new MomoFileValidationException(nameof(Match), "Query cannot be null inside Matchers of Mongo Expectations. Please provide a query");
        }
    }

    public override string ToString() => ConnectionString;
}

public record MomoMongoQueryExpectation
{
    public JsonNode Query { get; set; }
    public JsonSchema Match { get; set; }
}