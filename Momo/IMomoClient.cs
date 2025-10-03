using Momo.Expectations;
using Momo.Models;

namespace Momo;

public interface IMomoClient: IAsyncDisposable
{
    public Action<IMomoExpectation>? OnExpectationMatch { get; set; }
    
    public Action<MomoExpectationFile>? OnAllExpectationsMatch { get; set; }
    
    public Task MatchExpectations(CancellationToken cancellationToken);
}