using Momo.Models;

namespace Momo;

public class MomoClientFactoryOptions
{
    public required MomoExpectationFile ExpectationFile { get; set; }
}

public static class MomoClientFactory
{
    /// <summary>
    /// Entry point for feeding Momo with a new file, allowing it to analyze and match all relevant requests.
    /// </summary>
    public static MomoClient FeedMomo(MomoClientFactoryOptions options)
    {
        return MomoClient.ValidateAndCreate(options);
    }
}