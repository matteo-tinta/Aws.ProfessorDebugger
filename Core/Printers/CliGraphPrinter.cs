using Amazon.Runtime.Internal.Util;
using Models;

namespace Core.Printers
{
    internal class CliGraphPrinter: IGraphPrinter
    {
        public void Print(AwsResourceGraph graph)
        {
            Console.WriteLine("\r\n=========== CHILDREN ==========\r\n");

            foreach (var node in graph.GetAllNodes())
            {
                PrintChildrenGraph(node);
            }

            Console.WriteLine("\r\n=========== PARENTS ===========\r\n");

            foreach (var node in graph.GetAllNodes())
            {
                PrintParentGraph(node);
            }
        }

        private static void PrintChildrenGraph(AwsResourceNode node, int level = 1)
        {
            Console.WriteLine($"{new string('-', level)}> [{node.Type}] {node.Arn}");
            foreach (var child in node.Children)
            {
                PrintChildrenGraph(child, level + 1);
            }
        }

        private static void PrintParentGraph(AwsResourceNode node, int level = 1)
        {
            Console.WriteLine($"{new string('-', level)}> [{node.Type}] {node.Arn}");
            foreach (var parent in node.Parents)
            {
                PrintParentGraph(parent, level + 1);
            }
        }
    }

}
