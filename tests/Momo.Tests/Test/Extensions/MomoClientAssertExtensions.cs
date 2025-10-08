using Momo.Commands;
using Momo.Exceptions;
using Momo.Expectations;
using Momo.Models;
using Momo.Steps;
using NSubstitute;

namespace Momo.Tests.Test.Extensions;

internal static class MomoClientAssertExtensions
{
    internal static async Task ShouldFailMatchingExpectationsAsync(this MomoClient sut, IStepHandler calledStepHandler, IMomoExpectation calledExpectation)
    {
        var exception = Assert.ThrowsAsync<AssertException>(async () => await sut.MatchExpectations(TestContext.CurrentContext.CancellationToken));
        Assert.That(exception.InnerException, Is.Not.TypeOf<TimeoutException>());
        
        await calledStepHandler.Received(1).PrepareAsync(calledExpectation,  Arg.Any<CancellationToken>());
        await calledStepHandler.Received(1).DisposeAsync();
    }
    
    internal static void ShouldBeRetriedMultipleTimes(this MomoClient sut, IStepHandler calledStepHandler)
    {
        //it will throw this exception
        _ = Assert.ThrowsAsync<AssertException>(async () => await sut.MatchExpectations(TestContext.CurrentContext.CancellationToken));
        
        var callCount = calledStepHandler.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IStepHandler.CheckAsync));

        Assert.That(callCount, Is.GreaterThan(1), "Should have been called more than once");
    }
    
    internal static async Task ShouldFailInTimeoutAsync(
        this MomoClient sut, IStepHandler calledStepHandler, IMomoExpectation calledExpectation, CancellationToken cancellationToken)
    {
        var exception = Assert.ThrowsAsync<AssertException>(() => sut.MatchExpectations(cancellationToken));
        
        Assert.Multiple(() =>
        {
            Assert.That(exception.Breakout, Is.EqualTo(true));
            Assert.That(exception.InnerException, Is.TypeOf<TimeoutException>());
        });

        await calledStepHandler.Received(1).PrepareAsync(calledExpectation,  Arg.Any<CancellationToken>());
        await calledStepHandler.Received(1).DisposeAsync();
    }
    
    internal static async Task ShouldMatchExpectationAsync(this MomoClient sut, IStepHandler calledStepHandler, IMomoExpectation calledExpectation, int calledTimes = 1)
    {
        await calledStepHandler.Received(calledTimes).PrepareAsync(calledExpectation,  Arg.Any<CancellationToken>());
        await calledStepHandler.Received(calledTimes).DisposeAsync();
    }

    internal static async Task ShouldHaveRunCommands(this MomoClient sut, IMomoCommand command, IMomoCommand? undoCommand, CancellationToken usedCancellationToken)
    {
        await command.Received(1).ExecuteAsync(usedCancellationToken);
        
        if (undoCommand is not null)
        {
            await undoCommand.Received(1).ExecuteAsync(CancellationToken.None);
        }
        
    }
}