# Idempotency Example

Bu proje, .NET Core Web API'lerinde **attribute-based idempotency** implementasyonunu göstermektedir. Kritik API endpoint'lerinde duplicate işlemleri önlemek için geliştirilmiştir.

## 🎯 Amaç

Ödeme, sipariş, fatura oluşturma gibi kritik işlemlerde aynı isteğin birden fazla kez gönderilmesi durumunda, sadece ilk isteğin işlenmesini ve sonraki isteklerde cache'lenmiş yanıtın döndürülmesini sağlar.

## 🚀 Özellikler

- ✅ **Attribute-based yaklaşım**: Sadece `[Idempotent]` attribute'u ekleyerek kullanım
- ✅ **Konfigüre edilebilir TTL**: Farklı endpoint'ler için farklı cache süreleri
- ✅ **Özelleştirilebilir header**: Default `Idempotency-Key` yerine istediğiniz header adını kullanabilme
- ✅ **Memory Cache**: Ek dependency olmadan in-memory çalışma
## 📋 Kullanım

### Temel Kullanım

```csharp
[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(IdempotencyFilter))]
public class PaymentsController : ControllerBase
{
    // Default: TTL=300s, Header=Idempotency-Key
    [HttpPost]
    [Idempotent]
    public IActionResult CreatePayment([FromBody] PaymentDto dto)
    {
        return Ok(new PaymentResponse 
        { 
            TransactionId = Guid.NewGuid().ToString(),
            Amount = dto.Amount 
        });
    }
}
```

### Özelleştirilmiş Kullanım

```csharp
// Custom TTL ve header name
[HttpPost("quick")]
[Idempotent(TtlSeconds = 60, HeaderName = "X-Idemp-Key")]
public IActionResult QuickPayment([FromBody] PaymentDto dto)
{
    // Implementation...
}

// Uzun süreli cache
[HttpPost("bulk")]
[Idempotent(TtlSeconds = 600)]
public IActionResult BulkPayment([FromBody] PaymentDto[] payments)
{
    // Implementation...
}
```

## 🏗️ Mimari

```
[Client] --(POST + Idempotency-Key)--> [ASP.NET Core API]
 
ASP.NET Core:
  │
  ├─ [Attribute Filter: IdempotencyFilter]
  │    ├─ IIdempotencyStore (MemoryCacheIdempotencyStore)
  │    └─ IdempotencyRecord Cache
  │
  └─ Controller Actions ([Idempotent] eklenmiş)
```

### Temel Bileşenler

1. **IdempotentAttribute**: Method-level attribute
2. **IdempotencyFilter**: ActionFilter implementasyonu
3. **IIdempotencyStore**: Cache abstraction
4. **MemoryCacheIdempotencyStore**: Memory cache implementasyonu
5. **IdempotencyRecord**: Cache'de saklanan veri modeli

## 🔧 Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8.0+

### Adımlar

1. **Projeyi klonlayın**
   ```bash
   git clone <repository-url>
   cd idempotency-example
   ```

2. **Bağımlılıkları yükleyin**
   ```bash
   dotnet restore
   ```

3. **Projeyi çalıştırın**
   ```bash
   cd src/IdempotencyExample.Api
   dotnet run
   ```

4. **Swagger UI'ye erişin**
   ```
   https://localhost:7000/swagger
   ```

## 🧪 Test Etme

### Unit Testler
```bash
dotnet test tests/IdempotencyExample.Tests/
```

### Manuel Test

**1. İlk İstek:**
```bash
curl -X POST "https://localhost:7000/api/payments" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-123" \
  -d '{
    "amount": 100.50,
    "currency": "TRY",
    "description": "Test Payment"
  }'
```

**2. Aynı Key ile Tekrar İstek:**
```bash
# Aynı Idempotency-Key ile tekrar gönderdiğinizde,
# aynı TransactionId ile cache'den yanıt dönecek
curl -X POST "https://localhost:7000/api/payments" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-123" \
  -d '{
    "amount": 100.50,
    "currency": "TRY",
    "description": "Test Payment"
  }'
```

**3. Custom Header ile Test:**
```bash
curl -X POST "https://localhost:7000/api/payments/quick" \
  -H "Content-Type: application/json" \
  -H "X-Idemp-Key: quick-test-456" \
  -d '{
    "amount": 50.25,
    "currency": "USD"
  }'
```

## 📊 API Endpoints

| Endpoint | Method | Header | TTL | Açıklama |
|----------|--------|--------|-----|----------|
| `/api/payments` | POST | `Idempotency-Key` | 300s | Standart ödeme |
| `/api/payments/quick` | POST | `X-Idemp-Key` | 60s | Hızlı ödeme |
| `/api/payments/bulk` | POST | `Idempotency-Key` | 600s | Toplu ödeme |
| `/api/payments/regular` | POST | - | - | İdempotency yok |

## 🔍 Monitoring ve Debugging

Proje, detaylı logging sağlar:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "IdempotencyExample.Api.Idempotency": "Debug"
    }
  }
}
```

Log mesajları:
- `Retrieved idempotency record for key: {Key}`
- `Saved idempotency record for key: {Key}, TTL: {TTL}`
- `Returning cached response for idempotency key: {Key}`

## 🔒 Güvenlik Notları

- Idempotency key'ler güçlü ve tahmin edilemez olmalıdır (UUID önerilir)
- Cache'de hassas bilgiler saklanmamalıdır
- TTL süreleri işlem tipine uygun ayarlanmalıdır
- Production'da distributed cache (Redis) kullanımı önerilir

## 📝 Yapı

```
src/
├── IdempotencyExample.Api/
│   ├── Controllers/
│   │   └── PaymentsController.cs
│   ├── Idempotency/
│   │   ├── IdempotentAttribute.cs
│   │   ├── IdempotencyFilter.cs
│   │   ├── IdempotencyRecord.cs
│   │   ├── IIdempotencyStore.cs
│   │   └── MemoryCacheIdempotencyStore.cs
│   ├── Models/
│   │   ├── PaymentDto.cs
│   │   └── PaymentResponse.cs
│   └── Program.cs
└── tests/
    └── IdempotencyExample.Tests/
        ├── Integration/
        │   └── IdempotencyIntegrationTests.cs
        └── Unit/
            └── MemoryCacheIdempotencyStoreTests.cs
```

## 💡 İpuçları

- Idempotency key'ler client tarafında üretilmelidir
- Cache boyutu ve TTL değerleri sistem kapasitesine göre ayarlanmalıdır
- Error response'ları cache'lenmez, sadece başarılı response'lar cache'lenir
- Aynı endpoint için farklı payload'lar aynı idempotency key ile gönderilirse ilk payload işlenir
