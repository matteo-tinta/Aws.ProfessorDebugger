using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace Core.ResourceResolvers
{
    internal class AwsResourceLambdaResolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly AmazonLambdaClient _lambdaClient;

        public AwsResourceLambdaResolver(string arn, AmazonLambdaClient lambdaClient)
        {
            this.arn = arn;
            this._lambdaClient = lambdaClient;
        }

        public async Task<List<string>> GetUpstreamResourcesAsync() {
            var sources = new List<string>();
            var functionName = Regex.Match(arn, @"function:(.+)").Groups[1].Value;

            // 1. Find sources configured via Event Source Mappings (the "pull" model)
            // This is for services like SQS, Kinesis, DynamoDB where Lambda polls for messages.
            try
            {
                var mappingResponse = await _lambdaClient.ListEventSourceMappingsAsync(
                    new ListEventSourceMappingsRequest {
                    FunctionName = functionName
                });

                foreach (var mapping in mappingResponse.EventSourceMappings)
                {
                    sources.Add(mapping.EventSourceArn);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\t\t[WARN] Could not get event source mappings: {ex.Message}");
            }


            // 2. Find sources by parsing the Lambda's own resource-based policy (the "push" model)
            // This is the most reliable way to find services that have permission to invoke the Lambda.
            try
            {
                var policyResponse = await _lambdaClient.GetPolicyAsync(new GetPolicyRequest { FunctionName = functionName });

                using (var doc = JsonDocument.Parse(policyResponse.Policy))
                {
                    if (doc.RootElement.TryGetProperty("Statement", out var statementArray))
                    {
                        foreach (var statement in statementArray.EnumerateArray())
                        {
                            if (statement.TryGetProperty("Condition", out var condition))
                            {
                                if (condition.TryGetProperty("ArnLike", out var arnLike) && arnLike.TryGetProperty("aws:SourceArn", out var sourceArn))
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
            catch (Exception ex)
            {
                // This will throw an exception if the policy doesn't exist, which is a common scenario.
                //Console.WriteLine($"\t\t[INFO] No resource policy found for this Lambda or policy cannot be parsed. {ex.Message}");
            }

            return sources;

        }
        public async Task<List<string>> GetDownstreamResourcesAsync() { return []; }
    }
}
