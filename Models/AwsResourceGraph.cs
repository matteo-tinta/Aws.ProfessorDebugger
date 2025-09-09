using System.Text.RegularExpressions;

namespace Models
{
    public class AwsResourceGraph
    {
        public Dictionary<string, AwsResourceNode> Nodes = new();

        public AwsResourceNode GetOrCreateNode(string arn, string type)
        {
            if (!Nodes.TryGetValue(arn, out var node))
            {
                node = new AwsResourceNode
                {
                    Arn = arn,
                    Name = arn.Split(":").Last(),
                    Type = type
                };
                Nodes[arn] = node;
            }

            return node;
        }

        public IEnumerable<AwsResourceNode> GetAllNodes() => Nodes.Values;
    }
}
