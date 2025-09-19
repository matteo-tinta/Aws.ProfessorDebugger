using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Cache;
using Core.Models;
using Models;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSnsResolver: IAwsResourceResolver
    {
        private readonly Arn _arn;
        private readonly AwsResourceSingleFlightCache _cache;
        private readonly string _snsName;

        public AwsResourceSnsResolver(string arn, 
            AwsResourceSingleFlightCache cache)
        {
            _arn = Arn.ParseArn(arn);
            _snsName = _arn.ResourceName;
            _cache = cache;
        }

        public async Task<List<string>> GetDownstreamResourcesAsync()
        {
            var source = new HashSet<string>();

            try
            {
                var response = await _cache.GetSnsSubscriptionsByTopicArnAsync(_arn.ResourceArn);
                foreach (var subscription in response.Subscriptions)
                {
                    switch (subscription.Protocol)
                    {
                        case "sqs":
                            source.Add(subscription.Endpoint);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return source.ToList();
        }

        public async Task<List<string>> GetUpstreamResourcesAsync()
        {
            var sources = new List<string>();

            // 1. Check S3 Buckets → SNS
            var buckets = await _cache.GetBucketsAsync();
            var tasks = buckets.Select(async bucket =>
            {
                try
                {
                    var notificationConfig = await _cache.GetBucketNotificationAsync(bucket.BucketName);

                    if (notificationConfig.TopicConfigurations != null)
                    {
                        foreach (var topicConfig in notificationConfig.TopicConfigurations)
                        {
                            if (!string.IsNullOrEmpty(topicConfig.Topic) && topicConfig.Topic == _arn.ResourceArn)
                            {
                                return $"arn:aws:s3:::{bucket.BucketName}";
                            }
                        }
                    }
                }
                catch
                {
                    // Skip buckets with access issues
                }

                return null; // No matching topic
            });

            var results = await Task.WhenAll(tasks);
            sources.AddRange(results.Where(r => r != null).ToList());

            // 2. Check Lambda → SNS (via IAM role policies) AND environment variables
            var functions = await _cache.GetLambdaFunctionsAsync();

            foreach (var function in functions)
            {
                var config = await _cache.GetLambdaConfigAsync(function.FunctionName);

                var roleArn = config.Role;
                if (string.IsNullOrEmpty(roleArn)) continue;

                var roleName = roleArn.Split('/').Last();

                // --- INLINE POLICIES ---
                var inlinePolicyList = await _cache.GetInlinePolicyListAsync(roleName);

                foreach (var policyName in inlinePolicyList.PolicyNames ?? Enumerable.Empty<string>())
                {
                    var policy = await _cache.GetInlinePolicyAsync(roleName, policyName);

                    if (PolicyGrantsSnsPublish(policy.PolicyDocument, _arn.ResourceArn))
                    {
                        sources.Add(function.FunctionArn);
                        break;
                    }
                }

                // --- MANAGED POLICIES ---
                var attachedPolicies = await _cache.GetAttachedPoliciesAsync(roleName);

                foreach (var attached in attachedPolicies.AttachedPolicies ?? [])
                {
                    var policyMetadata = await _cache.GetPolicyMetadataAsync(attached.PolicyArn);

                    var versionId = policyMetadata.Policy.DefaultVersionId;

                    var policyVersion = await _cache.GetPolicyVersionAsync(attached.PolicyArn, versionId);

                    if (PolicyGrantsSnsPublish(policyVersion.PolicyVersion.Document, _arn.ResourceArn))
                    {
                        sources.Add(function.FunctionArn);
                        break;
                    }
                }

                // --- ENVIRONMENT VARIABLES ---
                if (config.Environment?.Variables != null)
                {
                    foreach (var kvp in config.Environment.Variables)
                    {
                        if (kvp.Value != null && kvp.Value.Contains(_snsName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            sources.Add(function.FunctionArn);
                            break; // Found match, no need to continue scanning vars
                        }
                    }
                }
            }

            return sources.Distinct().ToList();
        }

        private bool PolicyGrantsSnsPublish(string encodedPolicyDocument, string targetSnsArn)
        {
            var decoded = System.Net.WebUtility.UrlDecode(encodedPolicyDocument);
            var json = JsonDocument.Parse(decoded);

            var statements = json.RootElement.TryGetProperty("Statement", out var stmts)
                ? stmts.ValueKind == JsonValueKind.Array
                    ? stmts.EnumerateArray()
                    : new[] { stmts }.AsEnumerable()
                : Enumerable.Empty<JsonElement>();

            foreach (var statement in statements)
            {
                if (statement.TryGetProperty("Effect", out var effect) && effect.GetString() != "Allow")
                    continue;

                if (!statement.TryGetProperty("Action", out var actionElement))
                    continue;

                var actions = actionElement.ValueKind switch
                {
                    JsonValueKind.String => new[] { actionElement.GetString() },
                    JsonValueKind.Array => actionElement.EnumerateArray().Select(x => x.GetString()),
                    _ => Enumerable.Empty<string>()
                };

                if (!actions.Any(a => a != null && (a == "sns:Publish" || a == "sns:*" || a == "*")))
                    continue;

                if (!statement.TryGetProperty("Resource", out var resourceElement))
                    continue;

                var resources = resourceElement.ValueKind switch
                {
                    JsonValueKind.String => new[] { resourceElement.GetString() },
                    JsonValueKind.Array => resourceElement.EnumerateArray().Select(x => x.GetString()),
                    _ => Enumerable.Empty<string>()
                };

                 //|| r == "*" || (r != null && r.EndsWith("/*"))
                if (resources.Any(r => r == targetSnsArn))
                    return true;
            }

            return false;
        }
    }
}
