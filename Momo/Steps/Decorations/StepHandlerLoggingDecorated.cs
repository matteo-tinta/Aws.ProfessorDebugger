using Momo.Models;

namespace Momo.Steps.Decorations;

internal class StepHandlerLoggingDecorated(IStepHandler stepHandler) : IStepHandler
{
    public void Dispose() => stepHandler.Dispose();

    public async Task<bool> WaitForMatchAsync(MomoExpectation step, int timeout, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{step.Arn}]: Matching {step.Match.Count} expectations...");
        
        var result = await stepHandler.WaitForMatchAsync(step, timeout, cancellationToken);
        
        Console.WriteLine($"[{step.Arn}]: Matched all {step.Match.Count} expectations");

        return result;
    }
}