namespace Models
{
    public class AwsResourceNode
    {
        public string Arn { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Lambda, SNS, etc.

        public HashSet<string> Parents { get; } = new();
        public HashSet<string> Children { get; } = new();
    }
}
