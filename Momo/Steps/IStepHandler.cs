using Momo.Expectations;

namespace Momo.Steps;

public interface IStepHandler
{
    Task<bool> WaitForMatchAsync(IMomoExpectation step, int timeout, CancellationToken cancellationToken);
}