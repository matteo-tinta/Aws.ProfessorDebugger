using System.Text.RegularExpressions;
using Amazon.S3;
using Core.Cache;

namespace Core.ResourceResolvers
{
    internal class AwsResourceS3Resolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly string bucketName;
        private readonly IAmazonS3 _s3Client;

        public AwsResourceS3Resolver(string arn, IAmazonS3 s3Client)
        {
            this.arn = arn;
            this.bucketName = Regex.Match(arn, @":([^:]+)$").Groups[1].Value;
            this._s3Client = s3Client;
        }

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var source = new HashSet<string>();

            var notifications = await AwsResourceCache.GetBucketNotificationAsync(_s3Client, bucketName);

            foreach (var notification in notifications.QueueConfigurations ?? [])
            {
                source.Add(notification.Queue);
            }

            foreach (var notification in notifications.TopicConfigurations ?? [])
            {
                source.Add(notification.Topic);
            }

            foreach (var notification in notifications.LambdaFunctionConfigurations ?? [])
            {
                source.Add(notification.FunctionArn);
            }

            return source.ToList();
        }

        public Task<List<string>> GetUpstreamResourcesAsync()
        {
            Console.WriteLine($"PROCESSING S3 [{arn}]...");
            //Usually S3 is the root node so we can stop here... (for now :)
            return Task.FromResult(new List<string>());
        }
    }
}
