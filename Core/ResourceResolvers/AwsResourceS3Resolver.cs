using System.Text.RegularExpressions;
using Core.Cache;
using Core.Models;

namespace Core.ResourceResolvers
{
    internal class AwsResourceS3Resolver(string arn, AwsResourceSingleFlightCache cache) : IAwsResourceResolver
    {
        private readonly string _bucketName = Arn.ParseArn(arn).ResourceName;

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var source = new HashSet<string>();

            var notifications = await cache.GetBucketNotificationAsync(_bucketName);

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
