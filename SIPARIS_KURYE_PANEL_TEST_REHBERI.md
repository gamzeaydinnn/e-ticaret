# 📋 Sipariş-Kurye-Panel Sistemi Test Rehberi

> Bu doküman FAZA 9 kapsamında hazırlanmış manuel test senaryolarını içerir.

---

## 🔐 Test Kullanıcıları

| Rol                 | Email                   | Şifre    |
| ------------------- | ----------------------- | -------- |
| **Admin**           | admin@admin.com         | admin123 |
| **Store Attendant** | storeattendant@test.com | Test123! |
| **Dispatcher**      | dispatcher@test.com     | Test123! |
| **Demo User**       | demo@example.com        | test123  |

---

## 📡 API Endpoint Testleri

### 1. Auth API

```bash
# Login Test
POST http://localhost:5000/api/auth/login
Body: {"email": "storeattendant@test.com", "password": "Test123!"}
Beklenen: 200 OK, JWT token döner
```

### 2. Store Attendant API

| Endpoint                                               | Method | Yetki                 | Açıklama              |
| ------------------------------------------------------ | ------ | --------------------- | --------------------- |
| `/api/StoreAttendantOrder/orders`                      | GET    | StoreAttendant, Admin | Sipariş listesi       |
| `/api/StoreAttendantOrder/summary`                     | GET    | StoreAttendant, Admin | Özet istatistikler    |
| `/api/StoreAttendantOrder/orders/{id}/start-preparing` | POST   | StoreAttendant, Admin | Hazırlamaya başla     |
| `/api/StoreAttendantOrder/orders/{id}/mark-ready`      | POST   | StoreAttendant, Admin | Hazır olarak işaretle |

### 3. Dispatcher API

| Endpoint                                  | Method | Yetki             | Açıklama           |
| ----------------------------------------- | ------ | ----------------- | ------------------ |
| `/api/DispatcherOrder/orders`             | GET    | Dispatcher, Admin | Sipariş listesi    |
| `/api/DispatcherOrder/summary`            | GET    | Dispatcher, Admin | Özet istatistikler |
| `/api/DispatcherOrder/couriers`           | GET    | Dispatcher, Admin | Kurye listesi      |
| `/api/DispatcherOrder/orders/{id}/assign` | POST   | Dispatcher, Admin | Kurye ata          |
| `/api/DispatcherOrder/orders/urgent`      | GET    | Dispatcher, Admin | Acil siparişler    |

---

## 🧪 Manuel Test Senaryoları

### Senaryo 1: Store Attendant Panel Login

**Adımlar:**

1. Tarayıcıda `http://localhost:3000/store/login` adresine git
2. Email: `storeattendant@test.com`
3. Şifre: `Test123!`
4. "Giriş Yap" butonuna tıkla

**Beklenen Sonuç:**

- ✅ Dashboard sayfasına yönlendirilir
- ✅ Sidebar'da menü öğeleri görünür
- ✅ Sipariş özeti kartları görünür

---

### Senaryo 2: Sipariş Hazırlama Akışı

**Önkoşul:** Store Attendant olarak giriş yapılmış olmalı

**Adımlar:**

1. Dashboard'da "Onay Bekleyen" siparişi bul
2. "Hazırlamaya Başla" butonuna tıkla
3. Sipariş durumunun "Hazırlanıyor" olarak değiştiğini gör
4. "Hazır Olarak İşaretle" butonuna tıkla
5. (Varsa) Tartı bilgisi gir
6. Onayla

**Beklenen Sonuç:**

- ✅ Sipariş durumu güncellenir
- ✅ SignalR ile Dispatcher paneline bildirim gider
- ✅ Özet istatistikler güncellenir

---

### Senaryo 3: Dispatcher Panel Login

**Adımlar:**

1. Tarayıcıda `http://localhost:3000/dispatch/login` adresine git
2. Email: `dispatcher@test.com`
3. Şifre: `Test123!`
4. "Giriş Yap" butonuna tıkla

**Beklenen Sonuç:**

- ✅ Dispatcher Dashboard açılır
- ✅ Hazır siparişler listesi görünür
- ✅ Aktif kuryeler görünür

---

### Senaryo 4: Kurye Atama

**Önkoşul:** Dispatcher olarak giriş yapılmış ve "Ready" durumunda sipariş olmalı

**Adımlar:**

1. "Hazır Siparişler" listesinden bir sipariş seç
2. "Kurye Ata" butonuna tıkla
3. Listeden müsait bir kurye seç
4. "Atamayı Onayla" butonuna tıkla

**Beklenen Sonuç:**

- ✅ Sipariş kurye'ye atanır
- ✅ Kurye'nin aktif sipariş sayısı artar
- ✅ SignalR ile kurye uygulamasına bildirim gider

---

### Senaryo 5: Mobil Uyumluluk

**Adımlar:**

1. Tarayıcıda Developer Tools aç (F12)
2. Device Toolbar'ı aktif et
3. iPhone X veya benzeri cihaz seç
4. Store Attendant Dashboard'u aç
5. Dispatcher Dashboard'u aç

**Beklenen Sonuç:**

- ✅ Sidebar collapse oluyor
- ✅ Bottom navigation görünür
- ✅ Kartlar responsive
- ✅ Butonlar touch-friendly (min 44x44px)

---

## 🔔 SignalR Hub Testleri

### StoreAttendant Hub

```javascript
// Hub URL: /hubs/storeattendant
// Events:
-OrderStatusChanged - NewOrderReceived - OrderCancelled;
```

### Dispatcher Hub

```javascript
// Hub URL: /hubs/dispatcher
// Events:
-OrderReady - CourierLocationUpdate - CourierStatusChanged;
```

---

## 📊 Özet Rapor

| Test                      | Durum         |
| ------------------------- | ------------- |
| StoreAttendant Login API  | ✅ Başarılı   |
| Dispatcher Login API      | ✅ Başarılı   |
| StoreAttendant Orders API | ✅ Başarılı   |
| Dispatcher Orders API     | ✅ Başarılı   |
| Dispatcher Couriers API   | ✅ Başarılı   |
| Frontend Build            | ✅ Başarılı   |
| Docker Container          | ✅ Çalışıyor  |
| Database Seed             | ✅ Tamamlandı |

---

## 🐛 Bilinen Sorunlar

1. **Port Konfigürasyonu**: API `5000` portunda çalışıyor (docker-compose'da belirtildiği gibi)
2. **Email Doğrulama**: Test kullanıcıları için email doğrulaması SQL ile atlandı

---

## 📅 Test Tarihi

- **Tarih**: 26 Ocak 2026
- **Versiyon**: FAZA 9 - Test ve Doğrulama
- **Test Eden**: Sistem
