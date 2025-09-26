using Momo.Exceptions;

namespace Momo.Helpers;

internal static class RetryHelper
{
    internal static async Task<T> RetryAsync<T>(
        Func<Task<T>> operationFactory,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (operationFactory == null) 
            throw new ArgumentNullException(nameof(operationFactory));

        var startTime = DateTime.UtcNow;
        var retryInterval = TimeSpan.FromSeconds(5);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operationFactory();
            }
            catch (Exception ex) when (ex is AssertException or MessageAssertException)
            {
                CheckForInternalsBreakdownThrows(ex);
                
                if (DateTime.UtcNow - startTime >= timeout)
                    throw;

                var remaining = timeout - (DateTime.UtcNow - startTime);
                var delay = remaining < retryInterval ? remaining : retryInterval;

                await Task.Delay(delay, cancellationToken);
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