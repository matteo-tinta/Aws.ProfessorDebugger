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
    internal class AwsResourceS3Resolver: IAwsResourceResolver
    {
        private readonly string arn;

        public AwsResourceS3Resolver(string arn)
        {
            this.arn = arn;
        }

        public Task<List<string>> GetDownstreamResourcesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetUpstreamResourcesAsync()
        {
            Console.WriteLine($"PROCESSING S3 [{arn}]...");
            //Usually S3 is the root node so we can stop here... (for now :)
            return Task.FromResult(new List<string>());
        }
    }
}
