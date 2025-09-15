using System.Text.RegularExpressions;

namespace Core.Models
{
    /// <summary>
    /// Reppresent a resource ARN
    /// </summary>
    public record Arn
    {
        public string Partition { get; private set; }
        public string Service { get; private set; }
        public string Region { get; private set; }
        public string AccountId { get; private set; }
        public string ResourceName { get; private set; }
        public string ResourceArn { get; private set; }

        private Arn() { }

        public static Arn ParseArn(string arn)
        {
            if (string.IsNullOrWhiteSpace(arn))
                throw new ArgumentException("ARN cannot be null or empty");

            Regex arnRegex = new Regex(
                @"^arn:(?<partition>[^:]+):(?<service>[^:]*):(?<region>[^:]*):(?<accountId>[^:]*):(?<resource>.+)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var match = arnRegex.Match(arn);

            if (!match.Success)
                throw new ArgumentException("Invalid ARN format");

            string resource = match.Groups["resource"].Value;
            
            return new Arn
            {
                Partition = match.Groups["partition"].Value,
                Service = match.Groups["service"].Value,
                Region = match.Groups["region"].Value,
                AccountId = match.Groups["accountId"].Value,
                //Resource = resource,
                //ResourceType = resourceType,
                //ResourceId = resourceId,
                ResourceArn = arn,
                ResourceName = ExtractResourceName(resource)
            };
        }

        private static string ExtractResourceName(string resource)
        {
            var parts = resource.Split(new[] { ':' }, 2);
            var name = parts.Length == 2 ? parts[1] : resource;

            var index = name.IndexOf(':');
            if (index >= 0)
            {
                name = name.Substring(0, index);
            }

            return name;
        }
    }
}

