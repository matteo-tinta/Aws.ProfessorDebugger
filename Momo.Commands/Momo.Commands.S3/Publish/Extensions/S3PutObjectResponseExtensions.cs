using System.Net;
using Amazon.S3.Model;

namespace Momo.Commands.S3.Publish.Extensions;

internal static class S3PutObjectResponseExtensions
{
    internal static void EnsureSuccessStatusCode(this PutObjectResponse response)
    {
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Command failed, status code was {response.HttpStatusCode}");
        }
    }
}