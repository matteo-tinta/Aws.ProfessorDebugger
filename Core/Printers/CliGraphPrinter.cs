using System.Reflection.Emit;
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
            WriteNodeWithColors(node, level);
            foreach (var child in node.Children)
            {
                PrintChildrenGraph(child, level + 1);
            }
        }

        private static void PrintParentGraph(AwsResourceNode node, int level = 1)
        {
            WriteNodeWithColors(node, level);
            foreach (var parent in node.Parents)
            {
                PrintParentGraph(parent, level + 1);
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
