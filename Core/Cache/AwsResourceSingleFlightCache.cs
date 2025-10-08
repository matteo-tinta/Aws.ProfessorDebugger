using Amazon.S3;
using Amazon.S3.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using GetPolicyResponse = Amazon.Lambda.Model.GetPolicyResponse;

namespace Core.Cache;

internal class AwsResourceSingleFlightCache(
    IAmazonS3 s3Client,
    IAmazonLambda lambdaClient,
    IAmazonSimpleSystemsManagement ssmClient,
    IAmazonSimpleNotificationService snsClient,
    IAmazonIdentityManagementService iamClient,
    IAmazonSQS sqsClient)
{
    private readonly SingleFlightCache _cache = new();

    // Example for S3 buckets
    public async Task<List<S3Bucket>> GetBucketsAsync()
    {
        string cacheKey = nameof(AwsResourceCache.GetBuckets);
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetBuckets(s3Client)) ?? [];
    }

    // Lambda functions
    public async Task<List<FunctionConfiguration>> GetLambdaFunctionsAsync()
    {
        string cacheKey = nameof(AwsResourceCache.GetLambdaFunctions);
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaFunctions(lambdaClient)) ?? [];
    }

    // SSM Parameter
    public async Task<Parameter?> GetSsmParameterAsync(string parameterPath)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSsmParameter)}:{parameterPath}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetSsmParameter(ssmClient, parameterPath));
    }

    // Lambda Config
    public async Task<GetFunctionConfigurationResponse?> GetLambdaConfigAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaConfigAsync)}:{functionName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaConfigAsync(lambdaClient, functionName));
    }

    // SQS Lambda triggers
    public async Task<ListEventSourceMappingsResponse?> GetSqsLambdaTriggersAsync(string sqsArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsLambdaTriggersAsync)}:{sqsArn}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsLambdaTriggersAsync(lambdaClient, sqsArn));
    }
    
    // SQS Attributes
    public async Task<GetQueueAttributesResponse?> GetSqsQueueAttributes(string sqsUrl)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsQueueAttributes)}:{sqsUrl}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsQueueAttributes(sqsClient, sqsUrl));
    }
    
    // SQS QueueUrl
    public async Task<GetQueueUrlResponse?> GetSqsQueueUrl(string sqsName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsQueueUrl)}:{sqsName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsQueueUrl(sqsClient, sqsName));
    }

    // Lambda EventSourceMappings
    public async Task<ListEventSourceMappingsResponse?> GetLambdaEventSourceMappingAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaEventSourceMappingAsync)}:{functionName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaEventSourceMappingAsync(lambdaClient, functionName));
    }
    
    // Lambda EventSourceMappings
    public async Task<GetPolicyResponse?> GetLambdaPolicyAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaPolicyAsync)}:{functionName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaPolicyAsync(lambdaClient, functionName));
    }

    // Bucket notifications
    public async Task<GetBucketNotificationResponse?> GetBucketNotificationAsync(string bucketName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetBucketNotificationAsync)}:{bucketName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetBucketNotificationAsync(s3Client, bucketName));
    }

    // SNS subscriptions
    public async Task<ListSubscriptionsByTopicResponse?> GetSnsSubscriptionsByTopicArnAsync(string snsArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSnsSubscriptionsByTopicArnAsync)}:{snsArn}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetSnsSubscriptionsByTopicArnAsync(snsClient, snsArn));
    }

    // Inline Policy List
    public async Task<ListRolePoliciesResponse?> GetInlinePolicyListAsync(string roleName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetInlinePolicyListAsync)}:{roleName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetInlinePolicyListAsync(iamClient, roleName));
    }

    // Inline Policy
    public async Task<GetRolePolicyResponse?> GetInlinePolicyAsync(string roleName, string policyName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetInlinePolicyAsync)}:{roleName}|{policyName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetInlinePolicyAsync(iamClient, roleName, policyName));
    }

    // Attached Policies
    public async Task<ListAttachedRolePoliciesResponse?> GetAttachedPoliciesAsync(string roleName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetAttachedPoliciesAsync)}:{roleName}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetAttachedPoliciesAsync(iamClient, roleName));
    }

    // Policy metadata
    public async Task<Amazon.IdentityManagement.Model.GetPolicyResponse?> GetPolicyMetadataAsync(string policyArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetPolicyMetadataAsync)}:{policyArn}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetPolicyMetadataAsync(iamClient, policyArn));
    }

    // Policy version
    public async Task<GetPolicyVersionResponse?> GetPolicyVersionAsync(string policyArn, string versionId)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetPolicyVersionAsync)}:{policyArn}|{versionId}";
        return await _cache.GetAsync(cacheKey, () => AwsResourceCache.GetPolicyVersionAsync(iamClient, policyArn, versionId));
    }
}