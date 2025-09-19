namespace Momo.Helpers;

public static class RetryHelper
{
    public static async Task<T> RetryAsync<T>(
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
            catch (Exception)
            {
                if (DateTime.UtcNow - startTime >= timeout)
                    throw;

                var remaining = timeout - (DateTime.UtcNow - startTime);
                var delay = remaining < retryInterval ? remaining : retryInterval;

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}