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

            JsonGraphSerializable json = new JsonGraphSerializable()
            {
                Arn = node.Arn,
                Name = node.Name,
                Type = node.Type,
                Children = BuildChildren(graph, node),
                Parents = BuildParents(graph, node)
            };

            var serializedJson = JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });

            Console.WriteLine(serializedJson);
        }

        private static Dictionary<string, JsonGraphSerializable> BuildChildren(AwsResourceGraph graph, AwsResourceNode node)
        {
            Dictionary<string, JsonGraphSerializable> children = [];
            foreach (var child in node.Children)
            {
                var childNode = graph.GetOrCreateNode(child);
                children.Add(child, new JsonGraphSerializable()
                {
                    Arn = childNode.Arn,
                    Name = childNode.Name,
                    Type = childNode.Type,
                    Children = BuildChildren(graph, childNode),
                    Parents = BuildParents(graph, childNode),
                });
            }

            return children;
        }

        private static Dictionary<string, JsonGraphSerializable> BuildParents(AwsResourceGraph graph, AwsResourceNode node)
        {
            Dictionary<string, JsonGraphSerializable> parents = [];
            foreach (var parent in node.Parents)
            {
                var parentNode = graph.GetOrCreateNode(parent);
                parents.Add(parent, new JsonGraphSerializable()
                {
                    Arn = parentNode.Arn,
                    Name = parentNode.Name,
                    Type = parentNode.Type,
                    Children = BuildChildren(graph, parentNode),
                    Parents = BuildParents(graph, parentNode),
                });
            }

            return parents;
        }
    }

}
