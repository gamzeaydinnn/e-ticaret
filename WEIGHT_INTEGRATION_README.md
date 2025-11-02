# Mikro-Ağırlık (Tartı) Entegrasyonu

## 📋 Genel Bakış

Bu entegrasyon, tartı cihazından gelen gerçek ağırlık ile müşterinin sipariş ettiği beklenen ağırlık arasındaki farkı (fazlalık) yönetmek için tasarlanmıştır.

## ✅ Tamamlanan Adımlar

### 1. Veritabanı Yapısı

#### Yeni Entity'ler:

- **WeightReport**: Tartı raporları için ana entity
  - ExternalReportId (idempotency için unique)
  - ExpectedWeightGrams, ReportedWeightGrams, OverageGrams
  - OverageAmount (parasal değer)
  - Status (Pending, Approved, Rejected, Charged, Failed, AutoApproved)
  - Metadata (JSON), AdminNote, CourierNote

#### Entity Güncellemeleri:

- **OrderItem**: `ExpectedWeightGrams` eklendi
- **Product**: `UnitWeightGrams` eklendi
- **Order**: WeightReports navigation property eklendi

### 2. Repository Katmanı

- **IWeightReportRepository** interface oluşturuldu
- **WeightReportRepository** implementation tamamlandı
- Özellikler:
  - Idempotency kontrolü (GetByExternalReportIdAsync)
  - Sayfalı listeleme (GetByStatusAsync)
  - İstatistikler (GetStatsAsync)
  - Unique index (ExternalReportId)

### 3. Business Katmanı

#### IWeightService Interface:

- ProcessReportAsync - Rapor işleme
- ApproveReportAsync - Yönetici onayı
- RejectReportAsync - Yönetici reddi
- ChargeOverageAsync - Ödeme tahsilatı

#### WeightService Implementation:

- Otomatik onay mekanizması (eşik değer kontrolü)
- Gram başına fiyat hesaplama
- Fazla tutar hesaplama
- Mock ödeme entegrasyonu (gerçek API bekliyor)

### 4. API Endpoints

#### Webhook Endpoint (MicroController):

```http
POST /api/micro/weight
Headers:
  - X-Micro-Signature: HMAC-SHA256 imzası
  - Content-Type: application/json

Body:
{
  "reportId": "unique-id",
  "orderId": 123,
  "orderItemId": 456,  // Opsiyonel
  "reportedWeightGrams": 1100,
  "timestamp": "2025-11-02T10:30:00Z",
  "source": "scale-device-01",
  "metadata": "{...}"
}
```

**Güvenlik:**

- HMAC-SHA256 imza doğrulama
- Timestamp doğrulama (replay attack önleme)
- Idempotency kontrolü

#### Admin Endpoints (WeightReportsController):

```http
GET    /api/admin/weightreports?status=Pending&page=1&pageSize=20
GET    /api/admin/weightreports/{id}
POST   /api/admin/weightreports/{id}/approve
POST   /api/admin/weightreports/{id}/reject
GET    /api/admin/weightreports/stats
```

### 5. Sipariş İşleme

**OrderManager güncellemeleri:**

- CreateAsync: ExpectedWeightGrams hesaplama eklendi
- CheckoutAsync: ExpectedWeightGrams hesaplama eklendi
- Hesaplama: `Product.UnitWeightGrams × Quantity`

### 6. Configuration

**appsettings.Development.json:**

```json
{
  "Micro": {
    "SharedSecret": "dev-secret-change-in-production",
    "AutoApproveThresholdGrams": 50
  }
}
```

## 🔄 İş Akışı

### Normal Akış:

1. **Sipariş oluşturulur**
   - Her OrderItem için ExpectedWeightGrams hesaplanır
2. **Paketleme sırasında tartı cihazı rapor gönderir**
   ```
   POST /api/micro/weight
   ```
3. **WeightService raporu işler**
   - Beklenen ağırlığı hesaplar
   - Fazlalığı (overage) hesaplar
   - Fazla tutarı hesaplar
   - Eşik kontrolü yapar (≤50gr → otomatik onay)
4. **Yönetici onayı (>50gr ise)**
   - Bekleyen raporlar listesi
   - Detay görüntüleme
   - Onayla/Reddet
5. **Ödeme tahsilatı**
   - Onaylandığında ChargeOverageAsync çalışır
   - Mock implementation (gerçek API bekleniyor)

## 📊 Veritabanı Migration

```bash
# Migration oluştur
dotnet ef migrations add AddWeightReportEntities --project src/ECommerce.Data --startup-project src/ECommerce.API

# Veritabanını güncelle
dotnet ef database update --project src/ECommerce.Data --startup-project src/ECommerce.API
```

## 🧪 Test Senaryoları

### 1. Webhook Test:

```bash
curl -X POST http://localhost:5000/api/micro/weight \
  -H "Content-Type: application/json" \
  -H "X-Micro-Signature: <HMAC-SHA256-signature>" \
  -d '{
    "reportId": "test-001",
    "orderId": 1,
    "reportedWeightGrams": 1100,
    "timestamp": "2025-11-02T10:30:00Z",
    "source": "test-device"
  }'
```

### 2. Yönetici Test:

```bash
# Bekleyen raporlar
GET /api/admin/weightreports?status=Pending

# Rapor onaylama
POST /api/admin/weightreports/1/approve
{
  "reportId": 1,
  "note": "Onaylandı"
}
```

## 🚀 Gelecek Geliştirmeler (API geldiğinde)

### Faz 2:

- [ ] Gerçek ödeme servisi entegrasyonu
  - Stripe/Iyzico off-session ödeme
  - SCA (Strong Customer Authentication) yönetimi
  - Başarısız ödeme retry mekanizması
- [ ] Kurye akışı
  - Kurye teslimat onayı
  - Mobil uygulama endpoint'leri
- [ ] Bildirim sistemi
  - Yöneticiye push notification
  - Müşteriye e-posta/SMS
- [ ] Background job sistemi
  - Hangfire/Azure Queue entegrasyonu
  - Asenkron işlem kuyruğu

### Faz 3:

- [ ] Raporlama ve analitik
- [ ] İade/iade işlemleri
- [ ] Mutabakat raporu
- [ ] Audit log detaylandırma

## 🔒 Güvenlik Notları

1. **Production'da MUTLAKA:**
   - `Micro:SharedSecret` güvenli bir değere değiştirilmeli
   - HTTPS kullanılmalı
   - Rate limiting eklenmeli
2. **Ödeme güvenliği:**
   - PCI-DSS uyumlu ödeme gateway kullanılmalı
   - Kart bilgileri asla saklanmamalı
   - Token-based ödeme yöntemleri tercih edilmeli

## 📝 Değişiklik Listesi

### Backend Değişiklikleri:

```
src/ECommerce.Entities/Concrete/
  ├── WeightReport.cs (YENİ)
  ├── OrderItem.cs (ExpectedWeightGrams eklendi)
  ├── Product.cs (UnitWeightGrams eklendi)
  └── Order.cs (WeightReports navigation eklendi)

src/ECommerce.Core/
  ├── DTOs/Weight/MicroWeightReportDto.cs (YENİ)
  ├── Interfaces/IWeightReportRepository.cs (YENİ)
  └── Interfaces/IWeightService.cs (YENİ)

src/ECommerce.Data/
  ├── Context/ECommerceDbContext.cs (DbSet ve Configuration eklendi)
  └── Repositories/WeightReportRepository.cs (YENİ)

src/ECommerce.Business/Services/Managers/
  ├── WeightService.cs (YENİ)
  └── OrderManager.cs (ExpectedWeightGrams hesaplama eklendi)

src/ECommerce.API/
  ├── Controllers/MicroController.cs (webhook endpoint eklendi)
  ├── Controllers/Admin/WeightReportsController.cs (YENİ)
  ├── Program.cs (DI registrations eklendi)
  └── appsettings.Development.json (Micro config eklendi)
```

## 💡 Kullanım Örnekleri

### C# - HMAC İmza Oluşturma:

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var dto = new MicroWeightReportDto { /* ... */ };
var payload = JsonSerializer.Serialize(dto);
var secret = "dev-secret-change-in-production";

using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
var signature = Convert.ToBase64String(hashBytes);

// HTTP Header: X-Micro-Signature: {signature}
```

## 🎯 Sonraki Adımlar

1. ✅ Migration çalıştır
2. ✅ Test verileri ekle (Product.UnitWeightGrams)
3. ⏳ Frontend admin paneli (React - WeightReports.jsx)
4. ⏳ Gerçek API entegrasyonu bekle
5. ⏳ Production deployment

## 📞 Destek

Sorular için: Developer Team
Dokümantasyon: Bu README
