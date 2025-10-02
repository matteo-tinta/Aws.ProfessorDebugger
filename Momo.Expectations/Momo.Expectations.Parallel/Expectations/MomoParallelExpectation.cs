using Momo.Exceptions;
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

    public bool Validate()
    {
        try
        {
            if (ParallelExpectations.Count == 0)
            {
                throw new MomoFileValidationException(nameof(ParallelExpectations), "ParallelExpectations cannot be empty. Remove the block");
            }
            
            return ParallelExpectations.All(v => v.Validate());
        }
        catch (MomoFileValidationException e)
        {
            throw new MomoFileValidationException(nameof(ParallelExpectations), "Some of contained expectations was invalid", e);
        }
    }

    public override string ToString() => "PARALLEL";
}