using Momo.Expectations.Parallel.Steps;
using Momo.Steps;

namespace Momo.Expectations.Parallel.Expectations;

public class MomoParallelExpectation: IMomoExpectation
{
    public List<IMomoExpectation> ParallelExpectations { get; set; }
    
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        return new MomoParallelStepHandler(options);
    }

    public override string ToString() => "PARALLEL";
}