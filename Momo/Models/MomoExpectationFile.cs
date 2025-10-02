using Momo.Exceptions;
using Momo.Expectations;
using Newtonsoft.Json;

namespace Momo.Models;

public record MomoExpectationFile
{
    public string TraceId { get; set; }
    
    [JsonProperty("expect")]
    public IReadOnlyCollection<IMomoExpectation> Expectations { get; set; }
    
    public int Timeout { get; set; }

    internal void ValidateAllExpectations()
    {
        if (Expectations.Count == 0)
        {
            throw new MomoFileValidationException(nameof(Expectations), "Expectations cannot be empty");
        }

        Expectations.ToList().ForEach(c => c.Validate());
    }
}