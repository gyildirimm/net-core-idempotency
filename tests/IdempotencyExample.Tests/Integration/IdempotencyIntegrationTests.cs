using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using IdempotencyExample.Api.Models;

namespace IdempotencyExample.Tests.Integration;

public class IdempotencyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public IdempotencyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayment_WithIdempotencyKey_ShouldReturnSameResponseOnDuplicateRequest()
    {
        // Arrange
        var payment = new PaymentDto
        {
            Amount = 100.50m,
            Currency = "TRY",
            Description = "Test Payment",
            CardToken = "test-token-123"
        };

        var json = JsonSerializer.Serialize(payment);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act - İlk istek
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        request1.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response1 = await _client.SendAsync(request1);
        var responseBody1 = await response1.Content.ReadAsStringAsync();

        // Act - İkinci istek (aynı idempotency key ile)
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/payments");
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        request2.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response2 = await _client.SendAsync(request2);
        var responseBody2 = await response2.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        
        // İki response'un transaction ID'si aynı olmalı (cache'den döndü)
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var payment1 = JsonSerializer.Deserialize<PaymentResponse>(responseBody1, options);
        var payment2 = JsonSerializer.Deserialize<PaymentResponse>(responseBody2, options);
        
        Assert.Equal(payment1?.TransactionId, payment2?.TransactionId);
    }

    [Fact]
    public async Task CreatePayment_WithoutIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var payment = new PaymentDto
        {
            Amount = 50.00m,
            Currency = "TRY"
        };

        var json = JsonSerializer.Serialize(payment);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/payments", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Missing required header: Idempotency-Key", responseBody);
    }

    [Fact]
    public async Task QuickPayment_WithCustomHeader_ShouldWork()
    {
        // Arrange
        var payment = new PaymentDto
        {
            Amount = 25.75m,
            Currency = "USD"
        };

        var json = JsonSerializer.Serialize(payment);
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/quick");
        request.Headers.Add("X-Idemp-Key", idempotencyKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var paymentResponse = JsonSerializer.Deserialize<PaymentResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(paymentResponse);
        Assert.Equal(25.75m, paymentResponse.Amount);
        Assert.Equal("Quick-Processed", paymentResponse.Status);
    }

    [Fact]
    public async Task RegularPayment_WithoutIdempotency_ShouldGenerateDifferentTransactionIds()
    {
        // Arrange
        var payment = new PaymentDto
        {
            Amount = 75.00m,
            Currency = "EUR"
        };

        var json = JsonSerializer.Serialize(payment);
        var content1 = new StringContent(json, Encoding.UTF8, "application/json");
        var content2 = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - İki farklı istek
        var response1 = await _client.PostAsync("/api/payments/regular", content1);
        var response2 = await _client.PostAsync("/api/payments/regular", content2);

        var responseBody1 = await response1.Content.ReadAsStringAsync();
        var responseBody2 = await response2.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var payment1 = JsonSerializer.Deserialize<PaymentResponse>(responseBody1, options);
        var payment2 = JsonSerializer.Deserialize<PaymentResponse>(responseBody2, options);

        // Regular endpoint'te idempotency yok, farklı transaction ID'ler olmalı
        Assert.NotEqual(payment1?.TransactionId, payment2?.TransactionId);
    }
}
