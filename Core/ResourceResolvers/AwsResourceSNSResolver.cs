using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSNSResolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly AmazonS3Client _s3Client;

        public AwsResourceSNSResolver(string arn, AmazonS3Client s3Client)
        {
            this.arn = arn;
            this._s3Client = s3Client;
        }

        public Task<List<string>> GetDownstreamResourcesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetUpstreamResourcesAsync()
        {
            var sources = new List<string>();

            // List all buckets and check their notification configurations
            var bucketsResponse = await _s3Client.ListBucketsAsync();
            foreach (var bucket in bucketsResponse.Buckets)
            {
                try
                {
                    var notificationConfig = await _s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest { BucketName = bucket.BucketName });
                    if (notificationConfig.TopicConfigurations != null)
                    {
                        foreach (var topicConfig in notificationConfig.TopicConfigurations)
                        {
                            if (topicConfig.Topic != null && topicConfig.Topic == arn)
                            {
                                sources.Add($"arn:aws:s3:::{bucket.BucketName}");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore buckets where we don't have permissions
                    continue;
                }
            }

            return sources;
        }
    }
}
