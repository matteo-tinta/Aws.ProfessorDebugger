using System.ComponentModel;
using System.Text.Json;
using Models;

namespace Core.Printers
{
    internal class JsonGraphSerializable
    {
        public string Arn { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, JsonGraphSerializable> Parents { get; set; }
        public Dictionary<string, JsonGraphSerializable> Children { get; set; }
    }

    internal class JsonGraphPrinter : IGraphPrinter
    {
        public void Print(AwsResourceGraph graph, AwsResourceNode node)
        {
            //Clearing the console so that the output is clean
            Console.Clear();

            var visited = new HashSet<string>();

            JsonGraphSerializable json = BuildNode(graph, node, visited);

            var serializedJson = JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });

            Console.WriteLine(serializedJson);
        }

        private static JsonGraphSerializable BuildNode(AwsResourceGraph graph, AwsResourceNode node, HashSet<string> visited)
        {
            if (visited.Contains(node.Arn))
            {
                // Prevent infinite recursion
                return new JsonGraphSerializable
                {
                    Arn = node.Arn,
                    Name = node.Name,
                    Type = node.Type,
                    Children = [],
                    Parents = []
                };
            }

            visited.Add(node.Arn);

            return new JsonGraphSerializable
            {
                Arn = node.Arn,
                Name = node.Name,
                Type = node.Type,
                Children = BuildChildren(graph, node, visited),
                Parents = BuildParents(graph, node, visited)
            };
        }

        private static Dictionary<string, JsonGraphSerializable> BuildChildren(AwsResourceGraph graph, AwsResourceNode node, HashSet<string> visited)
        {
            Dictionary<string, JsonGraphSerializable> children = [];
            foreach (var child in node.Children)
            {
                var childNode = graph.GetOrCreateNode(child);
                children[child] = BuildNode(graph, childNode, visited);
            }

            return children;
        }

        private static Dictionary<string, JsonGraphSerializable> BuildParents(AwsResourceGraph graph, AwsResourceNode node, HashSet<string> visited)
        {
            Dictionary<string, JsonGraphSerializable> parents = [];
            foreach (var parent in node.Parents)
            {
                var parentNode = graph.GetOrCreateNode(parent);
                parents[parent] = BuildNode(graph, parentNode, visited);
            }

            return parents;
        }
    }

}
