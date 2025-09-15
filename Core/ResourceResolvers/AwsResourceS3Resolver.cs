using System.Text.RegularExpressions;
using Core.Cache;

namespace Core.ResourceResolvers
{
    internal class AwsResourceS3Resolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly string bucketName;
        private readonly AwsResourceSingleFlightCache _cache;

        public AwsResourceS3Resolver(string arn, AwsResourceSingleFlightCache cache)
        {
            this.arn = arn;
            this.bucketName = Regex.Match(arn, @":([^:]+)$").Groups[1].Value;
            _cache = cache;
        }

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var source = new HashSet<string>();

            var notifications = await _cache.GetBucketNotificationAsync(bucketName);

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
            //Usually S3 is the root node so we can stop here... (for now :)
            return Task.FromResult(new List<string>());
        }
    }
}
