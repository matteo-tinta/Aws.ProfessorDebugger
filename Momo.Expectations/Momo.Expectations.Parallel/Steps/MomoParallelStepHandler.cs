using System.Collections.Concurrent;
using Momo.Expectations.Parallel.Expectations;
using Momo.Steps;
using Momo.Steps.Extensions;

namespace Momo.Expectations.Parallel.Steps;

internal class MomoParallelStepHandler(MomoClientFactoryOptions options) : IStepHandler
{
    private Dictionary<IMomoExpectation, IStepHandler> _stepHandlers = new();
    private List<IStepHandler> _validatedStepHandlers = new();
    private ConcurrentDictionary<IStepHandler, IMomoExpectation> _generatedExpectations = new();

    public async Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        if (config is not MomoParallelExpectation momoExpectation)
        {
            throw new InvalidOperationException("MomoParallelStepHandler must get a MomoParallelExpectation");
        }

        var tasks = new List<Task>();
        foreach (var expectation in momoExpectation.ParallelExpectations)
        {
            var stepHandler = expectation.GetStepHandler(options).DecorateWithLogging(options);
            _stepHandlers.Add(expectation, stepHandler);
            
            tasks.Add(stepHandler.PrepareAsync(expectation, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task<bool> CheckAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        var tasks = _stepHandlers.Select(async c =>
        {
            if (_validatedStepHandlers.Contains(c.Value))
            {
                return true; //already validated
            }
            
            var result = await c.Value.CheckAsync(c.Key, cancellationToken);
            
            _validatedStepHandlers.Add(c.Value);
            
            return result;
        });
        
        await Task.WhenAll(tasks);
        
        return true;
    }

    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        var tasks = _stepHandlers.Select(async c =>
        {
            if (_generatedExpectations.TryGetValue(c.Value, out var expectation))
            {
                return expectation; //already generated
            }
            
            var result = await c.Value.GenerateExpectationAsync(c.Key, cancellationToken);
            
            _generatedExpectations.TryAdd(c.Value, result);
            
            return result;
        });
        
        var result = await Task.WhenAll(tasks);
        
        return new MomoParallelExpectation()
        {
            ParallelExpectations = result.ToList()
        };
    }

    public async ValueTask DisposeAsync()
    {
        var tasks = _stepHandlers.Select(c => c.Value.DisposeAsync().AsTask());
        await Task.WhenAll(tasks);
        
        _stepHandlers.Clear();
    }
}