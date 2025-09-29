using Amazon.SimpleSystemsManagement.Model;
using Cli.FileLoader;
using Cli.FileLoader.Models;
using Core.Models;
using Momo.Expectations;
using Momo.Expectations.Parallel.Expectations;
using Momo.Models;

namespace Cli.Printers
{
    internal class MomoGraphPrinter: IGraphPrinter
    {
        private readonly string _path;
        private readonly HashSet<string> _visited = [];

        public MomoGraphPrinter(string path)
        {
            _path = path;
        }

        public void Print(AwsResourceGraph graph, AwsResourceNode node)
        {
            var file = new MomoExpectationFile()
            {
                Expectations = [],
                Timeout = 120,
                TraceId = Guid.NewGuid().ToString()
            };
            
            var currentNode = GetNode(node);
            if (currentNode is not null)
            {
                file.Expectations = [currentNode];
            }
            
            var parentFile = PopulateParentNode(graph, node, new MomoExpectationFile() { Expectations = [] }) ?? file;
            var childrenFile = PopulateChildrenNode(graph, node, new MomoExpectationFile() { Expectations = [] }) ?? file;

            List<IMomoExpectation> firstChildren = childrenFile.Expectations is { Count: > 0 } 
                ? [childrenFile.Expectations.ElementAt(0)] 
                : [];
                
            var final = file with
            {
                Expectations = parentFile.Expectations.Reverse().Concat(firstChildren).ToList()
            };
            
            MomoFileLoader.SaveAsync(final, _path).GetAwaiter().GetResult();
            Console.WriteLine($"File saved to {_path}");
            
            MomoFileLoader.Print(final);
        }
        
        private MomoExpectationFile? PopulateChildrenNode(AwsResourceGraph graph, AwsResourceNode node, MomoExpectationFile file, int level = 1)
        {
            if (!_visited.Add(node.Arn) && level > 1)
                return null; // already visited but ignores first level because it's the node

            var newNode = GetNode(node);
            var newList = new List<IMomoExpectation>(file.Expectations);
            
            if (newNode is not null && level > 1)
            {
                newList.Add(newNode);
            }
            
            var childList = new List<IMomoExpectation>();
            
            foreach (var child in node.Children)
            {
                var graphNode = graph.GetOrCreateNode(child);
                var childFile = PopulateChildrenNode(graph, graphNode, new MomoExpectationFile() { Expectations = [] }, level + 1);

                if (childFile?.Expectations is not { Count: > 0 }) 
                    continue;
                
                childList.AddRange(childFile.Expectations.Reverse());
            }

            if (node.Children is { Count: > 1 })
            {
                newList.Add(new MomoParallelExpectation()
                {
                    ParallelExpectations = childList,
                });
            }
            else
            {
                newList.AddRange(childList);
            }

            return file with { Expectations = newList };
        }

        private MomoExpectationFile? PopulateParentNode(AwsResourceGraph graph, AwsResourceNode node, MomoExpectationFile file, int level = 1)
        {
            if (!_visited.Add(node.Arn) && level > 1)
                return null; // already visited but ignores first level because it's the node

            var newNode = GetNode(node);
            var newList = new List<IMomoExpectation>(file.Expectations);
            
            if (newNode is not null && level > 1)
            {
                newList.Add(newNode);
            }
            
            var parentList = new List<IMomoExpectation>();
            
            foreach (var parent in node.Parents)
            {
                var graphNode = graph.GetOrCreateNode(parent);
                var parentFile = PopulateParentNode(graph, graphNode, file, level + 1);

                if (parentFile?.Expectations is not { Count: > 0 }) 
                    continue;

                if (parentFile?.Expectations is { Count: > 1 })
                {
                    parentList.Add(new MomoParallelExpectation()
                    {
                        ParallelExpectations = parentFile.Expectations.Reverse().ToList()
                    });    
                }
                else
                {
                    parentList.AddRange(parentFile?.Expectations ?? []);
                }
            }

            newList.AddRange(parentList);

            return file with { Expectations = newList };
        }

        private IMomoExpectation? GetNode(AwsResourceNode node)
        {
            var type = node.Type.ToLower().Trim();
            
            return type switch
            {
                "sns" => new MomoAwsSnsExpectationJsonModel() { Arn = node.Arn }.Build(),
                "s3" => new MomoAwsS3ExpectationJsonModel()
                {
                    Arn = node.Arn, 
                    File = new MomoAwsS3FileJsonModel()
                    {
                        Key = "fill_this",
                        Prefix = "fill_this"
                    }
                }.Build(),
                _ => null
            };
        }
    }

}
