namespace IdempotencyExample.Api.Idempotency;

[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute
{
    public string HeaderName { get; set; } = "Idempotency-Key";
    public int TtlSeconds { get; set; } = 300; // 5 dakika default
}
