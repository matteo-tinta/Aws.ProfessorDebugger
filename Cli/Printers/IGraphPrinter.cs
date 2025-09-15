using Core.Models;

namespace Cli.Printers
{
    internal interface IGraphPrinter
    {
        public void Print(AwsResourceGraph graph, AwsResourceNode node);
    }

}
