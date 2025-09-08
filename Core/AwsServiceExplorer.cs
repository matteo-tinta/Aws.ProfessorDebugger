using Amazon.SimpleNotificationService;

//TODO: Refactor with cleaner code

namespace Core
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon.Lambda;
    using Amazon.Lambda.Model;
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using Amazon.S3;
    using Amazon.S3.Model;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    namespace AwsEventGraphBuilder
    {
        public class AwsServiceExplorer
        {
            private readonly AmazonLambdaClient _lambdaClient;
            private readonly AmazonSQSClient _sqsClient;
            private readonly AmazonSimpleNotificationServiceClient _snsClient;
            private readonly AmazonS3Client _s3Client;

            public AwsServiceExplorer(AmazonLambdaClient lambdaClient, 
                AmazonSQSClient sqsClient, 
                AmazonSimpleNotificationServiceClient snsClient, 
                AmazonS3Client s3Client)
            {
                _lambdaClient = lambdaClient;
                _sqsClient = sqsClient;
                _snsClient = snsClient;
                _s3Client = s3Client;
            }

            public async Task<Graph> BuildGraphAsync(string startArn)
            {
                var graph = new Graph();
                var queue = new Queue<Node>();
                var visitedArns = new HashSet<string>();

                var startNode = new Node(startArn, "Lambda");
                queue.Enqueue(startNode);
                graph.AddNode(startNode);
                visitedArns.Add(startArn);

                while (queue.Count > 0)
                {
                    var currentNode = queue.Dequeue();

                    try
                    {
                        // Find sources for the current node
                        var sources = await FindSourcesAsync(currentNode);
                        foreach (var sourceArn in sources)
                        {
                            if (!visitedArns.Contains(sourceArn))
                            {
                                var sourceType = GetResourceTypeFromArn(sourceArn);
                                if (sourceType != null)
                                {
                                    var sourceNode = new Node(sourceArn, sourceType);
                                    graph.AddNode(sourceNode);
                                    graph.AddEdge(sourceNode, currentNode);
                                    queue.Enqueue(sourceNode);
                                    visitedArns.Add(sourceArn);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\t[ERROR] Could not process node {currentNode.Arn}: {ex.Message}");
                    }
                }

                return graph;
            }

            private async Task<List<string>> FindSourcesAsync(Node node)
            {
                var sources = new List<string>();

                Console.WriteLine($"\t- Processing {node.Type} {node.Arn} ...");

                switch (node.Type)
                {
                    case "Lambda":
                        sources.AddRange(await FindLambdaSourcesAsync(node.Arn));
                        break;
                    case "SQS":
                        sources.AddRange(await FindSqsSourcesAsync(node.Arn));
                        break;
                    case "SNS":
                        sources.AddRange(await FindSnsSourcesAsync(node.Arn));
                        break;
                    case "S3":
                        // S3 is often the root source, so we stop here.
                        break;
                    default:
                        Console.WriteLine($"\t\t[WARN] Unhandled resource type: {node.Type}");
                        break;
                }

                return sources;
            }

            private async Task<List<string>> FindLambdaSourcesAsync(string lambdaArn)
            {
                var sources = new List<string>();
                var functionName = Regex.Match(lambdaArn, @"function:(.+)").Groups[1].Value;

                // 1. Find sources configured via Event Source Mappings (the "pull" model)
                // This is for services like SQS, Kinesis, DynamoDB where Lambda polls for messages.
                try
                {
                    var mappingResponse = await _lambdaClient.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest { 
                        FunctionName = functionName });
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

            private async Task<List<string>> FindSqsSourcesAsync(string sqsArn)
            {
                var sources = new List<string>();
                var queueName = Regex.Match(sqsArn, @":([^:]+)$").Groups[1].Value;
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
                return sources;
            }

            private async Task<List<string>> FindSnsSourcesAsync(string snsArn)
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
                                if (topicConfig.Topic != null && topicConfig.Topic == snsArn)
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

            private string? GetResourceTypeFromArn(string arn)
            {
                if (arn.Contains(":lambda:")) return "Lambda";
                if (arn.Contains(":sqs:")) return "SQS";
                if (arn.Contains(":sns:")) return "SNS";
                if (arn.Contains(":::")) return "S3";
                return null;
            }
        }
    }

}
