namespace Momo.Models;

public record MomoExpectation
{
    public string Arn { get; set; }
    public Dictionary<string, string> Match { get; set; }
}