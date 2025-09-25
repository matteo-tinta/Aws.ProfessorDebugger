using Momo.Expectations;
using Newtonsoft.Json;

namespace Momo.Steps.Decorations;

internal class StepHandlerLoggingDecorated(IStepHandler stepHandler): IStepHandler
{
    private string? _name;
    private bool _hasAlreadyLogged = false;

    public async ValueTask DisposeAsync()
    {
        await stepHandler.DisposeAsync();
        
        Console.WriteLine($"[{GetName()}]: Disposed");
    }

    public async Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        _name = config.GetType().FullName;
        //Logging? 
        await stepHandler.PrepareAsync(config, cancellationToken);
    }

    public async Task<bool> CheckAsync(IMomoExpectation step, int timeout, CancellationToken cancellationToken)
    {
        LogCheckProcess(() => Console.WriteLine($"[{GetName()}]: Matching expectations...:\n{JsonConvert.SerializeObject(step, Formatting.Indented)}\n"));
        var matches = await stepHandler.CheckAsync(step, timeout, cancellationToken);

        if (matches)
        {
            Console.WriteLine($"[{GetName()}]: Matched all expectations");
        }

        return matches;
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