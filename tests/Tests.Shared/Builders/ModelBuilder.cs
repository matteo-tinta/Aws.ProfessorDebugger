using System.Linq.Expressions;
using System.Reflection;

namespace Tests.Shared.Builders;

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
public sealed class ModelBuilder<T>(Func<T> happyPathFactory)
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
    /// <exception cref="InvalidOperationException">Thrown when given property was not found, it's not writable or the parent object is null</exception>
    public ModelBuilder<T> Set<TProperty>(
        Expression<Func<T, TProperty>> selector,
        TProperty value)
    {
        if (selector.Body is not MemberExpression memberExpr)
            throw new ArgumentException("Selector must be a property access", nameof(selector));

        // Walk the expression tree to get the property chain
        Stack<MemberInfo> members = new();
        Expression? currentExpr = memberExpr;
        while (currentExpr is MemberExpression currentMemberExpr)
        {
            members.Push(currentMemberExpr.Member);
            currentExpr = currentMemberExpr.Expression;
        }

        // Start from the root model
        object? obj = _model;
        while (members.Count > 1) // Stop at the second-to-last member
        {
            MemberInfo member = members.Pop();
            if (member is not PropertyInfo prop)
                throw new InvalidOperationException($"Member {member.Name} is not a property.");

            obj = prop.GetValue(obj);
            if (obj == null)
                throw new InvalidOperationException($"Property {prop.Name} is null. Cannot set nested property.");
        }

        // Set the final property
        PropertyInfo? finalProperty = members.Pop() as PropertyInfo;
        if (finalProperty == null || !finalProperty.CanWrite)
            throw new InvalidOperationException($"Property '{finalProperty?.Name}' not found or not writable.");

        finalProperty.SetValue(obj, value);

        return this;
    }
}
