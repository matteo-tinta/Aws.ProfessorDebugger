using Core;
using Core.Cache;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var cli = new CliHandler();
            var options = cli.ParseArguments(args);

            await AwsResourceCache.InitializeAsync(new AwsResourceCacheInitOptions()
            {
                IgnoreCacheAndOverride = options.Value.IgnoreCache
            });

            var explorer = AwsClientFactory.CreateResourceResolver();
            var graph = await explorer.TraverseAsync(options.Value.AwsArn);

            await AwsResourceCache.SaveToDiskAsync();

            AwsClientFactory.CreateGraphPrinter(new CreateGraphPrinterOptions() {
                Type = options.Value.OutputAs
            }).Print(graph);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}
