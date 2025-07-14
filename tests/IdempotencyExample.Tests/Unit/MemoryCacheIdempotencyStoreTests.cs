using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IdempotencyExample.Api.Idempotency;

namespace IdempotencyExample.Tests.Unit;

public class MemoryCacheIdempotencyStoreTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<MemoryCacheIdempotencyStore>> _loggerMock;
    private readonly MemoryCacheIdempotencyStore _store;

    public MemoryCacheIdempotencyStoreTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<MemoryCacheIdempotencyStore>>();
        _store = new MemoryCacheIdempotencyStore(_memoryCache, _loggerMock.Object);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreRecord()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "test-key",
            StatusCode = 200,
            ResponseBody = "{\"success\": true}"
        };
        var ttl = TimeSpan.FromMinutes(5);

        // Act
        await _store.SaveAsync(record, ttl);

        // Assert
        var retrieved = await _store.GetAsync("test-key");
        Assert.NotNull(retrieved);
        Assert.Equal("test-key", retrieved.Key);
        Assert.Equal(200, retrieved.StatusCode);
        Assert.Equal("{\"success\": true}", retrieved.ResponseBody);
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotExists_ShouldReturnNull()
    {
        // Act
        var result = await _store.GetAsync("non-existing-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnRecord()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "existing-key",
            StatusCode = 201,
            ResponseBody = "{\"id\": 123}"
        };
        await _store.SaveAsync(record, TimeSpan.FromMinutes(1));

        // Act
        var result = await _store.GetAsync("existing-key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("existing-key", result.Key);
        Assert.Equal(201, result.StatusCode);
    }

    [Fact]
    public async Task SaveAsync_WithTtl_ShouldExpireAfterTtl()
    {
        // Arrange
        var record = new IdempotencyRecord
        {
            Key = "ttl-test-key",
            StatusCode = 200,
            ResponseBody = "{\"test\": true}"
        };
        var shortTtl = TimeSpan.FromMilliseconds(100);

        // Act
        await _store.SaveAsync(record, shortTtl);
        
        // Initial check - should exist
        var immediate = await _store.GetAsync("ttl-test-key");
        Assert.NotNull(immediate);

        // Wait for expiration
        await Task.Delay(150);

        // Should be expired
        var expired = await _store.GetAsync("ttl-test-key");
        Assert.Null(expired);
    }
}
