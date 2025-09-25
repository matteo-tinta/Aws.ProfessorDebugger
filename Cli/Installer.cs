using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;

namespace Cli;

public static class Services
{
    private static ServiceProvider? _provider;

    public static ServiceProvider Provider
    {
        get
        {
            if (_provider != null)
                return _provider;
            
            _provider = GenerateServices();
            return _provider;
        }
    }

    public static void Install()
    {
        _provider = GenerateServices();
    }
    
    private static AmazonS3Client GetAmazonS3Client()
    {
        try
        {
            return new AmazonS3Client(new AmazonS3Config()
            {
                ServiceURL = Environment.GetEnvironmentVariable("AWS_S3_ENDPOINT"),
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = Environment.GetEnvironmentVariable("AWS_REGION")
            });
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WARNING]: unable to construct s3 client, returning default: {e}");
            return new AmazonS3Client();
        }
    }
    
    private static ServiceProvider GenerateServices()
    {
        // Create service collection
        var serviceCollection = new ServiceCollection();

        // Register services
        serviceCollection.AddSingleton<IAmazonSQS, AmazonSQSClient>();
        serviceCollection.AddSingleton<IAmazonS3, AmazonS3Client>(_ => GetAmazonS3Client());
        serviceCollection.AddSingleton<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>();

        // Build the service provider
        return serviceCollection.BuildServiceProvider();
    }
}