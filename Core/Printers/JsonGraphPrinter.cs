using System.Text.Json;
using Models;

namespace Core.Printers
{
    internal class JsonGraphPrinter : IGraphPrinter
    {
        public void Print(AwsResourceGraph graph)
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
