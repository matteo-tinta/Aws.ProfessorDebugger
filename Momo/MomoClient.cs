using Momo.Exceptions;
using Momo.Models;
using Momo.Steps.Decorations;
using Newtonsoft.Json;

namespace Momo;

public class MomoClient
{
    private readonly MomoClientFactoryOptions _options;

    private MomoClient(
        MomoClientFactoryOptions options)
    {
        _options = options;
    }

    public async Task MatchExpectations(CancellationToken cancellationToken)
    {
        foreach (var expectation in _options.ExpectationFile.Expectations)
        {
            var step = expectation.GetStepHandler(_options);
            var stepButDecorated = new StepHandlerLoggingDecorated(step);
            
            var result = await stepButDecorated.WaitForMatchAsync(expectation, _options.ExpectationFile.Timeout, cancellationToken);
            if (!result)
            {
                throw new AssertException($"Step {JsonConvert.SerializeObject(expectation)} failed");
            }
        }
    }

    public static MomoClient ValidateAndCreate(MomoClientFactoryOptions options) => new(options);
}