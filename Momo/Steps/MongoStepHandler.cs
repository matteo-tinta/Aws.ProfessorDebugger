using System.Collections.Concurrent;
using Momo.Exceptions;
using Momo.Helpers;
using Momo.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Momo.Steps;

public class MongoStepHandler: IStepHandler
{
    public MongoStepHandler()
    {
        
    }

    private IMongoDatabase CreateAndConnect(string connectionString)
    {
        var mongoUrl = new MongoUrl(connectionString);
        var client = new MongoClient(mongoUrl);
        return client.GetDatabase(mongoUrl.DatabaseName ?? throw new ArgumentException("Connection string must contain database name"));
    }

    public async Task<bool> WaitForMatchAsync(IMomoExpectation baseConfig, int timeout, CancellationToken cancellationToken)
    {
        if (baseConfig is not MomoMongoExpectation config)
        {
            throw new InvalidOperationException(
                $"type of config in {nameof(S3StepHandler)} is invalid, expected MomoDatabaseException with matchers");
        }

        var database = CreateAndConnect(config.ConnectionString);
        ConcurrentDictionary<string, string> executedQueriesWithResults = [];

        return await RetryHelper.RetryAsync(
            async () =>
            {
                foreach (var query in config.Match)
                {
                    //execute query
                    var queryBsonResult = await TryExecutingQuery(cancellationToken, database, query);
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
            },TimeSpan.FromSeconds(timeout), cancellationToken);
    }

    private async Task<BsonDocument> TryExecutingQuery(CancellationToken cancellationToken, IMongoDatabase database,
        MomoMongoQueryExpectation query)
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