Product Requirements Document: Idempotency Middleware (Attribute-Based)

1. Introduction

Bu doküman, .NET Core API’lerinde kritik uç noktalarda idempotensi sağlamak için geliştirilecek Attribute-Based Idempotency modülünün gereksinimlerini tanımlar. Amaç, tekrar eden POST isteklerinin duplicate işlemler üretmeden güvenli bir şekilde işlenmesini sağlamaktır.

2. Amaç ve Kapsam
	•	Amaç:  Ödeme, sipariş, fatura oluşturma gibi kritik işlemler için idempotensi garantisi sunmak.
	•	Kapsam:  Sadece IdempotentAttribute eklenen controller aksiyonları etkilenecek; global middleware’e gerek kalmadan esnek TTL ve header yapılandırması sağlanacak.
	•	Çıktı:  Atribut tabanlı, MemoryCache kullanan, konfigüre edilebilir TTL ve header adı destekleyen idempotency modülü.

3. Hedefler & Başarı Kriterleri

Hedef	Metrik/Kriter
Tekrarlanan isteklerde duplicate önleme	Aynı Idempotency-Key ile ikinci istek 0 kayıt üretmeli
Dinamik TTL ve header konfigürasyonu	Attribute parametreleriyle özelleştirilebilmeli
Düşük bağımlılık	Ek cache servisine gerek kalmadan in-memory çalışmalı
Basit entegrasyon	Tek satır attribute ekleyerek uygulanabilmeli

4. Kullanıcı Hikayeleri
	1.	Geliştirici, PaymentsController.CreatePayment aksiyonuna [Idempotent] ekleyerek ödeme endpoint’ini idempotent hale getirebilmek ister.
	2.	Geliştirici, farklı işlem tipi için TTL süresini TtlSeconds=60 ile kısaltmak ister.
	3.	Geliştirici, header adını X-Idemp-Key olarak HeaderName parametresiyle değiştirmek ister.

5. Fonksiyonel Gereksinimler
	1.	IdempotentAttribute
	•	HeaderName (string, opsiyonel): Default Idempotency-Key.
	•	TtlSeconds (int, opsiyonel): Default 300 saniye.
	2.	IIdempotencyStore
	•	GetAsync(string key): Varolan kaydı getirir.
	•	SaveAsync(IdempotencyRecord record, TimeSpan ttl): Yanıtı saklar.
	3.	MemoryCacheIdempotencyStore implemantasyonu
	•	IMemoryCache ile IdempotencyRecord’ları TTL bazlı saklar.
	4.	IdempotencyFilter
	•	Header kontrolü; var ise cache’den kaydı alıp response olarak döner.
	•	Yoksa aksiyon çalıştırılır, sonucu yakalar ve cache’e kaydeder.

6. Teknik Mimari

[Client] --(POST + Idempotency-Key) --> [ASP.NET Core API]
 
ASP.NET Core:
  │
  ├─ [Attribute Filter: IdempotencyFilter]
  │    ├─ IIdempotencyStore (MemoryCacheIdempotencyStore)
  │    └─ IdempotencyRecord Cache
  │
  └─ Controller Actions ([Idempotent] eklenmiş)

1. Veri Modeli

public class IdempotencyRecord
{
    public string Key { get; set; }
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; }
}

8. API/Attribute Kullanım Örneği

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    // Default: TTL=300s, Header=Idempotency-Key
    [HttpPost]
    [Idempotent]
    public IActionResult CreatePayment([FromBody] PaymentDto dto) =>
        Ok(new { success = true, transactionId = Guid.NewGuid() });

    // Custom: TTL=60s, Header=X-Idemp-Key
    [HttpPost("quick")]
    [Idempotent(TtlSeconds = 60, HeaderName = "X-Idemp-Key")]
    public IActionResult QuickPayment([FromBody] PaymentDto dto) => …;
}

9. Non-Functional Gereksinimler
	•	Performans: Cache kontrolleri < 1ms ek yük.
	•	Test Edilebilirlik: Unit/integration test senaryoları olmalı.
	•	Dokümantasyon: README.md'de kullanım ve konfigürasyon örnekleri.


10. Yol Haritası
	1.	Interface ve model geliştirme
	2.	MemoryCache store ve filter implementasyonu
	3.	Unit & Integration testler 
	4.	Dokümantasyon & Örnek projeye entegrasyon
