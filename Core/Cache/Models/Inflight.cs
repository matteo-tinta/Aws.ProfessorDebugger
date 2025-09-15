namespace Core.Cache.Models;

public class Inflight<T> : IInflight
{
    private readonly Lazy<Task<T>> _lazyTask;

    public Inflight(Lazy<Task<T>> lazyTask)
    {
        _lazyTask = lazyTask;
    }

    public Task<T> TypedTask => _lazyTask.Value;
    public Task Task => _lazyTask.Value;
    public object? ResultUntyped => _lazyTask.IsValueCreated && _lazyTask.Value.IsCompletedSuccessfully ? _lazyTask.Value.Result : null;
}