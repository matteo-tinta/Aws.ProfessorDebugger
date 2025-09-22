using Momo.Models;
using MongoDB.Driver;

namespace Momo.Steps.Decorations;

internal class StepHandlerLoggingDecorated(IStepHandler stepHandler): IStepHandler
{
    public async Task<bool> WaitForMatchAsync(IMomoExpectation step, int timeout, CancellationToken cancellationToken)
        => step switch
        {
            MomoMongoExpectation momoDatabaseExpectation => await WaitForMatchAsync(momoDatabaseExpectation, timeout, cancellationToken),
            MomoAwsExpectation momoExpectation => await WaitForMatchAsync(momoExpectation, timeout, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(step))
        };

    private async Task<bool> WaitForMatchAsync(MomoMongoExpectation momoExpectation, int timeout,
        CancellationToken cancellationToken)
    {
        var mongoUrl = new MongoUrl(momoExpectation.ConnectionString);
        Console.WriteLine($"[{mongoUrl.DatabaseName ?? mongoUrl.Url}]: Matching {momoExpectation.Match.Count} queries...");
            
        var matches = await stepHandler.WaitForMatchAsync(momoExpectation, timeout, cancellationToken);

        if (matches)
        {
            Console.WriteLine($"[{mongoUrl.DatabaseName ?? mongoUrl.Url}]: Matched all expectations");
        }

        return matches;
    }
    
    private async Task<bool> WaitForMatchAsync(MomoAwsExpectation step, int timeout, CancellationToken cancellationToken)
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