using Momo.Models;

namespace Momo.Steps;

internal interface IStepHandler: IDisposable
{
    Task<bool> WaitForMatchAsync(MomoExpectation step, int timeout, CancellationToken cancellationToken);
}