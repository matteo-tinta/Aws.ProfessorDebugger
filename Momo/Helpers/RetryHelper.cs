using Momo.Exceptions;

namespace Momo.Helpers;

internal static class RetryHelper
{
    internal static async Task<T> RetryAsync<T>(
        Func<Task<T>> operationFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operationFactory);

        var retryInterval = TimeSpan.FromSeconds(5);
        Exception? lastException = null;
        
        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operationFactory();
            }
            catch (OperationCanceledException ex)
            {
                //in case of operation cancelled throw last recorded exception
                //otherwise throw ex;
                throw new AssertException("Step failed internally", lastException ?? ex);
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                CheckForInternalsBreakdownThrows(ex);
                await Task.Delay(retryInterval, CancellationToken.None); //checked above
            }
        }
    }

    private static void CheckForInternalsBreakdownThrows(Exception e)
    {
        switch (e)
        {
            case AssertException { Breakout: true }: throw e;
            case { InnerException: not null }:
                CheckForInternalsBreakdownThrows(e.InnerException);
                break;
        }
    }
}