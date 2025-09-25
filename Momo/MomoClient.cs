using Momo.Exceptions;
using Momo.Helpers;
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
            
            try
            {
                await stepButDecorated.PrepareAsync(expectation, cancellationToken);

                var timeout = TimeSpan.FromSeconds(_options.ExpectationFile.Timeout);
                var result = await RetryHelper.RetryAsync(
                    async () => await stepButDecorated.CheckAsync(expectation, _options.ExpectationFile.Timeout, cancellationToken),
                        timeout, cancellationToken);

                if (!result)
                {
                    throw new AssertException($"Step {JsonConvert.SerializeObject(expectation)} failed");
                }
            }
            finally
            {
                await stepButDecorated.DisposeAsync();
            }
        }
    }

    public static MomoClient ValidateAndCreate(MomoClientFactoryOptions options) => new(options);
}