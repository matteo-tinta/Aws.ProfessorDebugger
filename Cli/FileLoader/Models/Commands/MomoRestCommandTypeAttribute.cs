namespace Cli.FileLoader.Models.Commands;

[AttributeUsage(AttributeTargets.Class)]
public class MomoRestCommandTypeAttribute(string type) : Attribute
{
    public string Type { get; } = type;
}