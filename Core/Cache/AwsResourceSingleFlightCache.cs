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

public class AwsResourceSingleFlightCache
{
    private readonly IAmazonS3 s3Client;
    private readonly IAmazonLambda lambdaClient;
    private readonly IAmazonSimpleSystemsManagement ssmClient;
    private readonly IAmazonSimpleNotificationService snsClient;
    private readonly IAmazonIdentityManagementService iamClient;
    private readonly AmazonSQSClient sqsClient;
    private readonly SingleFlightCache cache;

    public AwsResourceSingleFlightCache(
        IAmazonS3 s3Client,
        IAmazonLambda lambdaClient,
        IAmazonSimpleSystemsManagement ssmClient, 
        IAmazonSimpleNotificationService snsClient, 
        IAmazonIdentityManagementService iamClient,
        AmazonSQSClient sqsClient
        )
    {
        this.s3Client = s3Client;
        this.lambdaClient = lambdaClient;
        this.ssmClient = ssmClient;
        this.snsClient = snsClient;
        this.iamClient = iamClient;
        this.sqsClient = sqsClient;

        cache = new SingleFlightCache();
    }

    // Example for S3 buckets
    public Task<List<S3Bucket>> GetBucketsAsync()
    {
        string cacheKey = nameof(AwsResourceCache.GetBuckets);
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetBuckets(s3Client));
    }

    // Lambda functions
    public Task<List<FunctionConfiguration>> GetLambdaFunctionsAsync()
    {
        string cacheKey = nameof(AwsResourceCache.GetLambdaFunctions);
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaFunctions(lambdaClient));
    }

    // SSM Parameter
    public Task<Parameter> GetSsmParameterAsync(string parameterPath)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSsmParameter)}:{parameterPath}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetSsmParameter(ssmClient, parameterPath));
    }

    // Lambda Config
    public Task<GetFunctionConfigurationResponse> GetLambdaConfigAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaConfigAsync)}:{functionName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaConfigAsync(lambdaClient, functionName));
    }

    // SQS Lambda triggers
    public Task<ListEventSourceMappingsResponse> GetSqsLambdaTriggersAsync(string sqsArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsLambdaTriggersAsync)}:{sqsArn}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsLambdaTriggersAsync(lambdaClient, sqsArn));
    }
    
    // SQS Attributes
    public Task<GetQueueAttributesResponse> GetSqsQueueAttributes(string sqsUrl)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsQueueAttributes)}:{sqsUrl}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsQueueAttributes(sqsClient, sqsUrl));
    }
    
    // SQS QueueUrl
    public Task<GetQueueUrlResponse> GetSqsQueueUrl(string sqsName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSqsQueueUrl)}:{sqsName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetSqsQueueUrl(sqsClient, sqsName));
    }

    // Lambda EventSourceMappings
    public Task<ListEventSourceMappingsResponse> GetLambdaEventSourceMappingAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaEventSourceMappingAsync)}:{functionName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaEventSourceMappingAsync(lambdaClient, functionName));
    }
    
    // Lambda EventSourceMappings
    public Task<GetPolicyResponse> GetLambdaPolicyAsync(string functionName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetLambdaPolicyAsync)}:{functionName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetLambdaPolicyAsync(lambdaClient, functionName));
    }

    // Bucket notifications
    public Task<GetBucketNotificationResponse> GetBucketNotificationAsync(string bucketName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetBucketNotificationAsync)}:{bucketName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetBucketNotificationAsync(s3Client, bucketName));
    }

    // SNS subscriptions
    public Task<ListSubscriptionsByTopicResponse> GetSnsSubscriptionsByTopicArnAsync(string snsArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetSnsSubscriptionsByTopicArnAsync)}:{snsArn}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetSnsSubscriptionsByTopicArnAsync(snsClient, snsArn));
    }

    // Inline Policy List
    public Task<ListRolePoliciesResponse> GetInlinePolicyListAsync(string roleName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetInlinePolicyListAsync)}:{roleName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetInlinePolicyListAsync(iamClient, roleName));
    }

    // Inline Policy
    public Task<GetRolePolicyResponse> GetInlinePolicyAsync(string roleName, string policyName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetInlinePolicyAsync)}:{roleName}|{policyName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetInlinePolicyAsync(iamClient, roleName, policyName));
    }

    // Attached Policies
    public Task<ListAttachedRolePoliciesResponse> GetAttachedPoliciesAsync(string roleName)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetAttachedPoliciesAsync)}:{roleName}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetAttachedPoliciesAsync(iamClient, roleName));
    }

    // Policy metadata
    public Task<Amazon.IdentityManagement.Model.GetPolicyResponse> GetPolicyMetadataAsync(string policyArn)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetPolicyMetadataAsync)}:{policyArn}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetPolicyMetadataAsync(iamClient, policyArn));
    }

    // Policy version
    public Task<GetPolicyVersionResponse> GetPolicyVersionAsync(string policyArn, string versionId)
    {
        string cacheKey = $"{nameof(AwsResourceCache.GetPolicyVersionAsync)}:{policyArn}|{versionId}";
        return cache.GetAsync(cacheKey, () => AwsResourceCache.GetPolicyVersionAsync(iamClient, policyArn, versionId));
    }
}