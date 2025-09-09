namespace Models
{
    public class AwsResourceNode
    {
        public string Arn { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Lambda, SNS, etc.

        public HashSet<AwsResourceNode> Parents { get; } = new();
        public HashSet<AwsResourceNode> Children { get; } = new();
    }
}
