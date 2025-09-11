using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Core.Cache;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSQSResolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly string queueName;
        private readonly AmazonSQSClient _sqsClient;
        private readonly IAmazonLambda _lambdaClient;
        private readonly IAmazonSimpleSystemsManagement _ssmClient;
        private readonly IAmazonIdentityManagementService _iamClient;

        public AwsResourceSQSResolver(string arn, 
            AmazonSQSClient sqsClient,
            IAmazonLambda lambdaClient,
            IAmazonSimpleSystemsManagement ssmClient,
            IAmazonIdentityManagementService iamClient)
        {
            this.arn = arn;
            this.queueName = Regex.Match(arn, @":([^:]+)$").Groups[1].Value;
            this._sqsClient = sqsClient;
            this._lambdaClient = lambdaClient;
            this._ssmClient = ssmClient;
            this._iamClient = iamClient;
        }

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var sources = new HashSet<string>();
            var mappingResponse = await AwsResourceCache.GetSqsLambdaTriggersAsync(_lambdaClient, arn);

            foreach (var mapping in mappingResponse.EventSourceMappings)
            {
                sources.Add(mapping.FunctionArn);
            }

            return sources.ToList();
        }

        public async Task<List<string>> GetUpstreamResourcesAsync()
        {
            var sources = new HashSet<string>();
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

                    //Reading the variables
                    if (config.Environment?.Variables != null)
                    {
                        foreach (var kvp in config.Environment.Variables)
                        {
                            if (kvp.Value != null
                                && kvp.Key.ToLower() != "queueurl" //ignores pcim queue url in env variables
                                && kvp.Key.ToLower() != "sqs__ingestionqueueurl" //ignores pcim queue url in env variables
                                && kvp.Value.Contains(queueName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                sources.Add(function.FunctionArn);
                                break; // Found match, no need to continue scanning vars
                            }
                        }
                    }

                    //Reading the policies and SSM
                    var roleArn = config.Role;
                    if (string.IsNullOrEmpty(roleArn)) continue;

                    var roleName = roleArn.Split('/').Last();

                    // --- INLINE/ATTACHED POLICIES FOR FINDING SQS IN SSM ---
                    foreach (var item in await GetSqsPoliciesForSsmAsync(function.FunctionArn, roleName))
                    {
                        sources.Add(item);
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

        private List<string> ExtractSsmParameterPathsFromPolicyDocument(string policyDocumentJson)
        {
            var paramPaths = new List<string>();

            using var jsonDoc = JsonDocument.Parse(policyDocumentJson);
            if (!jsonDoc.RootElement.TryGetProperty("Statement", out var statements))
                return paramPaths;

            foreach (var statement in statements.EnumerateArray())
            {
                if (!statement.TryGetProperty("Action", out var actions))
                    continue;

                // Normalize to list of strings
                var actionsList = new List<string>();
                if (actions.ValueKind == JsonValueKind.String)
                    actionsList.Add(actions.GetString());
                else if (actions.ValueKind == JsonValueKind.Array)
                    foreach (var act in actions.EnumerateArray())
                        actionsList.Add(act.GetString());

                // Check if policy allows or denies ssm:GetParameter or ssm:*
                if (!actionsList.Any(a => a.StartsWith("ssm:GetParameter") || a.StartsWith("ssm:*")))
                    continue;

                // Extract resource(s)
                if (!statement.TryGetProperty("Resource", out var resources))
                    continue;

                if (resources.ValueKind == JsonValueKind.String)
                {
                    var resource = resources.GetString();
                    if (resource.Contains("parameter"))
                        paramPaths.Add(resource);
                }
                else if (resources.ValueKind == JsonValueKind.Array)
                {
                    foreach (var resource in resources.EnumerateArray())
                    {
                        var resourceStr = resource.GetString();
                        if (resourceStr.Contains("parameter"))
                            paramPaths.Add(resourceStr);
                    }
                }
            }

            return paramPaths;
        }

        private async Task<HashSet<string>> GetSqsPoliciesForSsmAsync(string functionName, string roleName)
        {
            List<string> policies = new List<string>();

            var inlinePolicyList = await AwsResourceCache.GetInlinePolicyListAsync(_iamClient, roleName);
            foreach (var policyName in inlinePolicyList.PolicyNames ?? Enumerable.Empty<string>())
            {
                var policy = await AwsResourceCache.GetInlinePolicyAsync(_iamClient, roleName, policyName);

                var document = Uri.UnescapeDataString(policy.PolicyDocument);
                var paramPaths = ExtractSsmParameterPathsFromPolicyDocument(document);
                policies.AddRange(paramPaths);
            }

            var attachedPolicies = await AwsResourceCache.GetAttachedPoliciesAsync(_iamClient, roleName);
            foreach (var attached in attachedPolicies.AttachedPolicies ?? [])
            {
                var policyMetadata = await AwsResourceCache.GetPolicyMetadataAsync(_iamClient, attached.PolicyArn);
                var versionId = policyMetadata.Policy.DefaultVersionId;

                var policyVersion = await AwsResourceCache.GetPolicyVersionAsync(_iamClient, attached.PolicyArn, versionId);
                var document = Uri.UnescapeDataString(policyVersion.PolicyVersion.Document);
                var paramPaths = ExtractSsmParameterPathsFromPolicyDocument(document);
                policies.AddRange(paramPaths);
            }

            return await GetSqsFromPoliciesInParameterStore(functionName, policies.Distinct().ToList());
        }

        private async Task<HashSet<string>> GetSqsFromPoliciesInParameterStore(
            string functionArn,
            List<string> parameters)
        {
            HashSet<string> sources = new HashSet<string>();
            foreach (var parameterArn in parameters.Distinct())
            {
                var match = Regex.Match(parameterArn, @"^arn:aws:ssm:[^:]*:[^:]*:parameter(/.+)$");
                if(!match.Success)
                {
                    continue;
                }

                var parameterPath = match.Groups[1].Value;
                try
                {
                    var parameterResponse = await AwsResourceCache.GetSsmParameter(_ssmClient, parameterPath);
                    var jsonString = parameterResponse.Value;

                    using var docJson = JsonDocument.Parse(jsonString);

                    if (jsonString.Contains(queueName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        sources.Add(functionArn);
                        break;
                    }
                }
                catch (ParameterNotFoundException)
                {
                    Console.WriteLine($" == SSM {parameterArn} NOT FOUND == ");
                }
            }

            return sources;
        }

    }
}
