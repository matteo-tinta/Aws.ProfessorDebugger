using Momo.Expectations;
using Momo.Models;
using Momo.Steps;

namespace Momo;

public interface IMomoClient: IAsyncDisposable
{
    public Action<IMomoExpectation>? OnExpectationMatch { get; set; }
    
    public Action<MomoExpectationFile>? OnAllExpectationsMatch { get; set; }
    
    public Action<IMomoExpectation, IStepHandler>? OnExpectationPrepared { get; set; }
    
    public Task MatchExpectations(CancellationToken cancellationToken);
    
    public Task ExecuteCommands(CancellationToken cancellationToken);
}