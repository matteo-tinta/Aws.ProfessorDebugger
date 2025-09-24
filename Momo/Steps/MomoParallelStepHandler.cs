using Momo.Expectations;
using Momo.Models;
using Momo.Steps.Decorations;

namespace Momo.Steps;

internal class MomoParallelStepHandler(MomoClientFactoryOptions options) : IStepHandler
{
    public async Task<bool> WaitForMatchAsync(IMomoExpectation step, int timeout, CancellationToken cancellationToken)
    {
        if (step is not MomoParallelExpectation momoExpectation)
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
}