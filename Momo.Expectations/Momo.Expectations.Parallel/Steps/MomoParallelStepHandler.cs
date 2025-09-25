using Momo.Steps;

namespace Momo.Expectations.Parallel.Steps;

internal class MomoParallelStepHandler(MomoClientFactoryOptions options) : IStepHandler
{
    public Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> CheckAsync(IMomoExpectation config, int timeout, CancellationToken cancellationToken)
    {
        if (config is not Expectations.MomoParallelExpectation momoExpectation)
        {
            throw new InvalidOperationException("MomoParallelStepHandler must get a MomoParallelExpectation");
        }

        var tasks = momoExpectation
            .ParallelExpectations
            .Select(async c =>
            {
                //Creating a shadow new client, recursivelly, so that parallel reuse main logic
                options.ExpectationFile = options.ExpectationFile with { Expectations = [c] };
                
                var client = MomoClient.ValidateAndCreate(options);
                
                await client.MatchExpectations(cancellationToken);

                return true;
            })
            .ToList();
        
        var parallelResult = await Task.WhenAll(tasks);
        return parallelResult.All(x => x); //check if all are true, otherwise, return false
    }
    
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}