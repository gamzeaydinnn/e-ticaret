# 📦 Sipariş Akış Dokümantasyonu

## Genel Bakış

Bu dokümantasyon, e-ticaret sistemindeki sipariş akışının tüm rollerle nasıl entegre çalıştığını açıklar.

## 🔄 Sipariş Durumları

```
Pending → Confirmed → Preparing → Ready → Assigned → OutForDelivery → Delivered
                                    ↘️
                                   Cancelled / DeliveryFailed
```

## 👥 Roller ve Sorumlulukları

### 1. **Müşteri (Kayıtlı/Misafir)**

- Sipariş oluşturma
- Sipariş takibi
- Sipariş durumu değişikliklerinde bildirim alma

### 2. **Mağaza Görevlisi (Store Attendant)**

- Yeni siparişleri görme
- Sipariş hazırlamaya başlama (`Confirmed` → `Preparing`)
- Sipariş hazır işaretleme (`Preparing` → `Ready`)
- Kurye atama (opsiyonel)

### 3. **Sevkiyat Görevlisi (Dispatcher)**

- Hazır siparişleri görme
- Kuryeye sipariş atama (`Ready` → `Assigned`)
- Kurye değiştirme

### 4. **Kurye (Courier)**

- Atanan siparişleri görme
- Sipariş teslim alma
- Teslimat durumunu güncelleme
- Teslim etme (`OutForDelivery` → `Delivered`)

### 5. **Admin**

- Tüm siparişleri görme ve yönetme
- Tüm durum değişikliklerini takip etme
- Manuel durum değiştirme yetkisi

## 🔔 Bildirim Akışı

### Sipariş Oluşturulduğunda

```
Müşteri → Backend → SignalR →
  ├── Admin Panel (NewOrder)
  ├── Store Attendant Panel (NewOrderForStore)
  └── Müşteri (OrderStatusChanged)
```

### Mağaza Görevlisi Durumu Değiştirdiğinde

```
Store Attendant → Backend → SignalR →
  ├── Admin Panel (OrderStatusChanged)
  ├── Dispatcher Panel (OrderStatusChanged)
  ├── Kurye Panel (eğer atanmışsa)
  └── Müşteri (OrderStatusChanged)
```

### Kurye Atandığında

```
Dispatcher → Backend → SignalR →
  ├── Admin Panel (OrderStatusChanged)
  ├── Store Attendant Panel (OrderStatusChanged)
  ├── Kurye Panel (NewOrderAssigned) + SES
  └── Müşteri (OrderStatusChanged)
```

## 🧑‍💻 Misafir Kullanıcı Yönetimi

### Session Bazlı Ayrım

- Her tarayıcı penceresi için **benzersiz session ID** oluşturulur
- `sessionStorage` kullanılarak farklı tarayıcılarda farklı misafir kullanıcılar ayrılır
- Aynı tarayıcıda farklı tab'larda **aynı session** paylaşılır

### Sipariş Kaydı

```javascript
// SessionStorage'a kaydedilen misafir siparişi
{
  orderNumber: "ORD-12345",
  orderId: 123,
  email: "misafir@email.com",
  totalPrice: 150.00,
  createdAt: "2026-01-29T10:00:00Z",
  status: "paid",
  sessionId: "abc123-...", // Hangi session'dan geldiği
}
```

### Sipariş Takibi (Misafir)

1. **Polling mekanizması**: Her 15 saniyede sipariş durumu kontrol edilir
2. **Email + Sipariş No ile sorgulama**: Manuel arama imkanı
3. **LocalStorage + SessionStorage**: Siparişler her iki storage'da da tutulur

## 📡 SignalR Hub'ları

| Hub                  | Endpoint         | Kullanım                        |
| -------------------- | ---------------- | ------------------------------- |
| OrderHub             | `/hubs/order`    | Müşteri sipariş takibi          |
| AdminNotificationHub | `/hubs/admin`    | Admin bildirimleri              |
| CourierHub           | `/hubs/courier`  | Kurye bildirimleri              |
| StoreAttendantHub    | `/hubs/store`    | Mağaza görevlisi bildirimleri   |
| DispatcherHub        | `/hubs/dispatch` | Sevkiyat görevlisi bildirimleri |

## 🔊 Ses Bildirimleri

| Olay            | Ses Tipi           | Hedef                  |
| --------------- | ------------------ | ---------------------- |
| Yeni sipariş    | `new_order`        | Admin, Store Attendant |
| Sipariş hazır   | `order_ready`      | Dispatcher             |
| Kurye ataması   | `new_assignment`   | Kurye                  |
| Teslimat sorunu | `delivery_problem` | Admin                  |

## 🚀 Test Senaryoları

### Senaryo 1: Normal Akış

1. Müşteri sipariş verir
2. Mağaza görevlisi "Hazırlanıyor" yapar
3. Mağaza görevlisi "Hazır" yapar
4. Dispatcher kuryeye atar
5. Kurye teslim eder

### Senaryo 2: Misafir Kullanıcı

1. Misafir olarak sipariş ver
2. Farklı tarayıcıda misafir olarak sipariş ver (farklı session)
3. Her tarayıcıda sadece kendi siparişlerini gör
4. Email + sipariş no ile sipariş sorgula

### Senaryo 3: Real-time Bildirim

1. İki farklı tarayıcıda Admin ve Mağaza paneli aç
2. Mağaza panelinde durumu değiştir
3. Admin panelinde anında güncelleme gör

## 📝 Önemli Notlar

1. **SignalR Bağlantı Grupları**:
   - Kurye: `courier-{courierId}`
   - Sipariş: `order-{orderId}`
   - Admin: `admin-notifications`
   - Store: `store-room`
   - Dispatch: `dispatch-room`

2. **Token Yönetimi**:
   - Kayıtlı kullanıcı: JWT token (localStorage)
   - Misafir: CartToken (sessionStorage) - her tarayıcı için benzersiz

3. **Fallback Mekanizması**:
   - SignalR bağlantısı yoksa polling kullanılır (15 saniye)
   - Misafir kullanıcılar için her zaman polling aktif
