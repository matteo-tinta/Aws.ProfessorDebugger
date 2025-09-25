using Momo.Exceptions;
using Momo.Expectations;
using Momo.Models;
using Momo.Steps;
using NSubstitute;

namespace Momo.Tests;

public class MomoClientTests
{
    private Func<MomoExpectationFile, MomoClient> _createSut;
    private MomoClientFactoryOptions _options;

    [SetUp]
    public void Setup()
    {
        _options = new MomoClientFactoryOptions
        {
            ExpectationFile = null,
        };
        
        _createSut = (expectations) =>
        {
            _options.ExpectationFile = expectations;
            return MomoClient.ValidateAndCreate(_options);
        };
    }
    
    [Test]
    public async Task MatchExpectations_StepThatSucceed_ShouldPass()
    {
        var momoStep = Substitute.For<IStepHandler>();
        var momoExpectation = Substitute.For<IMomoExpectation>();

        momoStep.CheckAsync(momoExpectation!, Arg.Any<int>(),TestContext.CurrentContext.CancellationToken)
            .Returns(true);
        
        momoExpectation.GetStepHandler(_options).Returns(momoStep);
        var expectationFile = new MomoExpectationFile()
        {
            Expectations = [momoExpectation]
        };
        
        var sut = _createSut(expectationFile);
        
        //Act
        await sut.MatchExpectations(TestContext.CurrentContext.CancellationToken);
        
        //Assert
        Assert.Pass("If nothing has been thrown, then expectations passed");
    }
    
    [Test]
    public void MatchExpectations_StepThatFails_ShouldFail()
    {
        var momoStep = Substitute.For<IStepHandler>();
        var momoExpectation = Substitute.For<IMomoExpectation>();

        momoStep.CheckAsync(momoExpectation!, Arg.Any<int>(),TestContext.CurrentContext.CancellationToken)
            .Returns(false);
        
        momoExpectation.GetStepHandler(_options).Returns(momoStep);
        var expectationFile = new MomoExpectationFile()
        {
            Expectations = [momoExpectation]
        };
        
        var sut = _createSut(expectationFile);
        
        //Act & Assert
        Assert.ThrowsAsync<AssertException>(
            () => sut.MatchExpectations(TestContext.CurrentContext.CancellationToken),
            "Step {} failed");
    }
}
