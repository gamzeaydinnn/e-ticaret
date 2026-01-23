# 🏋️ Ağırlık Farkına Göre Ödeme Sistemi

## 📋 Genel Bakış

Bu sistem, kg/gram bazlı satılan ürünler için kurye tesliminde gerçek tartı ile tahmini ağırlık arasındaki farka göre ek ödeme veya iade işlemlerini otomatik olarak yönetir.

## 🔄 İş Akışı

```
[Sipariş] → [Tahmini Ağırlık] → [Kurye Tartımı] → [Fark Hesaplama] → [Admin Onay?] → [Ödeme İşlemi]
```

### 1. Sipariş Oluşturma

- Müşteri kg/gram bazlı ürün siparişi verir
- Sistem tahmini ağırlık üzerinden fiyat hesaplar
- %20 marjlı ön provizyon (pre-auth) alınır

### 2. Kurye Tartım İşlemi

- Kurye teslimat sırasında ürünü tartar
- Gerçek ağırlık sisteme girilir
- Otomatik fark hesaplaması yapılır

### 3. Fark Değerlendirme

- **%20'nin altı veya 50 TL altı**: Otomatik işlem
- **%20'nin üzeri veya 50 TL üzeri**: Admin onayı gerekir

### 4. Ödeme İşlemi

- **Fazla geldiyse**: Müşteriden ek ödeme
- **Eksik geldiyse**: Müşteriye iade

## 📊 Durum Akışı (Status Flow)

```
PendingWeighing (Tartım Bekliyor)
    ↓
Weighed (Tartıldı)
    ↓
┌───────────────────────────────────────────┐
│ Fark > %20 veya > 50 TL?                  │
├───────────────────────────────────────────┤
│ HAYIR                      │ EVET         │
│   ↓                        │   ↓          │
│ NoDifference               │ PendingAdmin │
│ PendingAdditionalPayment   │   Approval   │
│ PendingRefund              │   ↓          │
│   ↓                        │ ┌────────┐   │
│   ↓                        │ │Onayla? │   │
│   ↓                        │ └────────┘   │
│   ↓                        │ EVET │ HAYIR │
│   ↓                        │   ↓  │   ↓   │
│   ↓                        │ Eski │ Reject│
│   ↓                        │Durum │ ByAdm │
└───────────────────────────────────────────┘
    ↓
Completed / Failed
```

## 🛠️ API Endpoints

### Kurye İşlemleri

| Method | Endpoint                                      | Açıklama               |
| ------ | --------------------------------------------- | ---------------------- |
| POST   | `/api/courier/weight-report`                  | Yeni tartım bildirimi  |
| GET    | `/api/courier/pending-weights`                | Bekleyen tartımlar     |
| GET    | `/api/courier/orders/{orderId}/weight-status` | Sipariş ağırlık durumu |

### Admin İşlemleri

| Method | Endpoint                                     | Açıklama                 |
| ------ | -------------------------------------------- | ------------------------ |
| GET    | `/api/admin/weight-adjustments`              | Tüm ağırlık ayarlamaları |
| GET    | `/api/admin/weight-adjustments/pending`      | Onay bekleyenler         |
| POST   | `/api/admin/weight-adjustments/{id}/approve` | Onayla                   |
| POST   | `/api/admin/weight-adjustments/{id}/reject`  | Reddet                   |

### Müşteri İşlemleri

| Method | Endpoint                                    | Açıklama            |
| ------ | ------------------------------------------- | ------------------- |
| GET    | `/api/customer/weight-adjustments`          | Benim ayarlamalarım |
| POST   | `/api/customer/weight-adjustments/{id}/pay` | Ek ödeme yap        |

## 📱 Frontend Bileşenleri

### Kurye Paneli (`/kurye/agirlik-raporu`)

- Tartım giriş formu
- Bekleyen tartımlar listesi
- Fark önizleme

### Admin Paneli (`/admin/agirlik-yonetimi`)

- Onay bekleyen işlemler
- İstatistik kartları
- Detay modalı

### Müşteri Sayfası

- Sipariş detayında ağırlık farkı bilgisi
- Ek ödeme butonu (gerekirse)

## 🔧 Konfigürasyon

`appsettings.json`:

```json
{
  "WeightAdjustment": {
    "AutoApproveThresholdPercent": 20.0,
    "AutoApproveThresholdAmount": 50.0,
    "PreAuthMarginPercent": 20.0,
    "PreAuthExpiryHours": 48
  }
}
```

## 💾 Veritabanı Yapısı

### WeightAdjustments Tablosu

```sql
CREATE TABLE WeightAdjustments (
    Id INT PRIMARY KEY IDENTITY,
    OrderId INT NOT NULL,
    OrderItemId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(255),

    -- Ağırlık Bilgileri
    WeightUnit INT DEFAULT 1,
    EstimatedWeight DECIMAL(18,4) NOT NULL,
    ActualWeight DECIMAL(18,4),
    WeightDifference DECIMAL(18,4),
    DifferencePercent DECIMAL(18,4),

    -- Fiyat Bilgileri
    PricePerUnit DECIMAL(18,2) NOT NULL,
    EstimatedPrice DECIMAL(18,2) NOT NULL,
    ActualPrice DECIMAL(18,2),
    PriceDifference DECIMAL(18,2),

    -- Durum
    Status INT NOT NULL DEFAULT 1,

    -- Kurye Bilgileri
    CourierId NVARCHAR(450),
    CourierName NVARCHAR(256),
    WeighedAt DATETIME2,

    -- Admin Bilgileri
    AdminId NVARCHAR(450),
    AdminName NVARCHAR(256),
    AdminComment NVARCHAR(1000),
    AdminActionAt DATETIME2,

    -- Ödeme Bilgileri
    PaymentStatus INT DEFAULT 0,
    PaymentTransactionId NVARCHAR(256),
    PaymentCompletedAt DATETIME2,

    -- Audit
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    IsActive BIT DEFAULT 1,

    FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    FOREIGN KEY (OrderItemId) REFERENCES OrderItems(Id)
);
```

## 🧪 Test Senaryoları

### Senaryo 1: Normal Fark (Otomatik)

1. Sipariş: 2 kg elma = 50 TL
2. Gerçek: 2.1 kg elma = 52.50 TL
3. Fark: %5, +2.50 TL → Otomatik ek ödeme

### Senaryo 2: Büyük Fark (Admin Onay)

1. Sipariş: 1 kg bal = 200 TL
2. Gerçek: 1.5 kg bal = 300 TL
3. Fark: %50, +100 TL → Admin onayı gerekli

### Senaryo 3: Eksik Geldiyse

1. Sipariş: 3 kg portakal = 45 TL
2. Gerçek: 2.8 kg portakal = 42 TL
3. Fark: %-7, -3 TL → Otomatik iade

## 📊 Raporlama

Admin panelinde mevcut istatistikler:

- Toplam ayarlama sayısı
- Bekleyen onay sayısı
- Günlük/haftalık/aylık fark toplamları
- Ortalama fark yüzdesi

## 🔒 Güvenlik

- JWT token doğrulama tüm endpoint'lerde zorunlu
- Kurye endpoint'leri `Courier` rolü gerektirir
- Admin endpoint'leri `Admin` rolü gerektirir
- CORS politikaları uygulanır

## 🚀 Deployment Notları

1. Migration çalıştır: `dotnet ef database update`
2. Seed data'yı kontrol et
3. Ön provizyon süresi konfigüre et
4. Test siparişi ver ve akışı doğrula

## 📝 Versiyon Geçmişi

| Versiyon | Tarih     | Değişiklikler |
| -------- | --------- | ------------- |
| 1.0.0    | Ocak 2026 | İlk sürüm     |

---

**Sorular için:** [Proje Dokümantasyonu](./README.md)
