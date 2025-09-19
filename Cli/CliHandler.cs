using System.Text.RegularExpressions;
using CommandLine;
using Core.Cache.Providers;

namespace Cli
{
    [Verb("momo", HelpText = "Feeds Momo with a new file for request matching.")]
    internal class MomoOptions
    {
        [Value(0, Required = true, HelpText = "Path to the input file.")]
        public string InputFile { get; set; }

        // [Option("ignore-cache", Default = false, HelpText = "Ignore stored cache.")]
        // public bool IgnoreCache { get; set; }
    }
    
    [Verb("graph", HelpText = "Analyze AWS ARN and explore graph resources.")]
    internal class GraphOptions
    {
        [Value(0, Required = true, HelpText = "AWS ARN to process.")]
        public string AwsArn { get; set; }

        [Option("ignore-cache", Default = false, HelpText = "Ignore stored cache. Actual present cache file will be overidden.")]
        public bool IgnoreCache { get; set; }

        [Option("output-as", Default = GraphPrinterType.Cli, HelpText = "Cli, Json or Graph (case sensitive)")]
        public GraphPrinterType OutputAs { get; set; }

        ////TODO
        [Option("cache-type", Default = CacheType.JsonFile, HelpText = "Only JsonFile is available for now (case sensitive)")]
        public CacheType CacheType { get; set; }

        [Option("max-level", Required = false, HelpText = "Set the max level to stop")]
        public int? MaxLevel { get; set; }
    }

    internal class CliHandler
    {
        public ParsedCliResult? ParseArguments(string[] args)
        {
            ParsedCliResult? result = null;
            
            Parser.Default.ParseArguments<MomoOptions, GraphOptions>(args)
                .WithParsed<MomoOptions>(options =>
                {
                    result = new ParsedCliResult(CommandType.Momo, options);
                })
                .WithParsed<GraphOptions>(options =>
                {
                    if (!IsValidArn(options.AwsArn))
                    {
                        Console.Error.WriteLine("Arn is not valid");
                        Environment.Exit(1);
                    }
                    
                    result = new ParsedCliResult(CommandType.Graph, options);
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            return result;
        }

        public bool IsValidArn(string arn)
        {
            var pattern = @"^arn:(aws|aws-cn|aws-us-gov):[a-z0-9-]+:[a-z0-9-]*:\d{0,12}:[^:\s]+(:[^:\s]+)*$";
            return Regex.IsMatch(arn, pattern);
        }
    }
    
}

public enum CommandType
{
    Graph,
    Momo
}

internal record ParsedCliResult(
    CommandType Command,
    object Options
);
