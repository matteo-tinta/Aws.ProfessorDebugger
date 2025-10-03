namespace Momo.Helpers;

internal static class TimeoutHelpers
{
    internal static Task<TOut> TimeoutAsync<TOut>(TimeSpan timeout, Func<CancellationToken, Task<TOut>> action, CancellationToken cancellationToken)
    {
        var cancellationSource = new CancellationTokenSource(timeout);
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationSource.Token, cancellationToken);
        
        return action(cancellationTokenSource.Token);
    }
    
    internal static Task TimeoutAsync(TimeSpan timeout, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        var cancellationSource = new CancellationTokenSource(timeout);
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationSource.Token, cancellationToken);
        
        return action(cancellationTokenSource.Token);
    }
}