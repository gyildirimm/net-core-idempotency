using Microsoft.Extensions.Caching.Memory;

namespace IdempotencyExample.Api.Idempotency;

public class MemoryCacheIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheIdempotencyStore> _logger;

    public MemoryCacheIdempotencyStore(IMemoryCache memoryCache, ILogger<MemoryCacheIdempotencyStore> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<IdempotencyRecord?> GetAsync(string key)
    {
        try
        {
            var record = _memoryCache.Get<IdempotencyRecord>(key);
            _logger.LogDebug("Retrieved idempotency record for key: {Key}, Found: {Found}", key, record != null);
            return Task.FromResult(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving idempotency record for key: {Key}", key);
            return Task.FromResult<IdempotencyRecord?>(null);
        }
    }

    public Task SaveAsync(IdempotencyRecord record, TimeSpan ttl)
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Priority = CacheItemPriority.Normal
            };

            _memoryCache.Set(record.Key, record, options);
            _logger.LogDebug("Saved idempotency record for key: {Key}, TTL: {TTL}", record.Key, ttl);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving idempotency record for key: {Key}", record.Key);
            return Task.CompletedTask;
        }
    }
}
