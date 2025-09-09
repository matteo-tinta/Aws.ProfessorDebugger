using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.IdentityManagement;
using Amazon.Lambda;
using Amazon.S3;
using Core.Cache;

namespace Core.ResourceResolvers
{
    internal class AwsResourceSNSResolver: IAwsResourceResolver
    {
        private readonly string arn;
        private readonly AmazonS3Client _s3Client;
        private readonly AmazonLambdaClient _lambdaClient;
        private readonly IAmazonIdentityManagementService _iamClient;

        public AwsResourceSNSResolver(string arn, 
            AmazonS3Client s3Client,
            AmazonLambdaClient lambdaClient,
            IAmazonIdentityManagementService iamClient)
        {
            this.arn = arn;
            this._s3Client = s3Client;
            this._lambdaClient = lambdaClient;
            this._iamClient = iamClient;
        }

        public Task<List<string>> GetDownstreamResourcesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetUpstreamResourcesAsync()
        {
            Console.WriteLine($"PROCESSING SNS [{arn}]...");
            var sources = new List<string>();
            var snsName = Regex.Match(arn, @":([^:]+)$").Groups[1].Value;

            // 1. Check S3 Buckets → SNS
            var buckets = await AwsResourceCache.GetBuckets(_s3Client);
            foreach (var bucket in buckets)
            {
                try
                {
                    var notificationConfig = await AwsResourceCache.GetBucketNotificationAsync(_s3Client, bucket.BucketName);

                    if (notificationConfig.TopicConfigurations != null)
                    {
                        foreach (var topicConfig in notificationConfig.TopicConfigurations)
                        {
                            if (!string.IsNullOrEmpty(topicConfig.Topic) && topicConfig.Topic == arn)
                            {
                                sources.Add($"arn:aws:s3:::{bucket.BucketName}");
                            }
                        }
                    }
                }
                catch
                {
                    continue; // Skip buckets with access issues
                }
            }

            // 2. Check Lambda → SNS (via IAM role policies) AND environment variables
            var functions = await AwsResourceCache.GetLambdaFunctions(_lambdaClient);

            foreach (var function in functions)
            {
                var config = await AwsResourceCache.GetLambdaConfigAsync(_lambdaClient, function.FunctionName);

                var roleArn = config.Role;
                if (string.IsNullOrEmpty(roleArn)) continue;

                var roleName = roleArn.Split('/').Last();

                // --- INLINE POLICIES ---
                var inlinePolicyList = await AwsResourceCache.GetInlinePolicyListAsync(_iamClient, roleName);

                foreach (var policyName in inlinePolicyList.PolicyNames ?? Enumerable.Empty<string>())
                {
                    var policy = await AwsResourceCache.GetInlinePolicyAsync(_iamClient, roleName, policyName);

                    if (PolicyGrantsSnsPublish(policy.PolicyDocument, arn))
                    {
                        sources.Add(function.FunctionArn);
                        break;
                    }
                }

                // --- MANAGED POLICIES ---
                var attachedPolicies = await AwsResourceCache.GetAttachedPoliciesAsync(_iamClient, roleName);

                foreach (var attached in attachedPolicies.AttachedPolicies ?? [])
                {
                    var policyMetadata = await AwsResourceCache.GetPolicyMetadataAsync(_iamClient, attached.PolicyArn);

                    var versionId = policyMetadata.Policy.DefaultVersionId;

                    var policyVersion = await AwsResourceCache.GetPolicyVersionAsync(_iamClient, attached.PolicyArn, versionId);

                    if (PolicyGrantsSnsPublish(policyVersion.PolicyVersion.Document, arn))
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
                        if (kvp.Value != null && kvp.Value.Contains(snsName, StringComparison.InvariantCultureIgnoreCase))
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
