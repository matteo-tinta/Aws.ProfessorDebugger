using System.Text.RegularExpressions;
using CommandLine;

namespace Core
{
    internal class CliOptions
    {
        [Value(0, Required = true, HelpText = "AWS ARN to process.")]
        public string AwsArn { get; set; }

        [Option("ignore-cache", Default = false, HelpText = "Ignore stored cache. Actual present cache file will be overidden.")]
        public bool IgnoreCache { get; set; }

        [Option("output-as", Default = GraphPrinterType.Cli, HelpText = "Cli or Json (case sensitive)")]
        public GraphPrinterType OutputAs { get; set; }

        ////TODO
        [Option("cache-type", Default = CacheType.JsonFile, HelpText = "Only JsonFile is available for now (case sensitive)")]
        public CacheType CacheType { get; set; }
    }

    internal class CliHandler
    {
        public ParserResult<CliOptions> ParseArguments(string[] args)
        {
            return Parser.Default.ParseArguments<CliOptions>(args)
                .WithParsed(options =>
                {
                    if (!IsValidArn(options.AwsArn))
                    {
                        Console.Error.WriteLine("Arn is not valid");
                        Environment.Exit(1);
                    }
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });
        }

        public bool IsValidArn(string arn)
        {
            var pattern = @"^arn:(aws|aws-cn|aws-us-gov):[a-z0-9-]+:[a-z0-9-]*:\d{0,12}:[^:\s]+(:[^:\s]+)*$";
            return Regex.IsMatch(arn, pattern);
        }
    }
}
