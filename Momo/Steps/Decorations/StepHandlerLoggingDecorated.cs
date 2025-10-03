using Momo.Expectations;
using Newtonsoft.Json;

namespace Momo.Steps.Decorations;

internal class StepHandlerLoggingDecorated(IStepHandler stepHandler, MomoClientFactoryOptions options): IStepHandler
{
    private readonly MomoClientFactoryOptions _options = options;
    private string? _name;
    private bool _hasAlreadyLogged = false;

    public async ValueTask DisposeAsync()
    {
        await stepHandler.DisposeAsync();
        
        Console.WriteLine($"[{GetName()}]: Disposed");
    }

    public async Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        _name = config.ToString() ?? config.GetType().FullName;
        //Logging? 
        await stepHandler.PrepareAsync(config, cancellationToken);
    }

    public async Task<bool> CheckAsync(IMomoExpectation step, CancellationToken cancellationToken)
    {
        LogCheckProcess(() => Console.WriteLine($"[{GetName()}]: Matching expectations (timeout is {_options.ExpectationFile.Timeout})...:"));
        var matches = await stepHandler.CheckAsync(step, cancellationToken);

        if (matches)
        {
            Console.WriteLine($"[{GetName()}]: Matched all expectations");
        }
        else
        {
            await Console.Error.WriteLineAsync($"[{GetName()}]: Failed to match expectations:\n\n{JsonConvert.SerializeObject(step, Formatting.Indented)}");
        }

        return matches;
    }

    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation step, CancellationToken cancellationToken)
    {
        LogCheckProcess(() => Console.WriteLine($"[{GetName()}]: Listening (timeout is {_options.ExpectationFile.Timeout})..."));
        
        var expectation = await stepHandler.GenerateExpectationAsync(step, cancellationToken);
        
        Console.WriteLine($"[{GetName()}]: expectation generated successfully");

        return expectation;
    }

    private void LogCheckProcess(Action logAction)
    {
        if (!_hasAlreadyLogged)
        {
            logAction();
        }
        _hasAlreadyLogged = true;
    }

    private string GetName() => _name ?? "Unknown";
}