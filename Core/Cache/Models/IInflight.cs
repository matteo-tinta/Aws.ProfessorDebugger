namespace Core.Cache.Models;

public interface IInflight
{
    Task Task { get; }            // non-generic Task for awaiting
    object? ResultUntyped { get; } // optional for inspection
}