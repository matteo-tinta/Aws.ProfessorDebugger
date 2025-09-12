namespace Core.Cache;

public interface IInflight
{
    Task Task { get; }            // non-generic Task for awaiting
    object? ResultUntyped { get; } // optional for inspection
}