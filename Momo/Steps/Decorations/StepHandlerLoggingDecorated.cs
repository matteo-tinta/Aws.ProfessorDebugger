using Momo.Models;

namespace Momo.Steps.Decorations;

internal class StepHandlerLoggingDecorated(IStepHandler stepHandler): IStepHandler
{
    public async Task<bool> WaitForMatchAsync(MomoExpectation step, int timeout, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{step.Arn}]: Matching {step.Match.Count} expectations...");
        
        var matches = await stepHandler.WaitForMatchAsync(step, timeout, cancellationToken);

        if (matches)
        {
            Console.WriteLine($"[{step.Arn}]: Matched all expectations");
        }

        return matches;
    }
}