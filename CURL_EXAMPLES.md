# Idempotency Test Komutları

Bu dosya, API'nin idempotency özelliklerini test etmek için örnek CURL komutları içerir.

## 1. Normal Payment (İdempotency var)

### İlk İstek
```bash
curl -X POST "http://localhost:5000/api/payments" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-123" \
  -d '{
    "amount": 100.50,
    "currency": "TRY",
    "description": "Test Payment",
    "cardToken": "test-token-123"
  }'
```

### Aynı Key ile İkinci İstek (Cache'den dönecek)
```bash
curl -X POST "http://localhost:5000/api/payments" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-123" \
  -d '{
    "amount": 100.50,
    "currency": "TRY",
    "description": "Test Payment",
    "cardToken": "test-token-123"
  }'
```

**Beklenen Sonuç**: İki istek de aynı `transactionId` döner.

## 2. Quick Payment (Custom Header)

```bash
curl -X POST "http://localhost:5000/api/payments/quick" \
  -H "Content-Type: application/json" \
  -H "X-Idemp-Key: quick-test-456" \
  -d '{
    "amount": 50.25,
    "currency": "USD",
    "description": "Quick Payment Test"
  }'
```

**Özellik**: 60 saniye TTL, custom header adı.

## 3. Bulk Payment (Uzun TTL)

```bash
curl -X POST "http://localhost:5000/api/payments/bulk" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: bulk-test-789" \
  -d '[
    {
      "amount": 25.00,
      "currency": "EUR",
      "description": "Bulk Payment 1"
    },
    {
      "amount": 35.00,
      "currency": "EUR", 
      "description": "Bulk Payment 2"
    }
  ]'
```

**Özellik**: 600 saniye (10 dakika) TTL.

## 4. Regular Payment (İdempotency yok)

```bash
curl -X POST "http://localhost:5000/api/payments/regular" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 33.33,
    "currency": "GBP",
    "description": "Regular Payment"
  }'
```

**Not**: Bu endpoint'e aynı isteği tekrar gönderirseniz farklı `transactionId` alırsınız.

## 5. Hata Durumları

### İdempotency Key Eksik
```bash
curl -X POST "http://localhost:5000/api/payments" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 75.00,
    "currency": "EUR"
  }'
```

**Beklenen Sonuç**: `400 Bad Request` - "Missing required header: Idempotency-Key"

### Yanlış Header Adı
```bash
curl -X POST "http://localhost:5000/api/payments/quick" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: wrong-header" \
  -d '{
    "amount": 25.00,
    "currency": "USD"
  }'
```

**Beklenen Sonuç**: `400 Bad Request` - "Missing required header: X-Idemp-Key"

## 6. TTL Test (Manuel)

1. Bir istek gönderin:
```bash
curl -X POST "http://localhost:5000/api/payments/quick" \
  -H "Content-Type: application/json" \
  -H "X-Idemp-Key: ttl-test-123" \
  -d '{"amount": 10.00, "currency": "TRY"}'
```

2. 60 saniye bekleyin

3. Aynı isteği tekrar gönderin:
```bash
curl -X POST "http://localhost:5000/api/payments/quick" \
  -H "Content-Type: application/json" \
  -H "X-Idemp-Key: ttl-test-123" \
  -d '{"amount": 10.00, "currency": "TRY"}'
```

**Beklenen Sonuç**: İkinci istek yeni bir `transactionId` döner (cache expired).

## 7. Swagger UI Test

API'yi browserdan test etmek için:
```
http://localhost:5000/swagger
```

Header'ları manuel olarak ekleyebilir ve test edebilirsiniz.

## 8. Loglama

Development ortamında detaylı logları görmek için:
```bash
cd src/IdempotencyExample.Api
dotnet run --verbosity diagnostic
```

Bu, idempotency filter'ın debug loglarını görmenizi sağlar.
