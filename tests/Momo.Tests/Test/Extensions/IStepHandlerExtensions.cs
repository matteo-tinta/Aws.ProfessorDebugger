using NSubstitute;
using NSubstitute.Core;

namespace Momo.Tests.Test.Extensions;

internal static class IStepHandlerExtensions
{
    internal static ConfiguredCall Passes(this Task<bool> task)
    {
        return task.Returns(Task.FromResult(true));
    }
    
    internal static async Task<ConfiguredCall> PassesAfterAsync(this Task<bool> task, TimeSpan delay, CancellationToken testCancellationToken)
    {
        return task.Returns(async args =>
        {
            await Task.Delay(delay, testCancellationToken);
            _ = Passes(task); //ignored
        });
    }

    internal static ConfiguredCall Fails(this Task<bool> task)
    {
        return task.Returns(Task.FromResult(false));
    }
    
    internal static ConfiguredCall HangsFor(this Task<bool> task, TimeSpan timeSpan)
    {
        return task.Returns(async args =>
        {
            await Task.Delay(timeSpan);
            return true;
        });
    }
    
    internal static ConfiguredCall HangsForever(this Task<bool> task) => HangsFor(task, Timeout.InfiniteTimeSpan);
}