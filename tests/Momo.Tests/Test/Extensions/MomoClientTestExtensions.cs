namespace Momo.Tests.Test.Extensions;

internal static class MomoClientTestExtensions
{
    internal static MomoClient WithOnExpectationPreparedExecuteCommands(this MomoClient sut)
    {
        var alreadyRanCommands = false;
        
        sut.OnExpectationPrepared = async (expectation, handler) =>
        {
            if (!alreadyRanCommands)
            {
                alreadyRanCommands = true;
                await sut.ExecuteCommands(TestContext.CurrentContext.CancellationToken);
            }
        };

        return sut;
    }
}