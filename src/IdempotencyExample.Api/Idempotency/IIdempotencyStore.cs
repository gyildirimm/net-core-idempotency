namespace IdempotencyExample.Api.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key);
    Task SaveAsync(IdempotencyRecord record, TimeSpan ttl);
}
