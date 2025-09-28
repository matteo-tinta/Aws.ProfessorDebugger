using Momo.Models;

namespace Momo;

public class MomoClientFactoryOptions
{
    public bool AutoMode { get; set; } = false;
    public required MomoExpectationFile ExpectationFile { get; set; }
}

public static class MomoClientFactory
{
    /// <summary>
    /// Entry point for feeding Momo with a new file, allowing it to analyze and match all relevant requests.
    /// </summary>
    public static IMomoClient FeedMomo(MomoClientFactoryOptions options)
    {
        var client = MomoClient.ValidateAndCreate(options);

        if (options.AutoMode)
        {
            return new AutoMomoClient(options);
        }
        
        return client;
    }
}