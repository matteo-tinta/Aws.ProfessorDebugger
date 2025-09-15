namespace Core.Cache.Models;

public class Inflight<T>(Lazy<Task<T>> lazyTask) : IInflight
{
    public Task<T> TypedTask => lazyTask.Value;
    public Task Task => lazyTask.Value;
    public object? ResultUntyped => lazyTask.IsValueCreated && lazyTask.Value.IsCompletedSuccessfully ? lazyTask.Value.Result : null;
}