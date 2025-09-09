using System.Reflection.Emit;
using Amazon.Runtime.Internal.Util;
using Models;

namespace Core.Printers
{
    internal class CliGraphPrinter: IGraphPrinter
    {
        public void Print(AwsResourceGraph graph, AwsResourceNode node)
        {
            Console.WriteLine("\r\n=========== CHILDREN ==========\r\n");
            PrintChildrenGraph(graph, node);

            Console.WriteLine("\r\n=========== PARENTS ===========\r\n");
            PrintParentGraph(graph, node);
        }

        private static void PrintChildrenGraph(AwsResourceGraph graph, AwsResourceNode node, int level = 1)
        {
            WriteNodeWithColors(node, level);
            foreach (var child in node.Children)
            {
                var graphNode = graph.GetOrCreateNode(child);
                PrintChildrenGraph(graph, graphNode, level + 1);
            }
        }

        private static void PrintParentGraph(AwsResourceGraph graph, AwsResourceNode node, int level = 1)
        {
            WriteNodeWithColors(node, level);
            foreach (var parent in node.Parents)
            {
                var graphNode = graph.GetOrCreateNode(parent);
                PrintParentGraph(graph, graphNode, level + 1);
            }
        }

        private static void WriteNodeWithColors(AwsResourceNode node, int level)
        {
            var type = node.Type.ToLower().Trim();
            Console.Write($"{new string('-', level)}> [");
            switch (type)
            {
                case "lambda":
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "sqs":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case "sns":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Green;
                    break;
            }

            Console.Write(type.ToUpper());
            Console.ResetColor();
            Console.Write($"] {node.Name}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($" ({node.Arn})\r\n");
        }
    }

}
