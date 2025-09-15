using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.SimpleSystemsManagement.Model;
using Core.Cache;
using Core.Models;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSqsResolver: IAwsResourceResolver
    {
        private readonly Arn _arn;
        private readonly string _queueName;
        private readonly AwsResourceSingleFlightCache _cache;

        public AwsResourceSqsResolver(string arn,
            AwsResourceSingleFlightCache cache)
        {
            _arn = Arn.ParseArn(arn);
            _queueName = _arn.ResourceName;
            _cache = cache;
        }

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var sources = new HashSet<string>();
            var mappingResponse = await _cache.GetSqsLambdaTriggersAsync(_arn.ResourceArn);

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
                var queueUrlResponse = await _cache.GetSqsQueueUrl(_queueName);
                
                // Get the queue policy to find allowed senders (e.g., SNS topics)
                var attributes = await _cache.GetSqsQueueAttributes(queueUrlResponse.QueueUrl);

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
                var functions = await _cache.GetLambdaFunctionsAsync();
                
                var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent operations
                var tasks = functions.Select(async function =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var config = await _cache.GetLambdaConfigAsync(function.FunctionName);

                        // Check env variables
                        bool isMatchInEnv = false;
                        if (config.Environment?.Variables != null)
                        {
                            foreach (var kvp in config.Environment.Variables)
                            {
                                if (kvp.Value != null
                                    && kvp.Key.ToLower() != "queueurl"
                                    && kvp.Key.ToLower() != "sqs__ingestionqueueurl"
                                    && kvp.Value.Contains(_queueName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    isMatchInEnv = true;
                                    break;
                                }
                            }
                        }

                        var matchingSources = new List<string>();

                        if (isMatchInEnv)
                        {
                            matchingSources.Add(function.FunctionArn);
                        }

                        // Check policies and SSM
                        var roleArn = config.Role;
                        if (!string.IsNullOrEmpty(roleArn))
                        {
                            var roleName = roleArn.Split('/').Last();
                            var policyMatches = await GetSqsPoliciesForSsmAsync(function.FunctionArn, roleName);
                            matchingSources.AddRange(policyMatches);
                        }

                        return matchingSources;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                    
                });
                
                var results = await Task.WhenAll(tasks);
                return sources.Concat(results.SelectMany(r => r).ToHashSet()).ToList();
            }
            catch (Exception e)
            {
                //TODO: Add feedback
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

            var inlinePolicyList = await _cache.GetInlinePolicyListAsync(roleName);
            foreach (var policyName in inlinePolicyList.PolicyNames ?? Enumerable.Empty<string>())
            {
                var policy = await _cache.GetInlinePolicyAsync(roleName, policyName);

                var document = Uri.UnescapeDataString(policy.PolicyDocument);
                var paramPaths = ExtractSsmParameterPathsFromPolicyDocument(document);
                policies.AddRange(paramPaths);
            }

            var attachedPolicies = await _cache.GetAttachedPoliciesAsync(roleName);
            foreach (var attached in attachedPolicies.AttachedPolicies ?? [])
            {
                var policyMetadata = await _cache.GetPolicyMetadataAsync(attached.PolicyArn);
                var versionId = policyMetadata.Policy.DefaultVersionId;

                var policyVersion = await _cache.GetPolicyVersionAsync(attached.PolicyArn, versionId);
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
                    var parameterResponse = await _cache.GetSsmParameterAsync(parameterPath);
                    var jsonString = parameterResponse.Value;

                    using var docJson = JsonDocument.Parse(jsonString);

                    if (jsonString.Contains(_queueName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        sources.Add(functionArn);
                        break;
                    }
                }
                catch (ParameterNotFoundException)
                {
                    //TODO: add feedback
                }
            }

            return sources;
        }

    }
}
