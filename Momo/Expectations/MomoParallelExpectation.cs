using Momo.Steps;

namespace Momo.Expectations;

public class MomoParallelExpectation: IMomoExpectation
{
    public List<IMomoExpectation> ParallelExpectations { get; set; }
    
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        return new MomoParallelStepHandler(options);
    }
}