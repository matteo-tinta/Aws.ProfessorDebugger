using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Momo.Commands;
using Momo.Commands.Rest;
using Momo.Models;
using Newtonsoft.Json;

namespace Cli.FileLoader.Models.Commands;

public interface IMomoCommandBuildable
{
    public string CommandType { get; set; }
}

public abstract class BaseMomoCommandBuildable : IMomoCommandBuildable
{
    [JsonPropertyName("_type")]
    public string CommandType { get; set; }
} 

[MomoRestCommandType("rest")]
public class MomoRestCommandJsonModel: BaseMomoCommandBuildable
{
    public required string Endpoint { get; set; }
    public string Method { get; set; } = "GET";
    public JsonObject? JsonBody { get; set; }

    public IMomoCommand Build() => new RestMomoCommand(Endpoint, HttpMethod.Parse(Method.ToUpper()), JsonBody?.ToJsonString());
}