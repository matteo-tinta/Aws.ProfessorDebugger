using Momo.Exceptions;

namespace Momo.Helpers;

internal static class TimeoutHelpers
{
    internal static async Task<TOut> TimeoutAsync<TOut>(TimeSpan timeout, Func<CancellationToken, Task<TOut>> action, CancellationToken cancellationToken)
    {
        var cancellationSource = new CancellationTokenSource(timeout);
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationSource.Token, cancellationToken);
        
        //checks if user has forgotten to check the cancellation token
        //will ignore the current task after the specified time (fail-safe)
        //Task will be still up and running, there is no way to stop it except making the whole window process fail
        var userForgotToCheckCancellationTokenSource = new CancellationTokenSource();
        userForgotToCheckCancellationTokenSource.CancelAfter(timeout.Add(TimeSpan.FromSeconds(5)));
        
        var task = action(cancellationTokenSource.Token);
        var delayTask = Task.Delay(Timeout.InfiniteTimeSpan, userForgotToCheckCancellationTokenSource.Token);

        var completed = await Task.WhenAny(task, delayTask);

        if (completed == delayTask)
        {
            throw new AssertException("Process is timed out", 
                    new TimeoutException("The operation has been ignored. Please remember to check the cancellation token expiration in your code."))
                .BreakWhenRaised();
        }

        return await task;
    }
    
    internal static async Task TimeoutAsync(TimeSpan timeout, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        //reusing logic, but value ignored
        _ = await TimeoutAsync(timeout, async (c) =>
            {
                c.ThrowIfCancellationRequested();
                await action(c);
                return true;
            }, 
            cancellationToken);
    }
}