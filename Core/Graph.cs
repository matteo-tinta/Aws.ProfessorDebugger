namespace Core
{
    public class Node
    {
        public string Arn { get; }
        public string Type { get; }
        public List<Node> Sources { get; } = new List<Node>();

        public Node(string arn, string type)
        {
            Arn = arn;
            Type = type;
        }

        public override int GetHashCode() => Arn.GetHashCode();
        public override bool Equals(object obj) => obj is Node other && other.Arn == Arn;
    }

    public class Graph
    {
        private readonly Dictionary<string, Node> _nodes = new Dictionary<string, Node>();

        public void AddNode(Node node)
        {
            if (!_nodes.ContainsKey(node.Arn))
            {
                _nodes.Add(node.Arn, node);
            }
        }

        public void AddEdge(Node source, Node destination)
        {
            if (_nodes.TryGetValue(destination.Arn, out var destNode))
            {
                if (!_nodes.TryGetValue(source.Arn, out var srcNode))
                {
                    // If source node is not in the graph, add it
                    AddNode(source);
                    srcNode = source;
                }
                destNode.Sources.Add(srcNode);
            }
        }

        public void PrintGraph()
        {
            foreach (var node in _nodes.Values)
            {
                Console.WriteLine($"[{node.Type}] {node.Arn}");
                foreach (var source in node.Sources)
                {
                    Console.WriteLine($"  <-- [{source.Type}] {source.Arn}");
                }
                Console.WriteLine();
            }
        }
    }
}
