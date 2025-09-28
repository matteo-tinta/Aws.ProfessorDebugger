using Momo.Expectations;

namespace Momo.Steps;

public interface IStepHandler: IAsyncDisposable
{
    /// <summary>
    /// This method will be called first to instantiate all the stuff once
    /// </summary>
    Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken);
    
    /// <summary>
    /// This method will be called multiple times, please register all your class dependencies in PrepareAsync
    /// </summary>
    Task<bool> CheckAsync(IMomoExpectation config, int timeout, CancellationToken cancellationToken);
    
    /// <summary>
    /// This method will be called to autogenerate an expectation
    /// </summary>
    Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation config, CancellationToken cancellationToken);
}