using NJsonSchema.Validation;

namespace Momo.Expectations.SNS.Exceptions;

public class SchemaValidationException(
    ICollection<ValidationError> validationErrors)
    : Exception(string.Join("\n", validationErrors.Select(c => $"{c.Path}(${c.LineNumber}:{c.LinePosition}): {c.Property} expect a kind of type {c.Kind} but it was {c.Token?.Type.ToString() ?? "invalid"} with value {c.Token}")))
{
    public List<ValidationError> ValidationErrors { get; } = validationErrors.ToList();
}