using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Core.Cache;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSQSResolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly AmazonSQSClient _sqsClient;
        private readonly IAmazonLambda _lambdaClient;

        public AwsResourceSQSResolver(string arn, 
            AmazonSQSClient sqsClient,
            IAmazonLambda lambdaClient)
        {
            this.arn = arn;
            this._sqsClient = sqsClient;
            this._lambdaClient = lambdaClient;
        }

        public Task<List<string>> GetDownstreamResourcesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetUpstreamResourcesAsync()
        {
            var sources = new HashSet<string>();
            var queueName = Regex.Match(arn, @":([^:]+)$").Groups[1].Value;
            try
            {
                Console.WriteLine($"PROCESSING SQS [{arn}]...");
                var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName });
                

                // Get the queue policy to find allowed senders (e.g., SNS topics)
                var attributes = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
                {
                    QueueUrl = queueUrlResponse.QueueUrl,
                    AttributeNames = new List<string> { "Policy" }
                });

                if (attributes.Attributes != null && attributes.Attributes.TryGetValue("Policy", out var policyJson))
                {
                    using (var doc = JsonDocument.Parse(policyJson))
                    {
                        if (doc.RootElement.TryGetProperty("Statement", out var statementArray))
                        {
                            foreach (var statement in statementArray.EnumerateArray())
                            {
                                if (statement.TryGetProperty("Condition", out var condition))
                                {
                                    if (condition.TryGetProperty("ArnEquals", out var arnEquals) && arnEquals.TryGetProperty("aws:SourceArn", out var sourceArn))
                                    {
                                        if (sourceArn.ValueKind == JsonValueKind.String)
                                        {
                                            sources.Add(sourceArn.GetString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //Ceck inside lambda vars
                var functions = await AwsResourceCache.GetLambdaFunctions(_lambdaClient);
                foreach (var function in functions)
                {
                    var config = await AwsResourceCache.GetLambdaConfigAsync(_lambdaClient, function.FunctionName);
                    if (config.Environment?.Variables != null)
                    {
                        foreach (var kvp in config.Environment.Variables)
                        {
                            if (kvp.Value != null && kvp.Value.Contains(queueName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                sources.Add(function.FunctionArn);
                                break; // Found match, no need to continue scanning vars
                            }
                        }
                    }
                }


                return sources.ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: {e.Message} - {e.StackTrace}");
                return [];
            }
            
        }
    }
}
