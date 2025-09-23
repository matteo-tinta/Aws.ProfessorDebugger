using System.Collections.Concurrent;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Core.Cache.Enumerators;
using Core.Cache.Models;
using Core.Cache.Providers;
using GetPolicyRequest = Amazon.Lambda.Model.GetPolicyRequest;

namespace Core.Cache
{
    internal record AwsResourceCacheInitOptions
    {
        public bool IgnoreCacheAndOverride { get; set; }
    }

    /// <summary>
    /// This class is used for resource caching
    /// </summary>
    internal static class AwsResourceCache
    {
        private static readonly List<FunctionConfiguration> LambdaFunctions = new();
        private static readonly List<S3Bucket> Buckets = new();
        private static readonly ConcurrentDictionary<string, ListEventSourceMappingsResponse> LambdaEventSourceEvents = new();
        private static readonly ConcurrentDictionary<string, ListSubscriptionsByTopicResponse> SnsSubscriptions = new();
        private static readonly ConcurrentDictionary<string, ListEventSourceMappingsResponse> SqsLambdaTriggersEvents = new();
        private static readonly ConcurrentDictionary<string, GetQueueAttributesResponse> SqsQueueAttributes = new();
        private static readonly ConcurrentDictionary<string, GetQueueUrlResponse> SqsQueueUrls = new();
        private static readonly ConcurrentDictionary<string, Parameter> SsmParameters = new();
        private static readonly ConcurrentDictionary<string, GetBucketNotificationResponse> BucketNotifications = new();
        private static readonly ConcurrentDictionary<string, GetFunctionConfigurationResponse> LambdaConfigs = new();
        private static readonly ConcurrentDictionary<string, ListRolePoliciesResponse> InlinePolicyLists = new();
        private static readonly ConcurrentDictionary<(string roleName, string policyName), GetRolePolicyResponse> InlinePolicies = new();
        private static readonly ConcurrentDictionary<string, ListAttachedRolePoliciesResponse> AttachedPolicyLists = new();
        private static readonly ConcurrentDictionary<string, Amazon.IdentityManagement.Model.GetPolicyResponse> PolicyMetadata = new();
        private static readonly ConcurrentDictionary<(string policyArn, string versionId), GetPolicyVersionResponse> PolicyVersions = new();
        private static readonly ConcurrentDictionary<string, Amazon.Lambda.Model.GetPolicyResponse> LambdaPolicies = new();

        private static bool _initialized = false;

        public static bool CacheHasBeenInitialized { get; private set; } = false;

        public static async Task InitializeAsync(ICacheProvider<SerializableAwsCache> cacheProvider, AwsResourceCacheInitOptions options)
        {
            if (_initialized) return;

            if (options?.IgnoreCacheAndOverride == true)
            {
                return; //ignore cache
            }

            await InitializeAsync(cacheProvider);
        }

        public static async Task InitializeAsync(ICacheProvider<SerializableAwsCache> cacheProvider)
        {
            if (_initialized) return;

            try
            {
                var cache = await cacheProvider.GetAsync();

                LambdaFunctions.AddRange(cache.LambdaFunctions ?? []);
                Buckets.AddRange(cache.Buckets ?? []);

                foreach (var kv in cache.SnsSubscriptions ?? []) SnsSubscriptions[kv.Key] = kv.Value;
                foreach (var kv in cache.SqsLambdaTriggerEvents ?? []) SqsLambdaTriggersEvents[kv.Key] = kv.Value;
                foreach (var kv in cache.LambdaEventSourceEvents ?? []) LambdaEventSourceEvents[kv.Key] = kv.Value;
                foreach (var kv in cache.SsmParameters ?? []) SsmParameters[kv.Key] = kv.Value;
                foreach (var kv in cache.BucketNotifications ?? []) BucketNotifications[kv.Key] = kv.Value;
                foreach (var kv in cache.LambdaConfigs ?? []) LambdaConfigs[kv.Key] = kv.Value;
                foreach (var kv in cache.InlinePolicyLists ?? []) InlinePolicyLists[kv.Key] = kv.Value;
                foreach (var kv in cache.InlinePolicies ?? []) InlinePolicies[(kv.Key.Split('|')[0], kv.Key.Split('|')[1])] = kv.Value;
                foreach (var kv in cache.AttachedPolicies ?? []) AttachedPolicyLists[kv.Key] = kv.Value;
                foreach (var kv in cache.PolicyMetadata ?? []) PolicyMetadata[kv.Key] = kv.Value;
                foreach (var kv in cache.PolicyVersions ?? []) PolicyVersions[(kv.Key.Split('|')[0], kv.Key.Split('|')[1])] = kv.Value;
                foreach (var kv in cache.SqsQueueAttributes ?? []) SqsQueueAttributes[kv.Key] = kv.Value;

                CacheHasBeenInitialized = true;
            }
            catch
            {
                //ignored/
            }

            _initialized = true;
        }

        public static async Task SaveToDiskAsync(ICacheProvider<SerializableAwsCache> cacheProvider)
        {
            var cache = new SerializableAwsCache
            {
                LambdaFunctions = LambdaFunctions.ToList(),
                Buckets = Buckets.ToList(),
                BucketNotifications = BucketNotifications.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LambdaConfigs = LambdaConfigs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InlinePolicyLists = InlinePolicyLists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InlinePolicies = InlinePolicies.ToDictionary(kvp => $"{kvp.Key.roleName}|{kvp.Key.policyName}", kvp => kvp.Value),
                AttachedPolicies = AttachedPolicyLists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                PolicyMetadata = PolicyMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                PolicyVersions = PolicyVersions.ToDictionary(kvp => $"{kvp.Key.policyArn}|{kvp.Key.versionId}", kvp => kvp.Value),
                SsmParameters = SsmParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LambdaEventSourceEvents = LambdaEventSourceEvents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SqsLambdaTriggerEvents = SqsLambdaTriggersEvents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SqsQueueAttributes = SqsQueueAttributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SnsSubscriptions = SnsSubscriptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            await cacheProvider.SaveAsync(cache);
        }


        public static async Task<List<FunctionConfiguration>> GetLambdaFunctions(IAmazonLambda lambdaClient)
        {
            if (LambdaFunctions.Count == 0)
            {
                await foreach (var function in LambdaEnumerators.ListAllLambdaFunctions(lambdaClient))
                {
                    LambdaFunctions.Add(function);
                }
            }

            return LambdaFunctions ?? [];
        }

        public static async Task<List<S3Bucket>> GetBuckets(IAmazonS3 s3Client)
        {
            if (Buckets.Count == 0)
            {
                var buckets = await s3Client.ListBucketsAsync();
                Buckets.AddRange(buckets.Buckets);
            }

            return Buckets ?? [];
        }

        public static async Task<ListSubscriptionsByTopicResponse> GetSnsSubscriptionsByTopicArnAsync(IAmazonSimpleNotificationService snsClient, string snsArn)
        {
            if (SnsSubscriptions.TryGetValue(snsArn, out var cached))
            {
                return cached;
            }

            ListSubscriptionsByTopicResponse value = await snsClient.ListSubscriptionsByTopicAsync(snsArn);

            SnsSubscriptions[snsArn] = value;
            return value;
        }
        
        public static async Task<GetQueueUrlResponse> GetSqsQueueUrl(IAmazonSQS sqsClient, string queueName)
        {
            if (SqsQueueUrls.TryGetValue(queueName, out var cached))
            {
                return cached;
            }

            GetQueueUrlResponse? value = await sqsClient.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = queueName });

            SqsQueueUrls[queueName] = value;
            return value;
        }
        
        public static async Task<GetQueueAttributesResponse> GetSqsQueueAttributes(IAmazonSQS sqsClient, string sqsUrl)
        {
            if (SqsQueueAttributes.TryGetValue(sqsUrl, out var cached))
            {
                return cached;
            }

            GetQueueAttributesResponse? value = await sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
            {
                QueueUrl = sqsUrl,
                AttributeNames = new List<string> { "Policy" }
            });

            SqsQueueAttributes[sqsUrl] = value;
            return value;
        }

        public static async Task<ListEventSourceMappingsResponse> GetSqsLambdaTriggersAsync(IAmazonLambda lambdaClient, string sqsArn)
        {
            if (SqsLambdaTriggersEvents.TryGetValue(sqsArn, out var cached))
            {
                return cached;
            }

            var value = await lambdaClient.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest()
            {
                EventSourceArn = sqsArn
            });
            SqsLambdaTriggersEvents[sqsArn] = value;
            return value;
        }

        public static async Task<ListEventSourceMappingsResponse> GetLambdaEventSourceMappingAsync(IAmazonLambda lambdaClient, string functionName)
        {
            if (LambdaEventSourceEvents.TryGetValue(functionName, out var cached))
            {
                return cached;
            }

            var value = await lambdaClient.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest
            {
                FunctionName = functionName
            });
            LambdaEventSourceEvents[functionName] = value;
            return value;
        }

        public static async Task<GetBucketNotificationResponse> GetBucketNotificationAsync(IAmazonS3 s3Client, string bucketName)
        {
            if (BucketNotifications.TryGetValue(bucketName, out var cached))
            {
                return cached;
            }

            var result = await s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest
            {
                BucketName = bucketName
            });

            BucketNotifications[bucketName] = result;
            return result;
        }

        public static async Task<Parameter> GetSsmParameter(IAmazonSimpleSystemsManagement ssmClient, string parameterPath)
        {
            if (SsmParameters.TryGetValue(parameterPath, out var cached))
            {
                return cached;
            }

            var realParameterName = parameterPath.Replace("*", "");

            try
            {
                var config = await ssmClient.GetParametersByPathAsync(new GetParametersByPathRequest
                {
                    Path = realParameterName,
                    Recursive = parameterPath.EndsWith("*"),
                    WithDecryption = true
                });

                SsmParameters[parameterPath] = config.Parameters[0];
                return config.Parameters[0];
            }
            catch (Exception ex)
            {
                //Console.Error.WriteLine($"== PARAMETER PATH {realParameterName} THROW ERROR {ex.Message}");
                return new Parameter()
                {
                    Value = "{}"
                };
            }

        }

        public static async Task<GetFunctionConfigurationResponse> GetLambdaConfigAsync(IAmazonLambda lambdaClient, string functionName)
        {
            if (LambdaConfigs.TryGetValue(functionName, out var cached))
            {
                return cached;
            }

            var config = await lambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest
            {
                FunctionName = functionName
            });

            LambdaConfigs[functionName] = config;
            return config;
        }

        public static async Task<GetRolePolicyResponse> GetInlinePolicyAsync(
    IAmazonIdentityManagementService iamClient, string roleName, string policyName)
        {
            var key = (roleName, policyName);

            if (InlinePolicies.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var policy = await iamClient.GetRolePolicyAsync(new GetRolePolicyRequest
            {
                RoleName = roleName,
                PolicyName = policyName
            });

            InlinePolicies[key] = policy;
            return policy;
        }


        public static async Task<ListRolePoliciesResponse> GetInlinePolicyListAsync(
    IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (InlinePolicyLists.TryGetValue(roleName, out var cached))
            {
                return cached;
            }

            var list = await iamClient.ListRolePoliciesAsync(new ListRolePoliciesRequest
            {
                RoleName = roleName
            });

            InlinePolicyLists[roleName] = list;
            return list;
        }

        public static async Task<ListAttachedRolePoliciesResponse> GetAttachedPoliciesAsync(
    IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (AttachedPolicyLists.TryGetValue(roleName, out var cached))
            {
                return cached;
            }

            var result = await iamClient.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest
            {
                RoleName = roleName
            });

            AttachedPolicyLists[roleName] = result;
            return result;
        }

        public static async Task<Amazon.IdentityManagement.Model.GetPolicyResponse> GetPolicyMetadataAsync(
    IAmazonIdentityManagementService iamClient, string policyArn)
        {
            if (PolicyMetadata.TryGetValue(policyArn, out var cached))
            {
                return cached;
            }

            var policy = await iamClient.GetPolicyAsync(new Amazon.IdentityManagement.Model.GetPolicyRequest
            {
                PolicyArn = policyArn
            });

            PolicyMetadata[policyArn] = policy;
            return policy;
        }

        public static async Task<GetPolicyVersionResponse> GetPolicyVersionAsync(
    IAmazonIdentityManagementService iamClient, string policyArn, string versionId)
        {
            var key = (policyArn, versionId);
            if (PolicyVersions.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var version = await iamClient.GetPolicyVersionAsync(new GetPolicyVersionRequest
            {
                PolicyArn = policyArn,
                VersionId = versionId
            });

            PolicyVersions[key] = version;
            return version;
        }
        
        public static async Task<Amazon.Lambda.Model.GetPolicyResponse> GetLambdaPolicyAsync(IAmazonLambda lambdaClient, string functionName)
        {
            if (LambdaPolicies.TryGetValue(functionName, out var cached))
            {
                return cached;
            }

            var policy = await lambdaClient.GetPolicyAsync(new GetPolicyRequest { FunctionName = functionName });

            LambdaPolicies[functionName] = policy;
            return policy;
        }

        // Optional: expose clearing method for testing or resets
        public static void ClearAllCaches()
        {
            Buckets.Clear();
            LambdaFunctions.Clear();
            BucketNotifications.Clear();
            LambdaConfigs.Clear();
            InlinePolicyLists.Clear();
            InlinePolicies.Clear();
            AttachedPolicyLists.Clear();
            PolicyMetadata.Clear();
            PolicyVersions.Clear();
        }
    }
}
