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

    public void Validate()
    {
        try
        {
            if (ParallelExpectations.Count == 0)
            {
                throw new MomoFileValidationException(nameof(ParallelExpectations), "ParallelExpectations cannot be empty. Remove the block");
            }

            ParallelExpectations.ForEach(v => v.Validate());
        }
        catch (MomoFileValidationException e)
        {
            throw new MomoFileValidationException(nameof(ParallelExpectations), "Some of contained expectations was invalid", e);
        }
    }

    public override string ToString() => "PARALLEL";
}