using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Core.Enumerators
{
    internal static class LambdaEnumerators
    {
        public static async IAsyncEnumerable<FunctionConfiguration> ListAllLambdaFunctions(IAmazonLambda lambda)
        {
            string marker = null;

            do
            {
                var request = new ListFunctionsRequest
                {
                    Marker = marker,
                    MaxItems = 50
                };

                var response = await lambda.ListFunctionsAsync(request);

                foreach (var function in response.Functions)
                {
                    yield return function;
                }

                marker = response.NextMarker;

            } while (!string.IsNullOrEmpty(marker));
        }
    }
}
