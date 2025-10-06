using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Momo.Expectations;
using Momo.Models;
using Momo.Steps;
using Momo.Tests.Test.Extensions;
using NSubstitute;
using Tests.Shared.Builders;

namespace Momo.Tests;

[SuppressMessage("Structure", "NUnit1032:An IDisposable field/property should be Disposed in a TearDown method")]
public class MomoClientTests
{
    private ModelBuilder<MomoClientFactoryOptions> _options;
    private ModelBuilder<MomoExpectationFile> _expectationFile;
    private IStepHandler _momoStep;
    private IMomoExpectation _momoExpectation;

    private MomoClientFactoryOptions Options => _options.Build();
    private MomoExpectationFile ExpectationFile => _expectationFile.Build();

    [SetUp]
    public void Setup()
    {
        _momoStep = Substitute.For<IStepHandler>();
        _momoExpectation = Substitute.For<IMomoExpectation>();
        
        _expectationFile = new ModelBuilder<MomoExpectationFile>(() => new MomoExpectationFile()
        {
            Timeout = 3,
            Expectations = [_momoExpectation]
        });
        
        _options = new ModelBuilder<MomoClientFactoryOptions>(() => new MomoClientFactoryOptions
        {
            ExpectationFile = _expectationFile.Build(),
        });
        
        _momoExpectation.GetStepHandler(Options).Returns(_momoStep);
    }
    
    [Test]
    public async Task MatchExpectations_IfStepSucceed_Then_ShouldPass()
    {
        _momoStep
            .CheckAsync(_momoExpectation,Arg.Any<CancellationToken>())
            .Passes();

        _expectationFile.Set(x => x.Expectations, [
            _momoExpectation,
            _momoExpectation,
            _momoExpectation
        ]);
        
        var sut = CreateMomoClientUnderTest(ExpectationFile);
        
        //Act
        await sut.MatchExpectations(TestContext.CurrentContext.CancellationToken);
        
        //Disposing
        await sut.DisposeAsync();
        
        //Assert
        await sut.ShouldMatchExpectationAsync(_momoStep, _momoExpectation, calledTimes: 3); //3 times because we have 3 handlers
    }
    
    [Test]
    [Category("Slow")]
    public async Task MatchExpectations_IfStepFails_Then_ShouldFailAfterRetriedMultipleTimes()
    {
        _momoStep
            .CheckAsync(_momoExpectation, Arg.Any<CancellationToken>())
            .Fails();

        //longer timeouts to check retrying
        _options.Set(x => x.ExpectationFile.Timeout, 5);

        var sut = CreateMomoClientUnderTest(ExpectationFile);
        
        //Act & Assert
        await sut.ShouldFailMatchingExpectationsAsync(_momoStep, _momoExpectation);
        sut.ShouldBeRetriedMultipleTimes(_momoStep);
    }
    
    [Test]
    [Category("Slow")]
    public async Task MatchExpectations_IfStepHangs_Then_ShouldFailInTimeout()
    {
        _momoStep
            .CheckAsync(_momoExpectation,Arg.Any<CancellationToken>())
            .HangsForever();

        var sut = CreateMomoClientUnderTest(ExpectationFile);
        
        //Act & Assert
        await sut.ShouldFailInTimeoutAsync(_momoStep, _momoExpectation, TestContext.CurrentContext.CancellationToken);
    }
    
    [Test]
    [Category("Slow")]
    public async Task MatchExpectations_IfStepAborted_Then_ShouldFailAndDispose()
    {
        var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CurrentContext.CancellationToken);
        
        _momoStep
            .CheckAsync(_momoExpectation,Arg.Any<CancellationToken>())
            .HangsFor(TimeSpan.FromSeconds(3));

        var sut = CreateMomoClientUnderTest(ExpectationFile);
        
        //Act
        _ = sut.MatchExpectations(cancellationSource.Token);
        await cancellationSource.CancelAsync(); //Simulate a canceling
        
        //Assert
        await _momoStep.Received(1).DisposeAsync();
    }

    #region private

    private MomoClient CreateMomoClientUnderTest(MomoExpectationFile expectations)
    {
        _options.Set(x => x.ExpectationFile, expectations);
        return MomoClient.ValidateAndCreate(Options);
    }

    #endregion
}
