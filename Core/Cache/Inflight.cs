namespace Core.Cache;

public class Inflight<T> : IInflight
{
    private readonly Task<T> _task;

    public Inflight(Task<T> task)
    {
        _task = task;
    }

    public Task<T> TypedTask => _task;
    public Task Task => _task;
    public object? ResultUntyped => _task.IsCompletedSuccessfully ? _task.Result : null;
}