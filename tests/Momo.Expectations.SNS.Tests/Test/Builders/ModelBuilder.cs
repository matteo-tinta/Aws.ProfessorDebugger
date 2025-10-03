using System.Linq.Expressions;
using System.Reflection;

namespace Momo.Expectations.SNS.Tests.Test.Builders;

/// <summary>
/// Allow to create custom complex object by setting properties via reflection
/// </summary>
/// <param name="happyPathFactory">Costruct the minimal valid object</param>
/// <typeparam name="T">The type of the object to construct</typeparam>
/// <code> var modelBuilder = new ModelBuilder<![CDATA[<UploadListModel>]]>(() => new UploadListModel
/// {
///     Id = Guid.NewGuid(),
/// });
///
/// var built = modelBuilder.Set(x => x.Id, Guid.Empty()).Build
///
/// // built.Id = "00000000-0000-0000-0000-000000000000"
/// </code>
internal sealed class ModelBuilder<T>(Func<T> happyPathFactory)
    where T : class
{
    private readonly T _model = happyPathFactory();

    /// <summary>
    /// Creates a new instance and will init the new object with the happy path case. The old model will be lost
    /// </summary>
    public ModelBuilder<T> New() => new(happyPathFactory);

    /// <summary>
    /// Build the final model
    /// </summary>
    public T Build() => _model;

    /// <summary>
    /// Set a property in the model with the given value
    /// </summary>
    /// <param name="selector">The property to set</param>
    /// <param name="value">The value to assign</param>
    /// <exception cref="ArgumentException">Thrown when selector is not a property access (such as methods)</exception>
    /// <exception cref="InvalidOperationException">Thrown when given property was not found, or it's not publicly writable</exception>
    public ModelBuilder<T> Set<TProperty>(
        Expression<Func<T, TProperty>> selector,
        TProperty value)
    {
        if (selector.Body is not MemberExpression memberExpr)
            throw new ArgumentException("Selector must be a property access", nameof(selector));

        PropertyInfo? property = typeof(T).GetProperty(memberExpr.Member.Name);
        if (property == null || !property.CanWrite)
        {
            throw new InvalidOperationException($"Property '{memberExpr.Member.Name}' not found or not writable.");
        }

        property.SetValue(_model, value);
        return this;
    }
}