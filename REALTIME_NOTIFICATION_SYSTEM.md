# 🔔 Real-Time Bildirim Sistemi Dokümantasyonu

## Genel Bakış

Bu dokümantasyon, e-ticaret platformundaki gerçek zamanlı bildirim sistemini açıklar. Sistem, sipariş durumu değişikliklerini tüm ilgili taraflara anlık olarak iletir.

## Sipariş Akışı ve Bildirimler

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SİPARİŞ AKIŞI VE BİLDİRİMLER                        │
└─────────────────────────────────────────────────────────────────────────────┘

1. MÜŞTERİ SİPARİŞ VERİR
   │
   ├─── 🔔 Admin Panel → "Yeni Sipariş" bildirimi
   └─── 🔔 Store Attendant → "Yeni Sipariş" bildirimi + SES

2. ADMİN SİPARİŞİ ONAYLAR (Confirmed)
   │
   ├─── 🔔 Store Attendant → "Sipariş Onaylandı" + SES
   └─── 🔔 Müşteri → "Siparişiniz Onaylandı" bildirimi

3. STORE ATTENDANT HAZIRLAMA BAŞLATIR (Preparing)
   │
   ├─── 🔔 Admin Panel → Durum güncellendi
   ├─── 🔔 Dispatcher → Hazırlanıyor bildirimi
   └─── 🔔 Müşteri → "Siparişiniz Hazırlanıyor" + SES

4. STORE ATTENDANT HAZIR İŞARETLER (Ready)
   │
   ├─── 🔔 Admin Panel → Durum güncellendi
   ├─── 🔔 Dispatcher → "Sipariş Hazır - Kurye Ata" + SES
   └─── 🔔 Müşteri → "Siparişiniz Hazırlandı"

5. DISPATCHER KURYE ATAR (Assigned)
   │
   ├─── 🔔 Admin Panel → Kurye atandı
   ├─── 🔔 Kurye → "Yeni Sipariş Atandı" + SES
   ├─── 🔔 Store Attendant → Kurye bilgisi
   └─── 🔔 Müşteri → "Kuryeniz Atandı"

6. KURYE TESLİM ALIR (PickedUp)
   │
   ├─── 🔔 Admin Panel → Durum güncellendi
   ├─── 🔔 Dispatcher → Kurye teslim aldı
   └─── 🔔 Müşteri → "Siparişiniz Kurye'de"

7. KURYE YOLA ÇIKAR (OutForDelivery)
   │
   ├─── 🔔 Admin Panel → Kurye yolda
   ├─── 🔔 Dispatcher → Teslimat başladı
   └─── 🔔 Müşteri → "Kurye Yolda!" + SES + Konum Takibi

8. KURYE TESLİM EDER (Delivered)
   │
   ├─── 🔔 Admin Panel → Teslim edildi
   ├─── 🔔 Dispatcher → Teslimat tamamlandı
   ├─── 🔔 Store Attendant → Sipariş tamamlandı
   └─── 🔔 Müşteri → "Siparişiniz Teslim Edildi!" + SES
```

## SignalR Hub'ları

| Hub                  | Endpoint         | Kullanan        | Amaç                  |
| -------------------- | ---------------- | --------------- | --------------------- |
| OrderHub             | `/hubs/order`    | Müşteri         | Sipariş takibi        |
| AdminNotificationHub | `/hubs/admin`    | Admin           | Yönetim bildirimleri  |
| StoreAttendantHub    | `/hubs/store`    | Store Attendant | Mağaza bildirimleri   |
| DispatcherHub        | `/hubs/dispatch` | Dispatcher      | Sevkiyat bildirimleri |
| CourierHub           | `/hubs/courier`  | Kurye           | Teslimat bildirimleri |

## Frontend Entegrasyonu

### Dispatcher Dashboard (Yeni Eklendi)

```javascript
// SignalR bağlantısı
useEffect(() => {
  signalRService.connectDispatcher();

  signalRService.onDispatcherEvent("OrderReady", (data) => {
    playSound("newOrder");
    showBrowserNotification(
      "Sipariş Hazır!",
      `Sipariş #${data.orderNumber} kurye bekliyor`,
    );
    fetchData();
  });

  return () => signalRService.disconnectDispatcher();
}, [isAuthenticated]);
```

### Store Attendant Dashboard (Güncellendi)

```javascript
useEffect(() => {
  signalRService.connectStoreAttendant();

  signalRService.onStoreAttendantEvent("NewOrderForStore", (data) => {
    playSound("newOrder");
    showBrowserNotification(
      "🛒 Yeni Sipariş!",
      `Sipariş #${data.orderNumber} geldi`,
    );
    fetchData();
  });
}, [isAuthenticated]);
```

### Kurye Dashboard (Güncellendi)

```javascript
useEffect(() => {
  signalRService.connectCourier(courier.id);

  // Backend "NewOrderAssigned" gönderiyor
  courierHub.on("NewOrderAssigned", (data) => {
    playNotificationSound();
    showNotification(
      "🚴 Yeni Sipariş!",
      `Sipariş #${data.orderNumber} size atandı`,
    );
    loadOrders();
  });
}, [courier?.id]);
```

### Müşteri Sipariş Takibi (Güncellendi)

```javascript
useEffect(() => {
  signalRService.connectCustomer();

  signalRService.onOrderStatusChanged((data) => {
    playNotificationSound();
    showBrowserNotification(
      `📦 Sipariş #${data.orderNumber}`,
      statusInfo.label,
    );
    updateOrderStatus(data);
  });
}, []);
```

## Admin Panel Tam Kontrol

Admin paneline manuel durum değiştirme özelliği eklendi:

- Acil durumlar için tüm durumları seçebilme
- Dropdown ile hızlı durum geçişi
- İptal butonu
- Tüm taraflara otomatik bildirim

```javascript
<select onChange={(e) => updateOrderStatus(orderId, e.target.value)}>
  <option value="new">🆕 Yeni Sipariş</option>
  <option value="confirmed">✅ Onaylandı</option>
  <option value="preparing">🍳 Hazırlanıyor</option>
  <option value="ready">📦 Hazır</option>
  <option value="assigned">🚴 Kuryeye Atandı</option>
  <option value="out_for_delivery">🛵 Yolda</option>
  <option value="delivered">✓ Teslim Edildi</option>
  <option value="cancelled">🚫 İptal Edildi</option>
</select>
```

## Ses Bildirimleri

### Ses Dosyaları

| Dosya                                           | Kullanım                     |
| ----------------------------------------------- | ---------------------------- |
| `/sounds/mixkit-melodic-race-countdown-1955.wav` | Yeni sipariş, Uyarı          |
| `/sounds/mixkit-bell-notification-933.wav`       | Genel bildirim, Kurye atandı |
| `/sounds/mixkit-happy-bells-notification-937.wav`| Sipariş hazır, Başarı        |

### Fallback Mekanizması

WAV dosyası bulunamazsa Web Audio API ile beep sesi oluşturulur:

```javascript
// notificationSound.js
export const playBeep = (frequency = 800, duration = 200) => {
  const audioContext = new AudioContext();
  const oscillator = audioContext.createOscillator();
  oscillator.frequency.value = frequency;
  oscillator.start();
  oscillator.stop(audioContext.currentTime + duration / 1000);
};
```

## Browser Notification

Tüm panellerde browser notification desteği:

```javascript
const showBrowserNotification = (title, body) => {
  if (Notification.permission === "granted") {
    new Notification(title, {
      body,
      icon: "/logo192.png",
      tag: "order-notification",
      requireInteraction: true,
    });
  }
};
```

## Google Maps Entegrasyonu

Kurye panelinde müşteri adresine yönlendirme:

```javascript
const openGoogleMaps = () => {
  if (task?.deliveryLatitude && task?.deliveryLongitude) {
    window.open(
      `https://www.google.com/maps/dir/?api=1&destination=${task.deliveryLatitude},${task.deliveryLongitude}`,
      "_blank",
    );
  } else if (task?.deliveryAddress) {
    window.open(
      `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(task.deliveryAddress)}`,
      "_blank",
    );
  }
};
```

## Dosya Değişiklikleri

### Güncellenen Dosyalar

1. **DispatcherDashboard.jsx**
   - SignalR bağlantısı eklendi
   - Browser notification eklendi
   - Ses bildirimi tetikleyici eklendi

2. **StoreAttendantDashboard.jsx**
   - Browser notification eklendi
   - Yeni sipariş geldiğinde ses + bildirim

3. **CourierDashboard.jsx**
   - Event listener'lar düzeltildi (NewOrderAssigned)
   - courierHub kullanımına geçildi

4. **OrderTracking.jsx** (Müşteri)
   - Ses bildirimi eklendi
   - Browser notification eklendi

5. **AdminOrders.jsx**
   - Manuel durum değiştirme paneli eklendi
   - Tüm durumları seçebilme dropdown'u

### Yeni Dosyalar

1. **frontend/src/utils/notificationSound.js**
   - Merkezi ses yönetimi
   - Web Audio API fallback
   - Browser notification helper

2. **frontend/public/sounds/README.md**
   - Ses dosyası gereksinimleri

## Test Senaryoları

### Senaryo 1: Yeni Sipariş Akışı

1. Müşteri sipariş verir
2. ✅ Admin panelinde bildirim görünür
3. ✅ Store Attendant panelinde ses çalar + bildirim
4. Admin siparişi onaylar
5. ✅ Store Attendant'a "Sipariş Onaylandı" bildirimi
6. ✅ Müşteriye bildirim
7. Store hazırlamaya başlar
8. ✅ Tüm taraflara bildirim

### Senaryo 2: Kurye Ataması

1. Sipariş "Ready" durumunda
2. ✅ Dispatcher'a ses + bildirim
3. Dispatcher kurye atar
4. ✅ Kurye'ye "NewOrderAssigned" bildirimi + ses
5. ✅ Müşteriye "Kuryeniz atandı" bildirimi

### Senaryo 3: Teslimat

1. Kurye teslim alır
2. ✅ Tüm taraflara bildirim
3. Kurye yola çıkar
4. ✅ Müşteriye özel bildirim + ses
5. Kurye teslim eder
6. ✅ Tüm taraflara "Teslim Edildi" bildirimi

## Notlar

- Browser notification için kullanıcı izni gerekli
- Ses için kullanıcı etkileşimi gerekebilir (autoplay policy)
- SignalR bağlantısı kesilirse polling devreye girer
- Admin tüm durumları manuel değiştirebilir

---

**Son Güncelleme:** Ocak 2026
**Versiyon:** 2.0
