using Cli.Printers;

namespace Cli
{
    internal enum GraphPrinterType
    {
        Cli,
        Json,
        Graph,
        Momo
    }

    internal record CreateGraphPrinterOptions
    {
        public GraphPrinterType Type { get; set; } = GraphPrinterType.Cli;
        public string? OutputPath { get; set; }
    }

    internal static class CliClientFactory
    {
        public static IGraphPrinter CreateGraphPrinter(CreateGraphPrinterOptions options) => options.Type switch
        {
            GraphPrinterType.Cli => new CliGraphPrinter(),
            GraphPrinterType.Json => new JsonGraphPrinter(),
            GraphPrinterType.Graph => new GraphGraphPrinter(),
            GraphPrinterType.Momo => new MomoGraphPrinter(options.OutputPath ?? throw new ArgumentNullException(nameof(options.OutputPath))),
            _ => throw new NotImplementedException(),
        };
    }
}
