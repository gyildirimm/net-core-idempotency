using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;

namespace IdempotencyExample.Api.Idempotency;

public class IdempotencyFilter : ActionFilterAttribute
{
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyFilter> _logger;

    public IdempotencyFilter(IIdempotencyStore store, ILogger<IdempotencyFilter> logger)
    {
        _store = store;
        _logger = logger;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // IdempotentAttribute var mı kontrol et
        var idempotentAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<IdempotentAttribute>()
            .FirstOrDefault();

        if (idempotentAttribute == null)
        {
            await next();
            return;
        }

        // Idempotency key'i al
        var idempotencyKey = context.HttpContext.Request.Headers[idempotentAttribute.HeaderName].FirstOrDefault();
        
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new 
            { 
                error = $"Missing required header: {idempotentAttribute.HeaderName}" 
            });
            return;
        }

        _logger.LogDebug("Processing idempotency key: {Key}", idempotencyKey);

        // Cache'den varolan kaydı kontrol et
        var existingRecord = await _store.GetAsync(idempotencyKey);
        if (existingRecord != null)
        {
            _logger.LogInformation("Returning cached response for idempotency key: {Key}", idempotencyKey);
            
            context.Result = new ContentResult
            {
                StatusCode = existingRecord.StatusCode,
                Content = existingRecord.ResponseBody,
                ContentType = existingRecord.ContentType
            };
            return;
        }

        // Action'ı çalıştır
        var executedContext = await next();

        // Başarılı response'u cache'le
        if (executedContext.Result is ObjectResult objectResult && objectResult.StatusCode >= 200 && objectResult.StatusCode < 300)
        {
            var responseBody = JsonSerializer.Serialize(objectResult.Value);
            var record = new IdempotencyRecord
            {
                Key = idempotencyKey,
                StatusCode = objectResult.StatusCode ?? 200,
                ResponseBody = responseBody,
                ContentType = "application/json"
            };

            var ttl = TimeSpan.FromSeconds(idempotentAttribute.TtlSeconds);
            await _store.SaveAsync(record, ttl);
            
            _logger.LogInformation("Cached response for idempotency key: {Key}, TTL: {TTL}", idempotencyKey, ttl);
        }
        else if (executedContext.Result is ContentResult contentResult && contentResult.StatusCode >= 200 && contentResult.StatusCode < 300)
        {
            var record = new IdempotencyRecord
            {
                Key = idempotencyKey,
                StatusCode = contentResult.StatusCode ?? 200,
                ResponseBody = contentResult.Content ?? string.Empty,
                ContentType = contentResult.ContentType ?? "application/json"
            };

            var ttl = TimeSpan.FromSeconds(idempotentAttribute.TtlSeconds);
            await _store.SaveAsync(record, ttl);
            
            _logger.LogInformation("Cached response for idempotency key: {Key}, TTL: {TTL}", idempotencyKey, ttl);
        }
    }
}
