using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Cache;
using Core.Models;
using Models;

namespace Core.ResourceResolvers
{
    internal class AwsResourceLambdaResolver: IAwsResourceResolver
    {
        private readonly Arn _arn;
        private readonly string _functionName;
        private readonly AwsResourceSingleFlightCache _cache;

        public AwsResourceLambdaResolver(string arn,
            AwsResourceSingleFlightCache cache)
        {
            _arn = Arn.ParseArn(arn);
            _functionName = _arn.ResourceName;
            _cache = cache;
        }

        public async Task<List<string>> GetUpstreamResourcesAsync() {
            Console.WriteLine($"PROCESSING LAMBDA [{_arn.ResourceArn}]...");
            var sources = new List<string>();
            var functionName = Regex.Match(_arn.ResourceArn, @"function:(.+)").Groups[1].Value;

            // 1. Find sources configured via Event Source Mappings (the "pull" model)
            // This is for services like SQS, Kinesis, DynamoDB where Lambda polls for messages.
            try
            {
                var mappingResponse = await _cache.GetLambdaEventSourceMappingAsync(functionName);

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
                var policyResponse = await _cache.GetLambdaPolicyAsync(functionName);

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
        public async Task<List<string>> GetDownstreamResourcesAsync() 
        {
            var sources = new HashSet<string>();

            var config = await _cache.GetLambdaConfigAsync(_functionName);

            var roleArn = config.Role;
            if (!string.IsNullOrEmpty(roleArn))
            {
                var roleName = roleArn.Split('/').Last();

                // --- INLINE POLICIES ---
                var inlinePolicyList = await _cache.GetInlinePolicyListAsync(roleName);

                foreach (var policyName in inlinePolicyList.PolicyNames ?? Enumerable.Empty<string>())
                {
                    var policy = await _cache.GetInlinePolicyAsync(roleName, policyName);

                    //HERE!
                    var decoded = System.Net.WebUtility.UrlDecode(policy.PolicyDocument);
                    var results = GetSnsAndSqsPublishInPolicies(decoded);
                    foreach (var arn in results)
                    {
                        sources.Add(arn);
                    }
                }

                // --- MANAGED POLICIES ---
                var attachedPolicies = await _cache.GetAttachedPoliciesAsync(roleName);

                foreach (var attached in attachedPolicies.AttachedPolicies ?? [])
                {
                    var policyMetadata = await _cache.GetPolicyMetadataAsync(attached.PolicyArn);
                    var versionId = policyMetadata.Policy.DefaultVersionId;

                    var policyVersion = await _cache.GetPolicyVersionAsync(attached.PolicyArn, versionId);

                    var decoded = System.Net.WebUtility.UrlDecode(policyVersion.PolicyVersion.Document);
                    var results = GetSnsAndSqsPublishInPolicies(decoded);
                    foreach (var arn in results)
                    {
                        sources.Add(arn);
                    }
                }
            }

            return sources.ToList();
        }

        private HashSet<string> GetSnsAndSqsPublishInPolicies(string policyDocument)
        {
            var sources = new HashSet<string>();

            using var document = JsonDocument.Parse(policyDocument);
            var root = document.RootElement;

            if (!root.TryGetProperty("Statement", out var statements)
                || statements.ValueKind != JsonValueKind.Array)
            {
                return sources;
            }

            foreach (var statement in statements.EnumerateArray())
            {
                if (!statement.TryGetProperty("Effect", out var effect) || effect.GetString() != "Allow")
                    continue;

                List<string> actions = new();
                if (statement.TryGetProperty("Action", out var actionElement))
                {
                    if (actionElement.ValueKind == JsonValueKind.String)
                    {
                        actions.Add(actionElement.GetString());
                    }
                    else if (actionElement.ValueKind == JsonValueKind.Array)
                    {
                        actions.AddRange(actionElement.EnumerateArray().Select(a => a.GetString()));
                    }
                }

                // Check for sns:Publish or sqs:SendMessage
                var matchedActions = actions
                    .Where(a => string.Equals(a, "sns:Publish", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(a, "sqs:SendMessage", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedActions.Any())
                {
                    // Handle 'Resource' as string or array
                    string resourcesOutput = "";
                    if (statement.TryGetProperty("Resource", out var resourceElement))
                    {
                        if (resourceElement.ValueKind == JsonValueKind.String)
                        {
                            resourcesOutput = resourceElement.GetString();
                        }
                        else if (resourceElement.ValueKind == JsonValueKind.Array)
                        {
                            resourcesOutput = string.Join(", ", resourceElement.EnumerateArray().Select(r => r.GetString()));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(resourcesOutput))
                    {
                        var parsedArn = Arn.ParseArn(resourcesOutput);
                        if(!parsedArn.ResourceName.Contains("*"))
                        {
                            sources.Add(resourcesOutput);
                        }
                    }
                }
            }

            return sources;
        }
    }
}
