using NJsonSchema;

namespace Momo.Expectations.SNS.Tests.Test.Extensions;

internal static class JsonSchemaPropertiesExtensions
{
    internal static void ShouldMatchTypeAndFormat(this JsonSchemaProperty property, JsonObjectType type, string? format)
    {
        Assert.That(property.Type, Is.EqualTo(type));

        if (!String.IsNullOrWhiteSpace(format))
        {
            Assert.That(property.Format, Is.EqualTo(format));
        }
    }
}