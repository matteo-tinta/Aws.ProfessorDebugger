using System.Text.Json.Serialization;
using Momo.Commands;
using Momo.Exceptions;
using Momo.Expectations;
using Newtonsoft.Json;

namespace Momo.Models;

public class MomoClientCommand
{
    public required IMomoCommand Command { get; set; }
    public IMomoCommand? Undo { get; set; }
}


public record MomoExpectationFile
{
    public string TraceId { get; set; }

    [JsonPropertyName("do")]
    public IReadOnlyCollection<MomoClientCommand> Commands { get; init; } = [];
    
    public required IReadOnlyCollection<IMomoExpectation> Expectations { get; set; } = [];
    
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