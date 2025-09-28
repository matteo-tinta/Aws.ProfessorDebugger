using System.Collections.Concurrent;
using Momo.Exceptions;
using Momo.Expectations.Mongo.Expectations;
using Momo.Steps;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Momo.Expectations.Mongo.Steps;

public class MomoMongoStepHandler: IStepHandler
{
    private IMongoDatabase? _mongoDatabase;
    private ConcurrentDictionary<string, string> _executedQueriesWithResults = [];

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
        
        await ExecuteQueryAsync(
            baseConfig, (result, expectation) =>
            {
                if (!QueryMatches(result, expectation.Match))
                {
                    throw new AssertException($"Query did not match",
                        new AssertException($"Executed queries {Newtonsoft.Json.JsonConvert.SerializeObject(_executedQueriesWithResults)}"));
                }

                return true;
            }, cancellationToken);

        return true; //avoid cycling...
    }
    
    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        await PrepareAsync(baseConfig, cancellationToken);
        
        var config = (MomoMongoExpectation)baseConfig;

        //add "limit" as 1, to limit result to 1 output only
        var newConfig = new MomoMongoExpectation()
        {
            ConnectionString = config.ConnectionString,
            Match = config.Match.Select(m =>
            {
                var query = m.Query;
                query["limit"] = 1;

                return m with
                {
                    Query = query
                };
            }).ToList()
        };
        
        var results = await ExecuteQueryAsync(newConfig, 
            (result, expectation) => expectation with { Match = JsonSchema.FromSampleJson(result) }, 
            cancellationToken);

        return new MomoMongoExpectation()
        {
            ConnectionString = config.ConnectionString,
            Match = results,
        };
    }

    #region private

    private async Task<List<TOut>> ExecuteQueryAsync<TOut>(
        IMomoExpectation baseConfig, 
        Func<string, MomoMongoQueryExpectation, TOut> OnQueryResult,
        CancellationToken cancellationToken)
    {
        if (_mongoDatabase is null)
        {
            throw new InvalidOperationException("MongoDB is not initialized, please call {nameof(PrepareAsync)} before calling {nameof(CheckAsync)}");
        }
        
        var config = (MomoMongoExpectation)baseConfig;
        
        List<TOut> results = [];
        foreach (var query in config.Match)
        {
            //execute query
            var queryBsonResult = await TryExecutingQuery(cancellationToken, _mongoDatabase, query);
            var resultInJson = queryBsonResult["cursor"]["firstBatch"].ToJson();
            
            _executedQueriesWithResults[query.Query.ToString()] = resultInJson;
            
            results.Add(OnQueryResult(resultInJson, query));
        }

        return results;
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

    private bool QueryMatches(string queryJson, JsonSchema schema)
    {
        if (schema is null)
        {
            throw new Exception("Query match rules are empty", new ArgumentNullException(nameof(schema)));
        }
        
        var validationErrors = schema.Validate(queryJson);
        return validationErrors.Count == 0;
    }

    #endregion
    
}