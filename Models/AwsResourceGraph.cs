namespace Models
{
    public class AwsResourceGraph
    {
        private Dictionary<string, AwsResourceNode> _nodes = new();

        public AwsResourceNode GetOrCreateNode(string arn, string type)
        {
            if (!_nodes.TryGetValue(arn, out var node))
            {
                node = new AwsResourceNode
                {
                    Arn = arn,
                    Type = type
                };
                _nodes[arn] = node;
            }

            return node;
        }

        public IEnumerable<AwsResourceNode> GetAllNodes() => _nodes.Values;
    }
}
