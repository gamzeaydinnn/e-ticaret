# 📋 Sipariş Durum Akışı - Düzeltilmiş State Machine

> Bu doküman kod ve doküman arasındaki tutarsızlıkların giderilmesini açıklar.

---

## 🔄 Düzeltilmiş Tutarlı Akış

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DOĞRU SİPARİŞ AKIŞI                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  [MÜŞTERİ]          [ADMIN]         [STORE]        [DISPATCHER]   [KURYE]   │
│     │                  │               │                │            │       │
│  Sipariş              Onay         Hazırlama      Kurye Atama    Teslimat   │
│  Oluştur              Ver          Başla           Yap           Başla      │
│     │                  │               │                │            │       │
│     ▼                  ▼               ▼                ▼            ▼       │
│  PENDING ──────► CONFIRMED ──────► PREPARING ──────► READY ──────► ASSIGNED │
│     │                  │               │                │            │       │
│     │                  │               │                │            ▼       │
│     │                  │               │                │      OUT_FOR_      │
│     │                  │               │                │      DELIVERY      │
│     │                  │               │                │            │       │
│     │                  │               │                │            ▼       │
│     │                  │               │                │       DELIVERED    │
│     │                  │               │                │                    │
│     └──────────────────┴───────────────┴────────────────┴─► CANCELLED       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## ✅ Düzeltilen Sorunlar

### 1. **Assigned → OutForDelivery Geçişi Eksikti**

**Sorun:** State machine'de `Assigned` durumundan `OutForDelivery`'e geçiş tanımlı değildi.

**Çözüm:**

```csharp
[OrderStatus.Assigned] = new HashSet<OrderStatus>
{
    OrderStatus.PickedUp,          // Kurye teslim aldı
    OrderStatus.OutForDelivery,    // Kurye yola çıktı ✅ EKLENDİ
    OrderStatus.Shipped,           // Eski uyumluluk
    OrderStatus.Ready,             // Kurye iptal etti
    OrderStatus.DeliveryFailed,
    OrderStatus.Cancelled
};
```

### 2. **Confirmed → Preparing Geçişi Yoktu**

**Sorun:** State machine sadece `Confirmed → Processing` geçişi tanımlıyordu.

**Çözüm:**

```csharp
[OrderStatus.Confirmed] = new HashSet<OrderStatus>
{
    OrderStatus.Preparing,     // Store Attendant hazırlamaya başladı ✅ EKLENDİ
    OrderStatus.Processing,    // Eski uyumluluk için
    OrderStatus.Cancelled,
    OrderStatus.Refunded
};
```

### 3. **Ready → Assigned Geçişi Yoktu**

**Sorun:** `Ready` durumu tanımlı değildi, sadece eski `ReadyForPickup` kullanılıyordu.

**Çözüm:**

```csharp
[OrderStatus.Ready] = new HashSet<OrderStatus>
{
    OrderStatus.Assigned,         // Dispatcher kurye atadı ✅ EKLENDİ
    OrderStatus.Preparing,        // Geri alındı (sorun)
    OrderStatus.Cancelled,
    OrderStatus.Refunded
};
```

### 4. **CourierAssignableStates Güncellendi**

**Eski:**

```csharp
CourierAssignableStates = { Confirmed, Processing, ReadyForPickup, DeliveryFailed }
```

**Yeni:**

```csharp
CourierAssignableStates = { Ready, ReadyForPickup, DeliveryFailed }
```

---

## 📊 Durum Geçiş Matrisi

| Kaynak Durum       | Hedef Durumlar                                                          |
| ------------------ | ----------------------------------------------------------------------- |
| **Pending**        | Confirmed, Cancelled, New                                               |
| **Confirmed**      | Preparing, Processing, Cancelled, Refunded                              |
| **Preparing**      | Ready, ReadyForPickup, Cancelled, Refunded                              |
| **Ready**          | Assigned, Preparing, Cancelled, Refunded                                |
| **Assigned**       | **OutForDelivery**, PickedUp, Shipped, Ready, DeliveryFailed, Cancelled |
| **OutForDelivery** | Delivered, DeliveryFailed, DeliveryPaymentPending                       |
| **Delivered**      | Refunded, PartialRefund                                                 |
| **DeliveryFailed** | Ready, Assigned, Refunded, Cancelled                                    |

---

## 🔧 Geriye Uyumluluk

Eski durumlar (Processing, ReadyForPickup, Shipped) hala destekleniyor:

| Eski Durum     | Yeni Karşılığı |
| -------------- | -------------- |
| Processing     | Preparing      |
| ReadyForPickup | Ready          |
| Shipped        | OutForDelivery |

---

## 🧪 Test Senaryoları

### Senaryo 1: Tam Akış Testi

```
1. Sipariş oluştur → Pending
2. Admin onayla → Confirmed
3. Store Attendant "Hazırlamaya Başla" → Preparing
4. Store Attendant "Hazır" → Ready
5. Dispatcher kurye ata → Assigned
6. Kurye "Yola Çıktım" → OutForDelivery ✅ (ESKİDEN HATA VERİYORDU)
7. Kurye "Teslim Ettim" → Delivered
```

### Senaryo 2: Teslimat Başarısız + Yeniden Atama

```
1. ... → OutForDelivery
2. Kurye "Teslimat Başarısız" → DeliveryFailed
3. Dispatcher yeniden kurye ata → Assigned (veya Ready'e geri al)
4. Kurye "Yola Çıktım" → OutForDelivery
5. Kurye "Teslim Ettim" → Delivered
```

---

## 📅 Güncelleme Tarihi

- **Tarih:** 26 Ocak 2026
- **Düzeltme:** State machine tutarsızlıkları giderildi
- **Dosyalar:**
  - `OrderStateMachine.cs` - Geçiş matrisi güncellendi
