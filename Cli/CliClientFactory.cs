using Cli.Printers;

namespace Cli
{
    internal enum GraphPrinterType
    {
        Cli,
        Json,
        Graph
    }

    internal record CreateGraphPrinterOptions
    {
        public GraphPrinterType Type { get; set; } = GraphPrinterType.Cli;
    }

    internal static class CliClientFactory
    {
        public static IGraphPrinter CreateGraphPrinter(CreateGraphPrinterOptions options) => options.Type switch
        {
            GraphPrinterType.Cli => new CliGraphPrinter(),
            GraphPrinterType.Json => new JsonGraphPrinter(),
            GraphPrinterType.Graph => new GraphGraphPrinter(),
            _ => throw new NotImplementedException(),
        };
    }
}
