using Momo.Models;

namespace Momo.Steps;

internal interface IStepHandler
{
    Task<bool> WaitForMatchAsync(MomoExpectation step, int timeout, CancellationToken cancellationToken);
}