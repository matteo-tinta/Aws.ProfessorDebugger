using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Model;
using Amazon.S3.Model;

namespace Core.Cache
{
    internal class SerializableAwsCache
    {
        public List<FunctionConfiguration> LambdaFunctions { get; set; } = [];
        public List<S3Bucket> Buckets { get; set; } = [];
        public Dictionary<string, GetBucketNotificationResponse> BucketNotifications { get; set; } = [];
        public Dictionary<string, GetFunctionConfigurationResponse> LambdaConfigs { get; set; } = [];
        public Dictionary<string, ListRolePoliciesResponse> InlinePolicyLists { get; set; } = [];
        public Dictionary<string, GetRolePolicyResponse> InlinePolicies { get; set; } = [];
        public Dictionary<string, ListAttachedRolePoliciesResponse> AttachedPolicies { get; set; } = [];
        public Dictionary<string, Amazon.IdentityManagement.Model.GetPolicyResponse> PolicyMetadata { get; set; } = [];
        public Dictionary<string, GetPolicyVersionResponse> PolicyVersions { get; set; } = [];
    }
}
