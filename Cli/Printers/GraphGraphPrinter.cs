using System.Text.Json;
using Models;

namespace Cli.Printers
{
    internal class GraphGraphPrinter : IGraphPrinter
    {
        public void Print(AwsResourceGraph graph, AwsResourceNode node)
        {
            //Clearing the console so that the output is clean
            Console.Clear();

            var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            });

            Console.WriteLine(json);
        }
    }

}
