using System.Collections.Concurrent;
using Momo.Exceptions;
using Momo.Steps;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Momo.Expectations.Mongo.Steps;

public class MomoMongoStepHandler: IStepHandler
{
    private IMongoDatabase? _mongoDatabase;

    public ValueTask DisposeAsync()
    {
        _mongoDatabase?.Client.Dispose();
        return ValueTask.CompletedTask;
    }

    public Task PrepareAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        if (baseConfig is not Expectations.MomoMongoExpectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(baseConfig)} is invalid, expected MomoDatabaseException with matchers");
        }
        
        var mongoUrl = new MongoUrl(config.ConnectionString);
        var client = new MongoClient(mongoUrl);
        _mongoDatabase = client.GetDatabase(mongoUrl.DatabaseName ?? throw new ArgumentException("Connection string must contain database name"));

        return Task.CompletedTask;
    }

    public async Task<bool> CheckAsync(IMomoExpectation baseConfig, int timeout, CancellationToken cancellationToken)
    {
        if (_mongoDatabase is null)
        {
            throw new InvalidOperationException("MongoDB is not initialized, please call {nameof(PrepareAsync)} before calling {nameof(CheckAsync)}");
        }
        
        var config = (Expectations.MomoMongoExpectation)baseConfig;
        ConcurrentDictionary<string, string> executedQueriesWithResults = [];

        foreach (var query in config.Match)
        {
            //execute query
            var queryBsonResult = await TryExecutingQuery(cancellationToken, _mongoDatabase, query);
            var resultInJson = queryBsonResult["cursor"].ToJson();
            
            executedQueriesWithResults[query.Query.ToString()] = resultInJson;

            //Try match all the query results
            if (!QueryMatches(resultInJson, query.Match))
            {
                throw new AssertException($"Query did not match", 
                    new AssertException($"Executed queries {Newtonsoft.Json.JsonConvert.SerializeObject(executedQueriesWithResults)}"));
            }
        }

        return true;
    }

    public Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<BsonDocument> TryExecutingQuery(CancellationToken cancellationToken, IMongoDatabase database,
        Expectations.MomoMongoQueryExpectation query)
    {
        try
        {
            var queryBsonResult = await database.RunCommandAsync<BsonDocument>(query.Query.ToString(), cancellationToken: cancellationToken);
            return queryBsonResult;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Query \"{query.Query}\" is invalid: please double check your query field", ex);
        }
    }

    private bool QueryMatches(string queryJson, Dictionary<string, string> matchRules)
    {
        if (matchRules is null)
        {
            throw new Exception("Query match rules are empty", new ArgumentNullException(nameof(matchRules)));
        }
        
        var root = JObject.Parse(queryJson);
        root["documents"] = root["firstBatch"]; //easier to query...
        
        foreach (var kvp in matchRules)
        {
            var token = root.SelectToken(kvp.Key, new JsonSelectSettings() { ErrorWhenNoMatch = true });
                
            if (token == null || !string.Equals(token.ToString(), kvp.Value, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}