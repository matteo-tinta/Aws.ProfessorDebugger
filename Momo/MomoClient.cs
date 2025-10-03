using Momo.Exceptions;
using Momo.Expectations;
using Momo.Helpers;
using Momo.Models;
using Momo.Steps;
using Momo.Steps.Decorations;
using Newtonsoft.Json;

namespace Momo;

public class MomoClient: IMomoClient
{
    private readonly MomoClientFactoryOptions _options;

    private MomoClient(MomoClientFactoryOptions options)
    {
        _options = options;
    }

    public Action<IMomoExpectation>? OnExpectationMatch { get; set; }
    
    public Action<MomoExpectationFile>? OnAllExpectationsMatch { get; set; }
    
    private readonly List<IStepHandler> _ranExpectations = [];

    public async Task MatchExpectations(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var timeout = TimeSpan.FromSeconds(_options.ExpectationFile.Timeout);
        foreach (var expectation in _options.ExpectationFile.Expectations)
        {
            try
            {
                await TimeoutHelpers.TimeoutAsync(timeout, c => CheckAsync(expectation, c), cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                throw new AssertException(
                        $"Unable to check expectation in given timeout ({_options.ExpectationFile.Timeout} seconds)",
                        new AssertException($"Current Expectation was:\n{JsonConvert.SerializeObject(expectation, Formatting.Indented)}", e))
                    .BreakWhenRaised();
            }
        }
        
        OnAllExpectationsMatch?.Invoke(_options.ExpectationFile);
    }
    
    internal static MomoClient ValidateAndCreate(MomoClientFactoryOptions options)
    {
        try
        {
            options.ExpectationFile.ValidateAllExpectations();
        }
        catch (MomoFileValidationException e)
        {
            throw new MomoClientValidationException("Current Expectation File Is Invalid", e);
        }
        
        return new MomoClient(options);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_ranExpectations.Select(e => e.DisposeAsync().AsTask()));
    }
    
    private async Task DisposeStep(IStepHandler step)
    {
        await step.DisposeAsync();
        _ranExpectations.Remove(step);
    }
    
    private async Task CheckAsync(IMomoExpectation expectation, CancellationToken timedOutCancellationToken)
    {
        var step = expectation.GetStepHandler(_options);
        var stepButDecorated = new StepHandlerLoggingDecorated(step, _options);

        var registration = timedOutCancellationToken.Register(async () => await DisposeStep(stepButDecorated));

        try
        {
            await stepButDecorated.PrepareAsync(expectation, timedOutCancellationToken);

            _ranExpectations.Add(stepButDecorated);

            await RetryHelper.RetryAsync(
                async () =>
                {
                    var result = await stepButDecorated.CheckAsync(expectation, timedOutCancellationToken);
                    return !result ? throw new AssertException($"The following expectation did not match: {JsonConvert.SerializeObject(expectation)}") : true;
                },
                timedOutCancellationToken);

            OnExpectationMatch?.Invoke(expectation);
        }
        finally
        {
            await registration.DisposeAsync();
            await DisposeStep(stepButDecorated);
        }
    }
}