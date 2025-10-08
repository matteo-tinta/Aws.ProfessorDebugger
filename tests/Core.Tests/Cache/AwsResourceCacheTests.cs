using System.Text.Json;
using Core.Cache;
using Core.Cache.Models;
using Core.Cache.Providers;
using Core.Tests.Test.Extensions;
using Tests.Shared.Builders;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Core.Tests.Cache;

public class AwsResourceCacheTests
{
    private readonly ICacheProvider<SerializableAwsCache> _cacheProvider = Substitute.For<ICacheProvider<SerializableAwsCache>>();
    private readonly ModelBuilder<SerializableAwsCache> _testCache = new(() => new SerializableAwsCache()
    {
        Checksum = "default",
        CreatedAt = DateTime.Now,
        TTL = (int)TimeSpan.FromDays(1).TotalMinutes
    });

    public static TheoryData<string, Action<ModelBuilder<SerializableAwsCache>>> CacheInvalidationActions => new()
    {
        { "checksum is invalid", model => model.Set(x => x.Checksum, "invalid") },
        { "checksum is null" , model => model.Set(x => x.Checksum, null) }, 
        { "TTL is expired", model => model.Set(x => x.CreatedAt, DateTime.UtcNow.AddMinutes(-(int)TimeSpan.FromDays(1).TotalMinutes)) },
    };
    
    [Theory]
    [MemberData(nameof(CacheInvalidationActions))]
    internal async Task Cache_Is_Invalidated_When(string message, Action<ModelBuilder<SerializableAwsCache>> action)
    {
       //Arrange
       action(_testCache);
       
       _cacheProvider.GetAsync().Returns(_testCache.Build());
       
       //Act
       await AwsResourceCache.InitializeAsync(_cacheProvider);
       
       //Assert
       Assert.False(AwsResourceCache.CacheHasBeenInitialized, $"Cache has been initialized, but {message}");
    }

    [Fact]
    internal async Task Cache_Is_Invalidated_When_CacheProviderThrows()
    {
        //Arrange
        _cacheProvider.GetAsync().Throws(new JsonException("Unable to serialize cache"));
       
        //Act
        await AwsResourceCache.InitializeAsync(_cacheProvider);
       
        //Assert
        Assert.False(AwsResourceCache.CacheHasBeenInitialized, $"Cache has been initialized, but cache provider threw");
    }
    
    [Fact(Skip = "This test tests against a static class which is already initialized when testing")]
    internal async Task A_Cache_Is_Written_As_Expected()
    {
        //Arrange
        _cacheProvider.GetAsync().Returns(_testCache.Build());

        SerializableAwsCache savedCache = null!;
        await _cacheProvider.SaveAsync(Arg.Do<SerializableAwsCache>(x => savedCache = x));
       
        //Act
        await AwsResourceCache.InitializeAsync(_cacheProvider);
        await AwsResourceCache.SaveToDiskAsync(_cacheProvider);
       
        //Assert
        savedCache.ShouldBeEquivalentOf(_testCache.Build());
    }
}