using Newtonsoft.Json;

namespace Momo.Models;




public record MomoExpectationFile
{
    public string TraceId { get; set; }
    
    [JsonProperty("expect")]
    public IReadOnlyCollection<IMomoExpectation> Expectations { get; set; }
    
    public int Timeout { get; set; }
}