using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Model;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS.Model;

namespace Core.Cache.Models
{
    internal class SerializableAwsCache
    {
        public List<FunctionConfiguration> LambdaFunctions { get; set; } = [];
        public List<S3Bucket> Buckets { get; set; } = [];
        public Dictionary<string, ListEventSourceMappingsResponse> LambdaEventSourceEvents { get; set; } = [];
        public Dictionary<string, Parameter> SsmParameters { get; set; } = [];
        public Dictionary<string, GetBucketNotificationResponse> BucketNotifications { get; set; } = [];
        public Dictionary<string, GetFunctionConfigurationResponse> LambdaConfigs { get; set; } = [];
        public Dictionary<string, ListRolePoliciesResponse> InlinePolicyLists { get; set; } = [];
        public Dictionary<string, GetRolePolicyResponse> InlinePolicies { get; set; } = [];
        public Dictionary<string, ListAttachedRolePoliciesResponse> AttachedPolicies { get; set; } = [];
        public Dictionary<string, Amazon.IdentityManagement.Model.GetPolicyResponse> PolicyMetadata { get; set; } = [];
        public Dictionary<string, GetPolicyVersionResponse> PolicyVersions { get; set; } = [];
        public Dictionary<string, ListEventSourceMappingsResponse> SqsLambdaTriggerEvents { get; set; } = [];
        public Dictionary<string, ListSubscriptionsByTopicResponse> SnsSubscriptions { get; set; } = [];
        public Dictionary<string, GetQueueAttributesResponse> SqsQueueAttributes { get; set; }
    }
}
