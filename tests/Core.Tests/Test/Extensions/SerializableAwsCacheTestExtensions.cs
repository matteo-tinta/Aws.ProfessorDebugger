using System.Collections;
using Core.Cache.Models;
using FluentAssertions;

namespace Core.Tests.Test.Extensions;

internal static class SerializableAwsCacheTestExtensions
{
    /// <summary>
    /// This method checks the equivalent versus another serializable aws cache object.<br/>
    /// - IEnumerables (List, dictionaries, arrays, ...) that are null are equivalent to lists that are empty.
    /// </summary>
    internal static void ShouldBeEquivalentOf(this SerializableAwsCache cache, SerializableAwsCache to)
    {
        cache.Should().BeEquivalentTo(to, options => options
            .Using<IEnumerable>(ctx =>
            {
                if (ctx.Subject is string || ctx.Expectation is string)
                {
                    ctx.Subject.Should().Be(ctx.Expectation);
                    return;
                }

                bool subjectIsEmpty = ctx.Subject == null || !ctx.Subject.Cast<object>().Any();
                bool expectationIsEmpty = ctx.Expectation == null || !ctx.Expectation.Cast<object>().Any();

                if (subjectIsEmpty && expectationIsEmpty)
                    return;

                ctx.Subject.Should().BeEquivalentTo(ctx.Expectation);
            }).WhenTypeIs<IEnumerable>());
    }
}