# Teslimat Yönetim Sistemi - Kullanıcı Kılavuzu

## 📋 İçindekiler

1. [Genel Bakış](#genel-bakış)
2. [Admin Paneli](#admin-paneli)
3. [Kurye Uygulaması](#kurye-uygulaması)
4. [Durum Akışları](#durum-akışları)
5. [Hata Yönetimi](#hata-yönetimi)
6. [SSS](#sss)

---

## Genel Bakış

Teslimat Yönetim Sistemi, e-ticaret siparişlerinin kuryeler aracılığıyla müşterilere teslimini yönetir. Sistem aşağıdaki temel bileşenlerden oluşur:

### Temel Özellikler

| Özellik                      | Açıklama                                       |
| ---------------------------- | ---------------------------------------------- |
| 🎯 **Akıllı Kurye Atama**    | Mesafe, yük ve performansa göre otomatik atama |
| 📍 **Gerçek Zamanlı Takip**  | Kurye konumlarını canlı görüntüleme            |
| ✅ **Teslimat Kanıtı (POD)** | Fotoğraf ve OTP ile teslimat doğrulama         |
| 🔔 **Anlık Bildirimler**     | SignalR ile gerçek zamanlı güncellemeler       |
| 📊 **Detaylı Raporlama**     | Performans ve teslimat istatistikleri          |

### Sistem Mimarisi

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   Admin Panel   │────▶│    Backend API  │◀────│  Kurye Mobile   │
└─────────────────┘     └─────────────────┘     └─────────────────┘
                               │
                               ▼
                        ┌─────────────────┐
                        │    Database     │
                        └─────────────────┘
```

---

## Admin Paneli

### 1. Giriş ve Yetkilendirme

Admin paneline erişmek için:

1. `https://domain.com/admin` adresine gidin
2. Kullanıcı adı ve şifrenizi girin
3. İki faktörlü doğrulama (varsa) tamamlayın

**İzin Seviyeleri:**

| İzin              | Açıklama                         |
| ----------------- | -------------------------------- |
| `Orders.View`     | Teslimat görevlerini görüntüleme |
| `Orders.Manage`   | Görev oluşturma, atama, iptal    |
| `Couriers.View`   | Kurye listesini görüntüleme      |
| `Couriers.Manage` | Kurye ekleme, düzenleme, silme   |

### 2. Teslimat Görevleri

#### 2.1 Görev Listesi

Teslimat görevlerini görüntülemek için:

1. Sol menüden **Teslimatlar** seçin
2. Filtreleme seçenekleri:
   - **Durum**: Bekliyor, Atandı, Teslim Edildi, vb.
   - **Tarih**: Belirli bir tarih aralığı
   - **Kurye**: Belirli bir kurye

#### 2.2 Yeni Görev Oluşturma

Sipariş onaylandığında otomatik olarak teslimat görevi oluşturulur. Manuel oluşturmak için:

1. **+ Yeni Görev** butonuna tıklayın
2. Sipariş ID'sini seçin
3. **Oluştur** butonuna tıklayın

```json
// API İsteği Örneği
POST /api/admin/delivery-tasks
{
  "orderId": 12345
}
```

#### 2.3 Kurye Atama

**Manuel Atama:**

1. Görev satırında **Kurye Ata** butonuna tıklayın
2. Listeden uygun kuryeyi seçin
3. **Ata** butonuna tıklayın

**Otomatik Atama:**

1. **Otomatik Ata** butonuna tıklayın
2. Sistem en uygun kuryeyi seçer

**Atama Algoritması Kriterleri:**

- Kurye mesafesi (≤10 km)
- Mevcut görev sayısı (≤5)
- Aktif durumda olma
- Ortalama tamamlama süresi (performans)

#### 2.4 Görev İptali

1. Görev detayına gidin
2. **İptal Et** butonuna tıklayın
3. İptal sebebini girin
4. Onaylayın

⚠️ **Dikkat:** `Delivered` durumundaki görevler iptal edilemez.

### 3. Kurye Yönetimi

#### 3.1 Kurye Listesi

- **Aktif**: Çevrimiçi ve görev kabul edebilir
- **Meşgul**: Aktif teslimat yapıyor
- **Çevrimdışı**: Uygulamaya bağlı değil

#### 3.2 Kurye Performansı

| Metrik          | Açıklama                  | Hedef  |
| --------------- | ------------------------- | ------ |
| Tamamlama Oranı | Başarılı teslimat yüzdesi | >95%   |
| Ortalama Süre   | Kabul-Teslimat arası süre | <45 dk |
| Müşteri Puanı   | Ortalama değerlendirme    | >4.5/5 |

### 4. Canlı Harita

Kuryelerin konumlarını gerçek zamanlı görüntüleyin:

1. **Canlı Harita** sayfasına gidin
2. Renk kodları:
   - 🟢 Yeşil: Aktif, görev bekliyor
   - 🔵 Mavi: Teslimat yapıyor
   - ⚫ Gri: Çevrimdışı

---

## Kurye Uygulaması

### 1. Giriş

1. Uygulamayı açın
2. Telefon numarası ile giriş yapın
3. SMS ile gelen OTP kodunu girin

### 2. Ana Ekran

Ana ekranda görüntülenen bilgiler:

- Atanan görevler
- Bugünkü tamamlanan teslimatlar
- Kazanç özeti

### 3. Görev Akışı

#### 3.1 Görev Kabul/Ret

Yeni görev geldiğinde:

```
┌──────────────────────────────────────┐
│         🚚 YENİ GÖREV                │
│                                      │
│  Teslimat Adresi:                    │
│  Kadıköy, İstanbul                   │
│                                      │
│  Mesafe: 3.2 km                      │
│  Tahmini Süre: 15 dk                 │
│                                      │
│  ┌─────────┐    ┌─────────────────┐ │
│  │  REDDET │    │ KABUL ET (58s)  │ │
│  └─────────┘    └─────────────────┘ │
└──────────────────────────────────────┘
```

⏱️ **Timeout:** 60 saniye içinde yanıt verilmezse görev başka kuryeye atanır.

#### 3.2 Paketi Alma (Pickup)

1. Depo/mağazaya gidin
2. Paketi teslim alın
3. **Paketi Aldım** butonuna tıklayın

#### 3.3 Teslimat Yolunda (In Transit)

- Navigasyon otomatik başlar
- Konum 30 saniyede bir güncellenir
- Müşteri gerçek zamanlı takip edebilir

#### 3.4 Teslimat Tamamlama

**Fotoğraflı Teslimat:**

1. Paketi müşteriye teslim edin
2. Fotoğraf çekin
3. **Teslimat Tamamlandı** butonuna tıklayın

**OTP ile Teslimat:**

1. Müşteriden 6 haneli kodu isteyin
2. Kodu girin
3. **Doğrula ve Tamamla** butonuna tıklayın

#### 3.5 Başarısız Teslimat

Teslimat yapılamadıysa:

1. **Teslimat Başarısız** butonuna tıklayın
2. Sebep seçin:
   - Müşteri bulunamadı
   - Adres yanlış
   - Müşteri reddetti
   - Diğer
3. Not ekleyin (opsiyonel)
4. Onaylayın

### 4. Konum Paylaşımı

Uygulama arka planda konum gönderir:

- **Güncelleme sıklığı:** 30 saniye
- **Batarya optimizasyonu:** Aktif
- **Manuel güncelleme:** Çekin ve bırakın

### 5. Offline Mod

İnternet bağlantısı kesildiğinde:

- Son görev bilgileri saklanır
- Durum güncellemeleri kuyruklanır
- Bağlantı geldiğinde senkronize edilir

---

## Durum Akışları

### Teslimat Görevi Durumları

```
                    ┌──────────────────────────────────────────────────────────┐
                    │                    NORMAL AKIŞ                           │
                    └──────────────────────────────────────────────────────────┘

    ┌─────────┐     ┌──────────┐     ┌──────────┐     ┌───────────┐     ┌───────────┐
    │ Pending │────▶│ Assigned │────▶│ Accepted │────▶│ PickedUp  │────▶│ InTransit │
    └─────────┘     └──────────┘     └──────────┘     └───────────┘     └───────────┘
         │               │                                                    │
         │               │                                                    │
         │               ▼                                                    │
         │          ┌──────────┐                                             │
         │          │ Rejected │ (Başka kuryeye atanır)                      │
         │          └──────────┘                                             │
         │                                                                    │
         │                                                                    ▼
         │                                              ┌───────────┐   ┌──────────┐
         │                                              │ Delivered │   │  Failed  │
         │                                              └───────────┘   └──────────┘
         │                                                                    │
         │                                                                    ▼
         │                                                             ┌────────────┐
         │                                                             │ Rescheduled│
         │                                                             └────────────┘
         │
         └────────────────────────────────────────────────────────────────────────────▶
                                                                        ┌───────────┐
                                                                        │ Cancelled │
                                                                        └───────────┘
```

### Durum Açıklamaları

| Durum             | Kod           | Açıklama                          | Sonraki Durumlar       |
| ----------------- | ------------- | --------------------------------- | ---------------------- |
| Bekliyor          | `Pending`     | Görev oluşturuldu, kurye atanmadı | Assigned, Cancelled    |
| Atandı            | `Assigned`    | Kurye atandı, yanıt bekleniyor    | Accepted, Rejected     |
| Kabul Edildi      | `Accepted`    | Kurye görevi kabul etti           | PickedUp               |
| Teslim Alındı     | `PickedUp`    | Paket kuryede                     | InTransit              |
| Yolda             | `InTransit`   | Kurye müşteriye gidiyor           | Delivered, Failed      |
| Teslim Edildi     | `Delivered`   | Başarıyla teslim edildi           | -                      |
| Başarısız         | `Failed`      | Teslimat yapılamadı               | Rescheduled            |
| Yeniden Planlandı | `Rescheduled` | Tekrar denenecek                  | Assigned               |
| İptal             | `Cancelled`   | Görev iptal edildi                | -                      |
| Reddedildi        | `Rejected`    | Kurye reddetti                    | Assigned (başka kurye) |

---

## Hata Yönetimi

### Otomatik Hata İşleme

#### 1. Kurye Timeout (60 saniye)

Kurye 60 saniye içinde görevi kabul etmezse:

1. Görev otomatik olarak geri alınır
2. Yeni en uygun kurye bulunur
3. Görev yeniden atanır

**Maksimum deneme:** 3 kurye

#### 2. Kurye Çevrimdışı Algılama

Kurye 5 dakika boyunca konum güncellemezse:

1. Sistem alarm oluşturur
2. Admin panelinde bildirim gösterilir
3. Aktif görevler yeniden atanabilir

#### 3. Teslimat Yeniden Deneme

Başarısız teslimat sonrası:

- **Maksimum deneme:** 3
- **Yeniden deneme aralığı:** 2 saat
- 3 denemeden sonra: İade görevi oluşturulur

### Hata Kodları

| Kod                         | Mesaj                   | Çözüm                            |
| --------------------------- | ----------------------- | -------------------------------- |
| `TASK_NOT_FOUND`            | Görev bulunamadı        | Görev ID'yi kontrol edin         |
| `INVALID_STATUS_TRANSITION` | Geçersiz durum geçişi   | Durum akış şemasını kontrol edin |
| `COURIER_NOT_AVAILABLE`     | Kurye müsait değil      | Başka kurye atayın               |
| `UNAUTHORIZED_ACCESS`       | Yetkisiz erişim         | İzinlerinizi kontrol edin        |
| `POD_REQUIRED`              | Teslimat kanıtı gerekli | Fotoğraf veya OTP sağlayın       |

---

## SSS

### Admin İçin

**S: Teslimat görevi neden otomatik atanamıyor?**  
C: Aşağıdaki durumları kontrol edin:

- Aktif kurye var mı?
- Kuryeler 10 km içinde mi?
- Kuryeler maksimum görev limitine ulaşmış mı?

**S: Kurye performansı nasıl değerlendiriliyor?**  
C: Üç faktör değerlendirilir:

1. Tamamlama oranı (başarılı teslimat %)
2. Ortalama teslimat süresi
3. Müşteri puanlamaları

**S: Teslim edilen bir görev iptal edilebilir mi?**  
C: Hayır, `Delivered` durumundaki görevler iptal edilemez. İade işlemi için ayrı süreç başlatılmalıdır.

### Kurye İçin

**S: Görev süresi dolarsa ne olur?**  
C: 60 saniye içinde kabul etmezseniz görev başka kuryeye atanır.

**S: İnternet kesilirse görev kaybedilir mi?**  
C: Hayır, aktif görev bilgileri cihazda saklanır. Bağlantı geldiğinde senkronize edilir.

**S: Teslimat fotoğrafı çekilmezse ne olur?**  
C: Fotoğraf veya OTP olmadan teslimat tamamlanamaz. Bu, teslimat kanıtı için zorunludur.

**S: Müşteri evde değilse ne yapmalıyım?**  
C:

1. Müşteriyi telefonla arayın (uygulama üzerinden)
2. 10 dakika bekleyin
3. Hala ulaşılamıyorsa "Teslimat Başarısız" seçin
4. Sebep olarak "Müşteri bulunamadı" seçin

---

## Teknik Gereksinimler

### Admin Paneli

- **Tarayıcılar:** Chrome 90+, Firefox 88+, Safari 14+, Edge 90+
- **Çözünürlük:** Minimum 1280x720

### Kurye Uygulaması

- **Android:** 8.0 (API 26) veya üzeri
- **iOS:** 13.0 veya üzeri
- **GPS:** Zorunlu
- **Kamera:** Teslimat kanıtı için zorunlu

---

## İletişim ve Destek

- **Teknik Destek:** destek@eticaret.com
- **Kurye Destek Hattı:** 0850 XXX XX XX
- **Çalışma Saatleri:** 09:00 - 22:00 (Her gün)

---

_Son Güncelleme: 2025_
