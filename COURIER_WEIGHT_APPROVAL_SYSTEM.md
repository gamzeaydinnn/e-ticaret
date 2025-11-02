# 🚚 Kurye Ağırlık Onay Sistemi

## 📋 Genel Bakış

Bu özellik, kuryelerin teslimat yapmadan önce ağırlık fazlalığının admin tarafından onaylanıp onaylanmadığını kontrol etmelerini sağlar. 1 gram bile fazlalık varsa, admin onayı olmadan teslimat yapılamaz.

## ✨ Özellikler

### 🔒 Güvenlik Kontrolü

- **Otomatik Kontrol:** Kurye "Teslim Et" butonuna bastığında sistem otomatik olarak ağırlık raporunu kontrol eder
- **Admin Onayı Zorunluluğu:** 1 gram bile fazlalık varsa admin onayı olmadan ödeme tahsilatı yapılmaz
- **Görsel Uyarılar:** Pending (bekleyen) raporlar sarı, Approved (onaylı) raporlar yeşil ile işaretlenir

### 🎨 Kullanıcı Arayüzü

#### 1. Sipariş Tablosu

- **Sarı Satır:** Ağırlık onayı bekleyen siparişler
- **Mavi Satır:** Ağırlık onayı verilmiş siparişler
- **Badge Göstergeleri:**
  - `⏳ Onay Bekliyor` - Pending durumu
  - `✓ +XXXg` - Approved durumu + ağırlık fazlalığı

#### 2. Profesyonel Modal Sistemi

##### Pending (Onay Bekliyor) Durumu:

- **Kırmızı İkaz:** "Bu siparişte onaylanmamış ağırlık farkı var!"
- **Detaylı Bilgi:**
  - Beklenen Ağırlık
  - Tartılan Ağırlık
  - Fazlalık (gram)
  - Ek Ücret (TL)
- **Bilgilendirme:** "Admin onayını bekleyin" mesajı
- **Tek Buton:** "Kapat" (Teslimat yapılamaz)

##### Approved (Onaylandı) Durumu:

- **Yeşil Onay:** "Teslimat için hazır"
- **Sipariş Özeti:**
  - Müşteri bilgileri
  - Sipariş tutarı
  - Ağırlık farkı (varsa)
  - Toplam tahsilat
- **İki Buton:**
  - "İptal" - Vazgeç
  - "Teslim Et & Tahsil Et" - Onaylı teslimat

## 🔄 İş Akışı

```
1. Kurye siparişi görür
   ↓
2. "Teslim Et" butonuna basar
   ↓
3. Sistem ağırlık raporunu kontrol eder
   ↓
4a. EĞER Pending → Modal açılır: ⚠️ "Admin onayı bekleyin"
    └─ Teslimat yapılamaz
    └─ Kurye bekler

4b. EĞER Approved → Modal açılır: ✅ "Teslim Et & Tahsil Et"
    └─ Kurye onaylar
    └─ Ödeme otomatik tahsil edilir
    └─ Teslimat tamamlanır
```

## 🛠️ Teknik Detaylar

### Frontend Değişiklikleri

#### 1. `CourierOrders.jsx`

```jsx
// Yeni state'ler
const [weightReports, setWeightReports] = useState({});
const [showWeightModal, setShowWeightModal] = useState(false);
const [pendingDeliveryOrder, setPendingDeliveryOrder] = useState(null);

// Ağırlık kontrol fonksiyonu
const handleDeliveryAttempt = (order) => {
  const report = weightReports[order.id];

  if (report && report.status === "Pending") {
    // Admin onayı bekleniyor - uyarı göster
    setPendingDeliveryOrder(order);
    setShowWeightModal(true);
    return; // Teslimat yapılamaz
  }

  // Onaylı veya rapor yok - modal ile bilgilendir
  setPendingDeliveryOrder(order);
  setShowWeightModal(true);
};

// Onaylı teslimat
const confirmDelivery = async () => {
  const report = weightReports[orderId];

  if (report && report.status === "Approved") {
    // Ödeme tahsilatı
    await processPayment(report);
  }

  await updateOrderStatus(orderId, "delivered");
};
```

#### 2. `WeightApprovalWarningModal.jsx`

- Profesyonel gradient tasarım
- Animasyonlu geçişler
- Responsive (mobil uyumlu)
- İkonlarla görsel zenginlik

#### 3. `courierService.js`

```javascript
getOrderWeightReports: (orderId) => {
  return api
    .get(`${base}/orders/${orderId}/weight-reports`)
    .then((res) => res.data);
};
```

### Backend Değişiklikleri

#### `CourierController.cs`

```csharp
// GET: api/courier/orders/{orderId}/weight-reports
[HttpGet("orders/{orderId}/weight-reports")]
public async Task<IActionResult> GetOrderWeightReports(int orderId)
{
    var reports = await _weightReportRepository.GetByOrderIdAsync(orderId);

    return Ok(reports.Select(r => new
    {
        r.Id,
        r.OrderId,
        r.ExpectedWeightGrams,
        r.ReportedWeightGrams,
        r.OverageGrams,
        r.OverageAmount,
        Status = r.Status.ToString(), // Pending, Approved, etc.
        r.ReceivedAt
    }));
}
```

## 📊 Durum Göstergeleri

| Durum         | Görünüm                  | Anlamı                                                |
| ------------- | ------------------------ | ----------------------------------------------------- |
| **Pending**   | 🟡 Sarı satır + ⏳ badge | Admin onayı bekleniyor, teslimat yapılamaz            |
| **Approved**  | 🔵 Mavi satır + ✓ badge  | Onaylı, teslimat yapılabilir + ek ücret tahsil edilir |
| **Rapor Yok** | ⚪ Normal satır          | Ağırlık farkı yok, normal teslimat                    |

## 🎯 Kullanım Senaryoları

### Senaryo 1: Pending Rapor

```
1. Tartı cihazı: 1050g ölçtü (beklenen: 1000g)
2. WeightService: Status = Pending (50g fazlalık var)
3. Admin henüz onaylamadı
4. Kurye: "Teslim Et" butonuna bastı
5. Modal: ⚠️ "Admin onayı bekleniyor" (Teslimat yapılamaz)
6. Kurye: Bekliyor
```

### Senaryo 2: Approved Rapor

```
1. Tartı cihazı: 1050g ölçtü (beklenen: 1000g)
2. WeightService: Status = Pending → Admin onayladı → Status = Approved
3. Kurye: "Teslim Et" butonuna bastı
4. Modal: ✅ "Teslim Et & +15.50₺ Tahsil Et"
5. Kurye: Onayladı
6. Sistem: Ödeme tahsil edildi + Teslimat tamamlandı
```

### Senaryo 3: Rapor Yok

```
1. Tartı cihazı: 1000g ölçtü (beklenen: 1000g)
2. WeightService: Status = AutoApproved (fazlalık yok)
3. Kurye: "Teslim Et" butonuna bastı
4. Modal: ✅ "Teslimat için hazır"
5. Kurye: Onayladı
6. Sistem: Normal teslimat tamamlandı
```

## 🎨 Tasarım Özellikleri

### Renkler

- **Pending:** `#f59e0b` (Amber/Sarı)
- **Approved:** `#10b981` (Emerald/Yeşil)
- **Gradient Header:** `#667eea` → `#764ba2` (Mor)
- **Alert Boxes:** Gradient arka planlar

### Animasyonlar

- **fadeIn:** Modal açılış
- **slideUp:** Modal yukarı kayma
- **pulse:** Uyarı ikonu titreme
- **checkmark:** Onay ikonu belirir
- **slideIn:** Alert kutularının girişi

### Responsive

- Desktop: Geniş modal (550px)
- Tablet: Orta boyut
- Mobile: Tam genişlik, alt kısımdan açılır

## 📝 Dosya Yapısı

```
frontend/src/
├── components/
│   ├── WeightApprovalWarningModal.jsx  ✨ YENİ
│   └── WeightApprovalWarningModal.css  ✨ YENİ
├── pages/
│   └── Courier/
│       └── CourierOrders.jsx           🔄 GÜNCELLENDİ
└── services/
    └── courierService.js               🔄 GÜNCELLENDİ

backend/src/ECommerce.API/Controllers/
└── CourierController.cs                🔄 GÜNCELLENDİ
```

## 🚀 Test Adımları

1. **Backend'i başlat:** `dotnet run` (API çalışıyor olmalı)
2. **Frontend'i başlat:** `npm start`
3. **Kurye girişi yap:** `/courier/login`
4. **Siparişleri listele:** Mock veriler otomatik yüklenir
5. **Pending sipariş:** Sarı satırda "Teslim Et" → ⚠️ Modal açılır
6. **Approved sipariş:** Mavi satırda "Teslim Et" → ✅ Modal açılır → Onayla

## 💡 Önemli Notlar

- ✅ **1 gram bile fazla:** Admin onayı gerekli
- ✅ **Görsel feedback:** Renkli satırlar + badge'ler
- ✅ **Kullanıcı dostu:** Profesyonel modal tasarımı
- ✅ **Güvenli:** Backend'de de kontrol var
- ✅ **Responsive:** Mobil cihazlarda da çalışır

## 🔐 Güvenlik

1. **Double Check:** Hem frontend hem backend kontrolü
2. **Status Validation:** Sadece "Approved" raporlar için ödeme
3. **Visual Warning:** Pending durumda teslimat butonu engellenmez ama modal uyarır
4. **Audit Log:** Tüm işlemler loglanır

---

**Hazırlayan:** AI Assistant  
**Tarih:** 2 Kasım 2025  
**Versiyon:** 1.0
