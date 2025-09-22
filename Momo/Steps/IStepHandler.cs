using Momo.Models;

namespace Momo.Steps;

internal interface IStepHandler
{
    Task<bool> WaitForMatchAsync(IMomoExpectation step, int timeout, CancellationToken cancellationToken);
}