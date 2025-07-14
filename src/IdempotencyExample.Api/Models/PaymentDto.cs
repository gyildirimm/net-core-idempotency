namespace IdempotencyExample.Api.Models;

public class PaymentDto
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Description { get; set; } = string.Empty;
    public string CardToken { get; set; } = string.Empty;
}
