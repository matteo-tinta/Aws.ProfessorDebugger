using Models;

namespace Core.Printers
{
    internal interface IGraphPrinter
    {
        public void Print(AwsResourceGraph graph, AwsResourceNode node);
    }

}
