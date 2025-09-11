using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using Core.Cache.Providers;
using Core.Enumerators;

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
        private static readonly List<FunctionConfiguration> _lambdaFunctions = new();
        private static readonly List<S3Bucket> _buckets = new();
        private static readonly ConcurrentDictionary<string, ListEventSourceMappingsResponse> _lambdaEventSourceEvents = new();
        private static readonly ConcurrentDictionary<string, Amazon.SimpleNotificationService.Model.ListSubscriptionsByTopicResponse> _snsSubscriptions = new();
        private static readonly ConcurrentDictionary<string, ListEventSourceMappingsResponse> _sqsLambdaTriggersEvents = new();
        private static readonly ConcurrentDictionary<string, Parameter> _ssmParameters = new();
        private static readonly ConcurrentDictionary<string, GetBucketNotificationResponse> _bucketNotifications = new();
        private static readonly ConcurrentDictionary<string, GetFunctionConfigurationResponse> _lambdaConfigs = new();
        private static readonly ConcurrentDictionary<string, ListRolePoliciesResponse> _inlinePolicyLists = new();
        private static readonly ConcurrentDictionary<(string roleName, string policyName), GetRolePolicyResponse> _inlinePolicies = new();
        private static readonly ConcurrentDictionary<string, ListAttachedRolePoliciesResponse> _attachedPolicyLists = new();
        private static readonly ConcurrentDictionary<string, Amazon.IdentityManagement.Model.GetPolicyResponse> _policyMetadata = new();
        private static readonly ConcurrentDictionary<(string policyArn, string versionId), GetPolicyVersionResponse> _policyVersions = new();

        private static bool _initialized = false;

        public static bool CacheHasBeenInitialized { get; private set; } = false;

        public static async Task InitializeAsync(ICacheProvider<SerializableAwsCache> cacheProvider, AwsResourceCacheInitOptions options)
        {
            if (_initialized) return;

            if (options?.IgnoreCacheAndOverride == true)
            {
                Console.WriteLine("Ignoring cache...");
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

                _lambdaFunctions.AddRange(cache.LambdaFunctions ?? []);
                _buckets.AddRange(cache.Buckets ?? []);

                foreach (var kv in cache.SnsSubscriptions ?? []) _snsSubscriptions[kv.Key] = kv.Value;
                foreach (var kv in cache.SqsLambdaTriggerEvents ?? []) _sqsLambdaTriggersEvents[kv.Key] = kv.Value;
                foreach (var kv in cache.LambdaEventSourceEvents ?? []) _lambdaEventSourceEvents[kv.Key] = kv.Value;
                foreach (var kv in cache.SsmParameters ?? []) _ssmParameters[kv.Key] = kv.Value;
                foreach (var kv in cache.BucketNotifications ?? []) _bucketNotifications[kv.Key] = kv.Value;
                foreach (var kv in cache.LambdaConfigs ?? []) _lambdaConfigs[kv.Key] = kv.Value;
                foreach (var kv in cache.InlinePolicyLists ?? []) _inlinePolicyLists[kv.Key] = kv.Value;
                foreach (var kv in cache.InlinePolicies ?? []) _inlinePolicies[(kv.Key.Split('|')[0], kv.Key.Split('|')[1])] = kv.Value;
                foreach (var kv in cache.AttachedPolicies ?? []) _attachedPolicyLists[kv.Key] = kv.Value;
                foreach (var kv in cache.PolicyMetadata ?? []) _policyMetadata[kv.Key] = kv.Value;
                foreach (var kv in cache.PolicyVersions ?? []) _policyVersions[(kv.Key.Split('|')[0], kv.Key.Split('|')[1])] = kv.Value;

                CacheHasBeenInitialized = true;
            }
            catch
            {
                Console.WriteLine("Warning: Failed to load AWS cache. Continuing with empty cache.");
            }

            _initialized = true;
        }

        public static async Task SaveToDiskAsync(ICacheProvider<SerializableAwsCache> cacheProvider)
        {
            var cache = new SerializableAwsCache
            {
                LambdaFunctions = _lambdaFunctions.ToList(),
                Buckets = _buckets.ToList(),
                BucketNotifications = _bucketNotifications.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LambdaConfigs = _lambdaConfigs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InlinePolicyLists = _inlinePolicyLists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                InlinePolicies = _inlinePolicies.ToDictionary(kvp => $"{kvp.Key.roleName}|{kvp.Key.policyName}", kvp => kvp.Value),
                AttachedPolicies = _attachedPolicyLists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                PolicyMetadata = _policyMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                PolicyVersions = _policyVersions.ToDictionary(kvp => $"{kvp.Key.policyArn}|{kvp.Key.versionId}", kvp => kvp.Value),
                SsmParameters = _ssmParameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LambdaEventSourceEvents = _lambdaEventSourceEvents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SqsLambdaTriggerEvents = _sqsLambdaTriggersEvents.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SnsSubscriptions = _snsSubscriptions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

            await cacheProvider.SaveAsync(cache);
        }


        public static async Task<List<FunctionConfiguration>> GetLambdaFunctions(IAmazonLambda lambdaClient)
        {
            if (_lambdaFunctions.Count == 0)
            {
                await foreach (var function in LambdaEnumerators.ListAllLambdaFunctions(lambdaClient))
                {
                    _lambdaFunctions.Add(function);
                }
            }

            return _lambdaFunctions ?? [];
        }

        public static async Task<List<S3Bucket>> GetBuckets(IAmazonS3 s3Client)
        {
            if (_buckets.Count == 0)
            {
                Console.WriteLine(" == READING BUCKETS LIST FROM S3 == ");
                var buckets = await s3Client.ListBucketsAsync();
                _buckets.AddRange(buckets.Buckets);
            }

            return _buckets ?? [];
        }

        public static async Task<ListSubscriptionsByTopicResponse> GetSnsSubscriptionsByTopicArnAsync(IAmazonSimpleNotificationService snsClient, string snsArn)
        {
            if (_snsSubscriptions.TryGetValue(snsArn, out var cached))
            {
                return cached;
            }

            ListSubscriptionsByTopicResponse value = await snsClient.ListSubscriptionsByTopicAsync(snsArn);

            _snsSubscriptions[snsArn] = value;
            return value;
        }

        public static async Task<ListEventSourceMappingsResponse> GetSqsLambdaTriggersAsync(IAmazonLambda lambdaClient, string sqsArn)
        {
            if (_sqsLambdaTriggersEvents.TryGetValue(sqsArn, out var cached))
            {
                return cached;
            }

            var value = await lambdaClient.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest()
            {
                EventSourceArn = sqsArn
            });
            _sqsLambdaTriggersEvents[sqsArn] = value;
            return value;
        }

        public static async Task<ListEventSourceMappingsResponse> GetLambdaEventSourceMappingAsync(IAmazonLambda lambdaClient, string functionName)
        {
            if (_lambdaEventSourceEvents.TryGetValue(functionName, out var cached))
            {
                return cached;
            }

            var value = await lambdaClient.ListEventSourceMappingsAsync(new ListEventSourceMappingsRequest
            {
                FunctionName = functionName
            });
            _lambdaEventSourceEvents[functionName] = value;
            return value;
        }

        public static async Task<GetBucketNotificationResponse> GetBucketNotificationAsync(IAmazonS3 s3Client, string bucketName)
        {
            if (_bucketNotifications.TryGetValue(bucketName, out var cached))
            {
                return cached;
            }

            Console.WriteLine($" == READING BUCKETS {bucketName} NOTIFICATIONS == ");
            var result = await s3Client.GetBucketNotificationAsync(new GetBucketNotificationRequest
            {
                BucketName = bucketName
            });

            _bucketNotifications[bucketName] = result;
            return result;
        }

        public static async Task<Parameter> GetSsmParameter(IAmazonSimpleSystemsManagement ssmClient, string parameterPath)
        {
            if (_ssmParameters.TryGetValue(parameterPath, out var cached))
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

                _ssmParameters[parameterPath] = config.Parameters[0];
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
            if (_lambdaConfigs.TryGetValue(functionName, out var cached))
            {
                return cached;
            }

            var config = await lambdaClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest
            {
                FunctionName = functionName
            });

            _lambdaConfigs[functionName] = config;
            return config;
        }

        public static async Task<GetRolePolicyResponse> GetInlinePolicyAsync(
    IAmazonIdentityManagementService iamClient, string roleName, string policyName)
        {
            var key = (roleName, policyName);

            if (_inlinePolicies.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var policy = await iamClient.GetRolePolicyAsync(new GetRolePolicyRequest
            {
                RoleName = roleName,
                PolicyName = policyName
            });

            _inlinePolicies[key] = policy;
            return policy;
        }


        public static async Task<ListRolePoliciesResponse> GetInlinePolicyListAsync(
    IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (_inlinePolicyLists.TryGetValue(roleName, out var cached))
            {
                return cached;
            }

            var list = await iamClient.ListRolePoliciesAsync(new ListRolePoliciesRequest
            {
                RoleName = roleName
            });

            _inlinePolicyLists[roleName] = list;
            return list;
        }

        public static async Task<ListAttachedRolePoliciesResponse> GetAttachedPoliciesAsync(
    IAmazonIdentityManagementService iamClient, string roleName)
        {
            if (_attachedPolicyLists.TryGetValue(roleName, out var cached))
            {
                return cached;
            }

            var result = await iamClient.ListAttachedRolePoliciesAsync(new ListAttachedRolePoliciesRequest
            {
                RoleName = roleName
            });

            _attachedPolicyLists[roleName] = result;
            return result;
        }

        public static async Task<Amazon.IdentityManagement.Model.GetPolicyResponse> GetPolicyMetadataAsync(
    IAmazonIdentityManagementService iamClient, string policyArn)
        {
            if (_policyMetadata.TryGetValue(policyArn, out var cached))
            {
                return cached;
            }

            var policy = await iamClient.GetPolicyAsync(new Amazon.IdentityManagement.Model.GetPolicyRequest
            {
                PolicyArn = policyArn
            });

            _policyMetadata[policyArn] = policy;
            return policy;
        }

        public static async Task<GetPolicyVersionResponse> GetPolicyVersionAsync(
    IAmazonIdentityManagementService iamClient, string policyArn, string versionId)
        {
            var key = (policyArn, versionId);
            if (_policyVersions.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var version = await iamClient.GetPolicyVersionAsync(new GetPolicyVersionRequest
            {
                PolicyArn = policyArn,
                VersionId = versionId
            });

            _policyVersions[key] = version;
            return version;
        }

        // Optional: expose clearing method for testing or resets
        public static void ClearAllCaches()
        {
            _buckets.Clear();
            _lambdaFunctions.Clear();
            _bucketNotifications.Clear();
            _lambdaConfigs.Clear();
            _inlinePolicyLists.Clear();
            _inlinePolicies.Clear();
            _attachedPolicyLists.Clear();
            _policyMetadata.Clear();
            _policyVersions.Clear();
        }
    }
}
