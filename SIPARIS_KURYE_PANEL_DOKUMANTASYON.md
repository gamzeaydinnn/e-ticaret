# 📚 Sipariş-Kurye-Panel Sistemi - Kapsamlı Dokümantasyon

> 10 FAZA'lık geliştirme sürecinin tamamlanmış hali.

---

## 📋 İçindekiler

1. [Sistem Genel Bakış](#sistem-genel-bakış)
2. [Mimari Yapı](#mimari-yapı)
3. [Roller ve Yetkiler](#roller-ve-yetkiler)
4. [Panel Açıklamaları](#panel-açıklamaları)
5. [API Referans](#api-referans)
6. [SignalR Olayları](#signalr-olayları)
7. [Veritabanı Şeması](#veritabanı-şeması)
8. [Kurulum Rehberi](#kurulum-rehberi)

---

## 🔍 Sistem Genel Bakış

### Amaç

E-ticaret platformunda sipariş yaşam döngüsünü yönetmek için tasarlanmış çok katmanlı bir panel sistemi.

### Sipariş Akışı

```
[Müşteri Siparişi]
       ↓
[Sipariş Oluşturuldu - Pending]
       ↓
[Admin/Sistem Onayı - Confirmed]
       ↓
[Store Attendant: Hazırlamaya Başla - Preparing]
       ↓
[Store Attendant: Hazır - Ready]
       ↓
[Dispatcher: Kurye Ata - Assigned]
       ↓
[Kurye: Teslimata Çıktı - OutForDelivery]
       ↓
[Kurye: Teslim Edildi - Delivered]
```

---

## 🏗️ Mimari Yapı

### Teknoloji Stack

| Katman        | Teknoloji                  |
| ------------- | -------------------------- |
| **Backend**   | .NET 9, ASP.NET Core       |
| **Frontend**  | React 18, Bootstrap 5      |
| **Database**  | SQL Server                 |
| **Real-time** | SignalR                    |
| **Container** | Docker, docker-compose     |
| **Auth**      | ASP.NET Core Identity, JWT |

### Proje Yapısı

```
eticaret/
├── src/
│   ├── ECommerce.API/           # Web API
│   │   ├── Controllers/
│   │   │   ├── StoreAttendantOrderController.cs
│   │   │   ├── DispatcherOrderController.cs
│   │   │   └── ...
│   │   ├── Hubs/
│   │   │   ├── StoreAttendantHub.cs
│   │   │   ├── DispatcherHub.cs
│   │   │   └── CourierHub.cs
│   │   └── Infrastructure/
│   │       └── IdentitySeeder.cs
│   ├── ECommerce.Business/      # İş Mantığı
│   ├── ECommerce.Core/          # DTOs, Constants
│   ├── ECommerce.Data/          # Entity Framework
│   └── ECommerce.Entities/      # Domain Entities
├── frontend/
│   ├── src/
│   │   ├── pages/
│   │   │   ├── StoreAttendant/
│   │   │   │   ├── StoreAttendantLogin.jsx
│   │   │   │   └── StoreAttendantDashboard.jsx
│   │   │   └── Dispatcher/
│   │   │       ├── DispatcherLogin.jsx
│   │   │       └── DispatcherDashboard.jsx
│   │   ├── services/
│   │   │   ├── storeAttendantService.js
│   │   │   └── dispatcherService.js
│   │   ├── contexts/
│   │   │   └── StoreAttendantAuthContext.jsx
│   │   └── guards/
│   │       ├── StoreAttendantGuard.jsx
│   │       └── DispatcherGuard.jsx
│   └── public/
└── docker-compose.yml
```

---

## 👥 Roller ve Yetkiler

### Rol Hiyerarşisi

| Rol                | ID  | Yetkiler                       |
| ------------------ | --- | ------------------------------ |
| **SuperAdmin**     | 1   | Tüm yetkiler                   |
| **Admin**          | 2   | Yönetim paneli, tüm siparişler |
| **Moderator**      | 3   | İçerik yönetimi                |
| **User**           | 4   | Sipariş verme                  |
| **Courier**        | 5   | Teslimat işlemleri             |
| **Customer**       | 6   | Müşteri işlemleri              |
| **Guest**          | 7   | Sınırlı görüntüleme            |
| **StoreAttendant** | 8   | Sipariş hazırlama              |
| **Dispatcher**     | 9   | Kurye koordinasyonu            |

### Yetki Matrisi

| Eylem             | Admin | StoreAttendant | Dispatcher | Courier |
| ----------------- | :---: | :------------: | :--------: | :-----: |
| Sipariş Listele   |  ✅   |       ✅       |     ✅     |   ❌    |
| Hazırlamaya Başla |  ✅   |       ✅       |     ❌     |   ❌    |
| Hazır İşaretle    |  ✅   |       ✅       |     ❌     |   ❌    |
| Kurye Ata         |  ✅   |       ❌       |     ✅     |   ❌    |
| Teslim Et         |  ❌   |       ❌       |     ❌     |   ✅    |
| Kurye Yönet       |  ✅   |       ❌       |     ✅     |   ❌    |

---

## 📱 Panel Açıklamaları

### 1. Store Attendant Panel

**URL:** `/store/login` → `/store/dashboard`

**Amaç:** Market görevlisinin siparişleri hazırlaması

**Özellikler:**

- ✅ Onaylanmış siparişleri görüntüleme
- ✅ Hazırlamaya başlama
- ✅ Hazır olarak işaretleme
- ✅ Opsiyonel tartı girişi
- ✅ Gerçek zamanlı bildirimler
- ✅ Mobil uyumlu tasarım

**Dashboard Kartları:**

1. **Onay Bekleyen**: Henüz işleme alınmamış siparişler
2. **Hazırlanıyor**: Aktif hazırlanan siparişler
3. **Hazır**: Teslimata hazır siparişler
4. **Bugün Tamamlanan**: Günlük istatistik

---

### 2. Dispatcher Panel

**URL:** `/dispatch/login` → `/dispatch/dashboard`

**Amaç:** Sevkiyat koordinatörünün kurye ataması yapması

**Özellikler:**

- ✅ Hazır siparişleri görüntüleme
- ✅ Aktif kuryeleri listeleme
- ✅ Kurye atama/değiştirme
- ✅ Acil siparişleri önceliklendirme
- ✅ Kurye lokasyonunu takip
- ✅ Gerçek zamanlı güncellemeler

**Dashboard Bölümleri:**

1. **Sol Panel**: Hazır siparişler listesi
2. **Sağ Panel**: Aktif kuryeler
3. **Alt Bar**: Özet istatistikler
4. **Üst Bar**: Acil sipariş uyarıları

---

### 3. Admin Panel (Mevcut)

**URL:** `/admin`

**Güncellemeler:**

- ✅ StoreAttendant kullanıcı yönetimi
- ✅ Dispatcher kullanıcı yönetimi
- ✅ Rol tabanlı erişim kontrolü

---

### 4. Courier Panel (Mevcut)

**URL:** `/courier`

**Güncellemeler:**

- ✅ Dispatcher'dan gelen atamaları görme
- ✅ Teslimat durumu güncelleme
- ✅ Lokasyon paylaşımı

---

## 📡 API Referans

### Store Attendant API

```typescript
// Base: /api/StoreAttendantOrder

// Sipariş listesi
GET /orders
Query: status, page, pageSize
Response: { orders: [], summary: {}, totalPages, totalCount }

// Özet istatistikler
GET /summary
Response: { pendingCount, preparingCount, readyCount, ... }

// Hazırlamaya başla
POST /orders/{orderId}/start-preparing
Response: { success, message, order }

// Hazır olarak işaretle
POST /orders/{orderId}/mark-ready
Body: { weightInGrams?, notes? }
Response: { success, message, order }

// Siparişi onayla
POST /orders/{orderId}/confirm
Response: { success, message, order }
```

### Dispatcher API

```typescript
// Base: /api/DispatcherOrder

// Sipariş listesi
GET /orders
Query: status, page, pageSize
Response: { orders: [], summary: {}, totalPages, totalCount }

// Özet istatistikler
GET /summary
Response: { readyCount, assignedCount, availableCouriersCount, ... }

// Kurye listesi
GET /couriers
Response: { couriers: [], onlineCount, availableCount, busyCount }

// Acil siparişler
GET /orders/urgent
Response: { orders: [] }

// Kurye ata
POST /orders/{orderId}/assign
Body: { courierId }
Response: { success, message, order }

// Kurye değiştir
POST /orders/{orderId}/reassign
Body: { courierId, reason }
Response: { success, message, order }
```

---

## 🔔 SignalR Olayları

### StoreAttendant Hub

**URL:** `/hubs/storeattendant`

| Olay                 | Yön           | Açıklama                |
| -------------------- | ------------- | ----------------------- |
| `OrderStatusChanged` | Server→Client | Sipariş durumu değişti  |
| `NewOrderReceived`   | Server→Client | Yeni onaylanmış sipariş |
| `OrderCancelled`     | Server→Client | Sipariş iptal edildi    |
| `JoinStoreRoom`      | Client→Server | Odaya katıl             |

### Dispatcher Hub

**URL:** `/hubs/dispatcher`

| Olay                    | Yön           | Açıklama                    |
| ----------------------- | ------------- | --------------------------- |
| `OrderReady`            | Server→Client | Sipariş hazır               |
| `CourierAssigned`       | Server→Client | Kurye atandı                |
| `CourierLocationUpdate` | Server→Client | Kurye lokasyonu güncellendi |
| `CourierStatusChanged`  | Server→Client | Kurye durumu değişti        |
| `UrgentOrderAlert`      | Server→Client | Acil sipariş uyarısı        |

### Courier Hub

**URL:** `/hubs/courier`

| Olay             | Yön           | Açıklama            |
| ---------------- | ------------- | ------------------- |
| `NewAssignment`  | Server→Client | Yeni atama          |
| `OrderUpdated`   | Server→Client | Sipariş güncellendi |
| `UpdateLocation` | Client→Server | Lokasyon gönder     |
| `UpdateStatus`   | Client→Server | Durum güncelle      |

---

## 💾 Veritabanı Şeması

### Yeni/Güncellenen Tablolar

```sql
-- Users tablosu güncellemesi
ALTER TABLE Users ADD
    Role NVARCHAR(50) DEFAULT 'User';

-- AspNetRoles
-- Id=8: StoreAttendant
-- Id=9: Dispatcher

-- Orders tablosu
-- Mevcut status değerleri:
-- Pending, Confirmed, Preparing, Ready,
-- Assigned, OutForDelivery, Delivered, Cancelled
```

---

## 🛠️ Kurulum Rehberi

### 1. Gereksinimler

- .NET 9 SDK
- Node.js 18+
- Docker & docker-compose
- SQL Server (veya Docker ile)

### 2. Backend Kurulum

```bash
# Projeyi klonla
git clone <repo-url>
cd eticaret

# Docker servisleri başlat
docker-compose up -d

# Migration uygula (gerekirse)
cd src/ECommerce.API
dotnet ef database update
```

### 3. Frontend Kurulum

```bash
cd frontend

# Dependencies yükle
npm install

# Development server
npm start

# Production build
npm run build
```

### 4. Test Kullanıcıları

| Rol            | Email                   | Şifre    |
| -------------- | ----------------------- | -------- |
| Admin          | admin@admin.com         | admin123 |
| StoreAttendant | storeattendant@test.com | Test123! |
| Dispatcher     | dispatcher@test.com     | Test123! |

---

## 📊 FAZA Özeti

| FAZA | Açıklama                          | Durum |
| ---- | --------------------------------- | ----- |
| 1    | Backend: Roller ve Constants      | ✅    |
| 2    | Backend: SignalR Hub'ları         | ✅    |
| 3    | Backend: API Controller'ları      | ✅    |
| 4    | Frontend: Store Attendant Login   | ✅    |
| 5    | Frontend: Dispatcher Dashboard    | ✅    |
| 6    | Entegrasyon: Kurye Güncellemeleri | ✅    |
| 7    | Entegrasyon: Admin Güncellemeleri | ✅    |
| 8    | UI/UX: Mobile Responsive          | ✅    |
| 9    | Test ve Doğrulama                 | ✅    |
| 10   | Deployment ve Dokümantasyon       | ✅    |

---

## 📞 İletişim

Sorularınız için issue açabilir veya geliştirici ekibiyle iletişime geçebilirsiniz.

---

**Son Güncelleme:** 26 Ocak 2026  
**Versiyon:** 1.0.0
