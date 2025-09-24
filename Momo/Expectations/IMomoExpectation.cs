using Momo.Steps;

namespace Momo.Expectations;

public interface IMomoExpectation
{
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options);
}