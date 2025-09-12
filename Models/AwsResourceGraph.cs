namespace Models
{
    public class AwsResourceGraph
    {
        public Dictionary<string, AwsResourceNode> Nodes { get; set; } = [];

        public AwsResourceNode GetOrCreateNode(string arn)
        {
            if (!Nodes.TryGetValue(arn, out var node))
            {
                node = new AwsResourceNode
                {
                    Arn = arn,
                    Name = arn.Split(":").Last(),
                    Type = arn.Split(":").ElementAt(2)
                };
                Nodes[arn] = node;
            }

            return node;
        }

        public IEnumerable<AwsResourceNode> GetAllNodes() => Nodes.Values;
    }
}
