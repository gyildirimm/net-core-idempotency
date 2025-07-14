using Microsoft.AspNetCore.Mvc;
using IdempotencyExample.Api.Idempotency;
using IdempotencyExample.Api.Models;

namespace IdempotencyExample.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(IdempotencyFilter))]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(ILogger<PaymentsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new payment with default idempotency settings (TTL=300s, Header=Idempotency-Key)
    /// </summary>
    [HttpPost]
    [Idempotent]
    public IActionResult CreatePayment([FromBody] PaymentDto dto)
    {
        _logger.LogInformation("Processing payment for amount: {Amount} {Currency}", dto.Amount, dto.Currency);

        // Simulated payment processing
        var response = new PaymentResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = dto.Amount,
            Currency = dto.Currency
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a quick payment with custom idempotency settings (TTL=60s, Header=X-Idemp-Key)
    /// </summary>
    [HttpPost("quick")]
    [Idempotent(TtlSeconds = 60, HeaderName = "X-Idemp-Key")]
    public IActionResult QuickPayment([FromBody] PaymentDto dto)
    {
        _logger.LogInformation("Processing quick payment for amount: {Amount} {Currency}", dto.Amount, dto.Currency);

        var response = new PaymentResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = dto.Amount,
            Currency = dto.Currency,
            Status = "Quick-Processed"
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a bulk payment with extended TTL (TTL=600s)
    /// </summary>
    [HttpPost("bulk")]
    [Idempotent(TtlSeconds = 600)]
    public IActionResult BulkPayment([FromBody] PaymentDto[] payments)
    {
        _logger.LogInformation("Processing bulk payment with {Count} items", payments.Length);

        var responses = payments.Select(p => new PaymentResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = p.Amount,
            Currency = p.Currency,
            Status = "Bulk-Processed"
        }).ToArray();

        return Ok(new { Success = true, Payments = responses, TotalAmount = payments.Sum(p => p.Amount) });
    }

    /// <summary>
    /// Regular endpoint without idempotency (for comparison)
    /// </summary>
    [HttpPost("regular")]
    public IActionResult RegularPayment([FromBody] PaymentDto dto)
    {
        _logger.LogInformation("Processing regular payment (no idempotency) for amount: {Amount} {Currency}", dto.Amount, dto.Currency);

        var response = new PaymentResponse
        {
            Success = true,
            TransactionId = Guid.NewGuid().ToString(),
            Amount = dto.Amount,
            Currency = dto.Currency,
            Status = "Regular-Processed"
        };

        return Ok(response);
    }
}
