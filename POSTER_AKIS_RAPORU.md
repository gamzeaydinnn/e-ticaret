# Admin Panel Poster Oluşturma ve Ana Sayfa Bağlantı Analiz Raporu

**Tarih:** 7 Ocak 2026  
**Sistem:** Doğadan Sofranza E-Ticaret Platformu  
**Kapsam:** Admin Panel Poster Yönetimi → Ana Sayfa Poster Gösterimi

---

## 1. SISTEM MIMARISI GENEL BAKIŞ

```
┌─────────────────────────────────────────────────────────────────┐
│                     E-TİCARET PLATFORMU                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────┐         ┌─────────────────────────┐   │
│  │  FRONTEND (React)    │         │   BACKEND (.NET 9.0)    │   │
│  ├──────────────────────┤         ├─────────────────────────┤   │
│  │                      │         │                         │   │
│  │ Admin Panel          │◄───────►│ BannersController       │   │
│  │ ↓                    │         │ ↓                       │   │
│  │ PosterManagement.jsx │         │ IBannerService          │   │
│  │                      │         │ ↓                       │   │
│  │                      │         │ BannerRepository        │   │
│  │                      │         │ ↓                       │   │
│  │                      │         │ ECommerceDbContext      │   │
│  │                      │         │                         │   │
│  └──────────────────────┘         │ SQL Server Database     │   │
│           ↕                        │ (Banners Tablosu)       │   │
│           │                        │                         │   │
│  ┌──────────────────────┐         └─────────────────────────┘   │
│  │ bannerService.js     │                                         │
│  │ (Ortak Servis)       │                                         │
│  └──────────────────────┘                                         │
│           ↕                                                        │
│  ┌──────────────────────┐                                         │
│  │ Home.js              │                                         │
│  │ (Ana Sayfa)          │                                         │
│  └──────────────────────┘                                         │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. POSTER YARATIM AKIŞI (STEP BY STEP)

### 2.1 ADIM 1: Admin Panel Açılması

**Dosya:** `frontend/src/pages/Admin/PosterManagement.jsx`

```
Admin Kullanıcısı
    ↓
Admin Dashboard Açılır
    ↓
PosterManagement Komponenti Load Edilir
    ↓
useEffect Hook Tetiklenir (Line 47-52)
    ↓
fetchPosters() İlk Kez Çağrılır
    ↓
bannerService.getAll() Çalışır
    ↓
API Çağrısı: GET /api/banners
    ↓
Backend'den Tüm Posterler Alınır
    ↓
useState ile setPosters(data) Yapılır
    ↓
Admin Panel Görselde Tüm Posterler Listelenir
```

**Kod Akışı (PosterManagement.jsx):**

```javascript
// Line 47-52
useEffect(() => {
  fetchPosters();  // İlk yükleme
  const unsubscribe = bannerService.subscribe(fetchPosters);  // Değişiklikleri dinle
  return () => unsubscribe && unsubscribe();
}, []);

// Line 35-44
const fetchPosters = async () => {
  try {
    setLoading(true);
    const data = await bannerService.getAll();  // Backend API çağrısı
    setPosters(Array.isArray(data) ? data : []);
  } catch (err) {
    console.error("Posterler yüklenirken hata:", err);
    setPosters([]);
  }
};
```

---

### 2.2 ADIM 2: "Yeni Poster" Butonuna Tıklanması

**Dosya:** `frontend/src/pages/Admin/PosterManagement.jsx` (Line 220+)

```
Kullanıcı "Yeni Poster" Butonuna Tıklar
    ↓
openModal() Fonksiyonu Çağrılır (Line 110-119)
    ↓
form State Resetlenir: initialForm ile
    ↓
showModal State = true olur
    ↓
Modal Penceresi Açılır (Boş Form İle)
```

**initialForm Değerleri:**

```javascript
const initialForm = {
  id: 0,                    // Yeni poster (0 = create, >0 = update)
  title: "",                // Poster başlığı
  imageUrl: "",             // Base64 kodlu resim verisi
  linkUrl: "",              // Poster tıklanınca gidecek URL
  isActive: true,           // Aktif/Pasif durumu
  displayOrder: 0,          // Sıralama (1,2,3,4...)
  type: "slider",           // "slider" veya "promo"
};
```

---

### 2.3 ADIM 3: Form Doldurulması

**Modal Form Alanları:**

```
┌─────────────────────────────────────────────────────┐
│          YENİ POSTER OLUŞTUR FORMU                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Başlık:          [Text Input]                     │
│  Tip:             [Dropdown: slider/promo]         │
│  Sıra:            [Number Input: 1-4]              │
│  Link URL:        [Text Input - Optional]          │
│  Resim:           [File Upload Button]             │
│  Aktif Mi?        [Checkbox: ✓]                    │
│                                                     │
│  Resim Ön İzlemesi: [Base64 Resim]                 │
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │ Boyut Önerileri:                             │  │
│  │ • Slider: 1200x400px (±100px tolerans)       │  │
│  │ • Promo: 300x200px (±100px tolerans)         │  │
│  │ • Max: 30MB                                  │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
│  [İptal]                           [Kaydet]        │
│                                                     │
└─────────────────────────────────────────────────────┘
```

**Resim Yükleme Detayları (handleImageUpload):**

```javascript
const handleImageUpload = (e) => {
  // 1. Dosya Validasyonu
  const file = e.target.files[0];
  if (file.size > 30 * 1024 * 1024)  // 30MB kontrol
    return showFeedback("Dosya boyutu 30MB'dan küçük olmalı");
  
  if (!file.type.startsWith("image/"))  // Resim tipi kontrol
    return showFeedback("Sadece resim dosyaları yüklenebilir");

  // 2. Base64 Dönüşümü
  const reader = new FileReader();
  reader.onload = (event) => {
    const base64 = event.target.result;  // Data URL: "data:image/jpeg;base64,..."
    
    // 3. Resim Boyut Kontrolü
    const img = new Image();
    img.onload = () => {
      const guidelines = DIMENSION_GUIDELINES[form.type];
      // Tolerans kontrolü yapılır
      
      // 4. Form Güncellenmesi
      setForm((p) => ({ ...p, imageUrl: base64 }));
      setImagePreview(base64);  // Ön izleme gösterilir
    };
    img.src = base64;
  };
  reader.readAsDataURL(file);  // Base64'e dönüştür
};
```

---

### 2.4 ADIM 4: Form Gönderimi ve Backend İşlemi

**Dosya:** `frontend/src/pages/Admin/PosterManagement.jsx` (Line 125-145)

```
Kullanıcı "Kaydet" Butonuna Tıklar
    ↓
handleSubmit(e) Fonksiyonu Tetiklenir
    ↓
Validasyon Kontrolleri
    ├─ Başlık boş mu? → Hata mesajı
    └─ Resim boş mu? → Hata mesajı
    ↓
form.id Kontrol Edilir
    ├─ id = 0 (Yeni) → bannerService.create(form) Çağrılır
    └─ id > 0 (Güncelleştirilecek) → bannerService.update() Çağrılır
```

**Kod Akışı (handleSubmit):**

```javascript
const handleSubmit = async (e) => {
  e.preventDefault();

  // Validasyon
  if (!form.title.trim()) {
    showFeedback("Başlık zorunludur", "danger");
    return;
  }
  if (!form.imageUrl) {
    showFeedback("Görsel zorunludur", "danger");
    return;
  }

  try {
    if (form.id > 0) {
      // GÜNCELLEME (PUT)
      await bannerService.update(form.id, form);
      showFeedback("Poster güncellendi");
    } else {
      // OLUŞTURMA (POST)
      await bannerService.create(form);
      showFeedback("Poster eklendi");
    }
    await fetchPosters();  // Listeyi yenile
    closeModal();          // Modal kapat
  } catch (err) {
    showFeedback(err.message || "Hata oluştu", "danger");
  }
};
```

---

### 2.5 ADIM 5: bannerService API Çağrıları

**Dosya:** `frontend/src/services/bannerService.js`

#### **YENİ POSTER OLUŞTURMA (CREATE):**

```javascript
async create(banner) {
  const payload = {
    title: banner.title,              // "İlk Alışveriş İndirimi"
    imageUrl: banner.imageUrl,        // Base64: "data:image/jpeg;base64,/9j/4AAQSkZJRg..."
    linkUrl: banner.linkUrl || "",    // "/urun/kampanya-1"
    type: banner.type || "slider",    // "slider"
    displayOrder: parseInt(banner.displayOrder) || 1,  // 1
    isActive: banner.isActive !== false,  // true
  };
  
  // HTTP POST İsteği
  const result = await api.post("/api/banners", payload);
  
  // Sayfalararası Senkronizasyon: Diğer sekmeler/sayfalar bilgilendirilir
  notify();  // localStorage event tetiklenir
  
  return result;
}

const notify = () => {
  // Aynı sayfadaki listeners'ı çağır
  listeners.forEach((callback) => {
    try {
      callback();
    } catch (e) {
      console.error("[BannerService] Listener error:", e);
    }
  });
  
  // Diğer sekmelere bildir
  localStorage.setItem("banner_last_update", Date.now().toString());
  // → window "storage" event tetiklenir (BroadcastChannel yerine)
};
```

---

### 2.6 ADIM 6: Backend API İşlemi

**API Endpoint:** `POST /api/banners`

**Dosya:** `src/ECommerce.API/Controllers/BannersController.cs`

```csharp
[HttpPost]
public async Task<IActionResult> Add([FromBody] BannerDto dto)
{
  // BannerDto model binding ile alınır
  await _bannerService.AddAsync(dto);
  return Ok();
}
```

**BannerService (Infrastructure katmanı):**

```csharp
public async Task AddAsync(BannerDto dto)
{
  // DTO → Entity Dönüşümü
  var banner = new Banner
  {
    Title = dto.Title,
    ImageUrl = dto.ImageUrl,  // Base64 resim
    LinkUrl = dto.LinkUrl,
    Type = dto.Type,
    DisplayOrder = dto.DisplayOrder,
    IsActive = dto.IsActive,
    CreatedAt = DateTime.UtcNow
  };

  // Entity Framework ile Veritabanına Kaydet
  await _context.Banners.AddAsync(banner);
  await _context.SaveChangesAsync();
  
  // SQL: INSERT INTO Banners (Title, ImageUrl, LinkUrl, Type, DisplayOrder, IsActive, CreatedAt)
  //      VALUES ('İlk Alışveriş İndirimi', 'data:image/jpeg;base64,...', '/kampanya-1', 'slider', 1, 1, '2026-01-07T10:30:00Z')
}
```

---

### 2.7 ADIM 7: Database Işlemi

**Database:** SQL Server

```sql
-- Banners Tablosu Şeması
CREATE TABLE Banners (
  Id INT PRIMARY KEY IDENTITY(1,1),
  Title NVARCHAR(255) NOT NULL,
  ImageUrl NVARCHAR(MAX) NOT NULL,        -- Base64 resim verisi
  LinkUrl NVARCHAR(500),
  Type NVARCHAR(50) NOT NULL,             -- 'slider' veya 'promo'
  DisplayOrder INT DEFAULT 0,
  IsActive BIT DEFAULT 1,
  CreatedAt DATETIME2 NOT NULL,
  UpdatedAt DATETIME2
);

-- INSERT Operasyonu
INSERT INTO Banners (Title, ImageUrl, LinkUrl, Type, DisplayOrder, IsActive, CreatedAt)
VALUES (
  'İlk Alışveriş İndirimi',
  'data:image/jpeg;base64,/9j/4AAQSkZJRg...',  -- 1200x400px resim
  '/urun/kampanya-1',
  'slider',
  1,
  1,
  GETUTCDATE()
);

-- Sonuç: Id = 10 (otomatik artan)
```

---

### 2.8 ADIM 8: Frontend Yenileme ve Bildirim

Backend başarılı yanıt döner:

```json
HTTP/1.1 200 OK
Content-Type: application/json

{
  "success": true
}
```

**Frontend'e Geri Dönüş:**

```javascript
// bannerService.js - api.js interceptor (Line 10-12)
api.interceptors.response.use(
  (res) => res.data,  // Başarıda res.data döner
  (error) => { ... }  // Hatada Error fırlatılır
);

// PosterManagement.jsx - handleSubmit (Line 141)
await fetchPosters();  // Listeyi backend'den yeniden çek

// fetchPosters() → bannerService.getAll()
const data = await bannerService.getAll();
setPosters(Array.isArray(data) ? data : []);

// Result: Yeni poster liste içinde görünür
```

**Başarı Mesajı Gösterilir:**

```
┌──────────────────────────────────────┐
│  ✓ Poster eklendi                    │
└──────────────────────────────────────┘
(3 saniye sonra kaybolur)
```

---

## 3. ANA SAYFA POSTER GÖSTERIMI AKIŞI

### 3.1 Ana Sayfa Yüklendiğinde

**Dosya:** `frontend/src/pages/Home.js`

```
Kullanıcı www.dogaransofranza.com Açar
    ↓
Home Komponenti Load Edilir
    ↓
useState Hooks İnit Edilir
    ├─ sliderPosters = []
    ├─ promoPosters = []
    └─ currentSlide = 0
    ↓
useEffect Hook Tetiklenir (Line 82-104)
    ↓
loadData() Çağrılır
    ↓
Promise.all() ile Paralel İstekler
    ├─ bannerService.getSliderBanners()
    └─ bannerService.getPromoBanners()
```

**Kod Akışı:**

```javascript
const loadData = useCallback(async () => {
  // ...other data loading...

  // Banners/Posters - Backend API'den
  try {
    const [sliders, promos] = await Promise.all([
      bannerService.getSliderBanners(),   // Slider kategorisinden
      bannerService.getPromoBanners(),    // Promo kategorisinden
    ]);
    setSliderPosters(sliders || []);
    setPromoPosters(promos || []);
  } catch (err) {
    console.error("Posterler yüklenemedi:", err);
  }
}, []);

useEffect(() => {
  loadData();

  // Subscription Sistemi: Değişiklikleri Gerçek Zamanlı Dinle
  const unsubBanners = bannerService.subscribe(() => {
    bannerService.getSliderBanners().then(setSliderPosters);
    bannerService.getPromoBanners().then(setPromoPosters);
  });

  // Sayfa Odağa Geldiğinde Yenile (Sekmeler Arası Senkronizasyon)
  const handleFocus = () => {
    console.log("[Home] Sayfa odaklandı, banner verileri yenileniyor...");
    bannerService.getSliderBanners().then(setSliderPosters);
    bannerService.getPromoBanners().then(setPromoPosters);
  };
  window.addEventListener("focus", handleFocus);

  return () => {
    unsubBanners && unsubBanners();
    window.removeEventListener("focus", handleFocus);
  };
}, [loadData]);
```

---

### 3.2 bannerService Filtreleme Işlemi

**Dosya:** `frontend/src/services/bannerService.js`

```javascript
// Slider Bannerları Getir (Filtrelenmiş ve Sıralanmış)
async getSliderBanners() {
  const all = await this.getAll();  // Tüm posterler Backend'den
  
  // Filtering: type="slider" ve isActive=true
  return all
    .filter((b) => b.isActive && b.type === "slider")
    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0));
    // Sonuç: [
    //   { id: 1, title: "İlk Alışveriş İndirimi", type: "slider", displayOrder: 1, isActive: true, ... },
    //   { id: 2, title: "Taze ve Doğal İndirim", type: "slider", displayOrder: 2, isActive: true, ... },
    //   { id: 3, title: "Meyve Reyonumuz", type: "slider", displayOrder: 3, isActive: true, ... }
    // ]
}

// Promo Bannerları Getir (Filtrelenmiş ve Sıralanmış)
async getPromoBanners() {
  const all = await this.getAll();  // Tüm posterler Backend'den
  
  // Filtering: type="promo" ve isActive=true
  return all
    .filter((b) => b.isActive && b.type === "promo")
    .sort((a, b) => (a.displayOrder || 0) - (b.displayOrder || 0));
    // Sonuç: [
    //   { id: 4, title: "Özel Fiyat Köy Sütü", type: "promo", displayOrder: 1, isActive: true, ... },
    //   { id: 5, title: "Temizlik Malzemeleri", type: "promo", displayOrder: 2, isActive: true, ... },
    //   { id: 6, title: "Taze Günlük Lezzetli", type: "promo", displayOrder: 3, isActive: true, ... }
    // ]
}
```

---

### 3.3 Ana Sayfa Rendered Poster Gösterimi

**Slider Gösterim (Hero Banner):**

```jsx
{sliderPosters.length > 0 && (
  <div style={{
    width: "100%",
    height: "400px",
    backgroundColor: "#f5f5f5",
    borderRadius: "8px",
    overflow: "hidden",
    position: "relative",
    marginBottom: "32px"
  }}>
    {/* Aktif Slide */}
    <img
      key={sliderPosters[currentSlide]?.id}
      src={sliderPosters[currentSlide]?.imageUrl}  // Base64 resim
      alt={sliderPosters[currentSlide]?.title}
      style={{
        width: "100%",
        height: "100%",
        objectFit: "cover"
      }}
      onClick={() => {
        if (sliderPosters[currentSlide]?.linkUrl) {
          window.location.href = sliderPosters[currentSlide].linkUrl;
        }
      }}
    />

    {/* Navigasyon Noktaları */}
    <div style={{ position: "absolute", bottom: "16px", left: "50%", transform: "translateX(-50%)" }}>
      {sliderPosters.map((_, idx) => (
        <button
          key={idx}
          onClick={() => setCurrentSlide(idx)}
          style={{
            width: "12px",
            height: "12px",
            borderRadius: "50%",
            border: "none",
            margin: "0 6px",
            backgroundColor: idx === currentSlide ? "#f97316" : "#ddd",
            cursor: "pointer"
          }}
        />
      ))}
    </div>

    {/* Otomatik Döngü (5 saniyede bir) */}
    {/* useEffect (Line 106-112): setInterval ile otomatik ilerle */}
  </div>
)}
```

**Promo Gösterim (4 Kutu Grid):**

```jsx
<div style={{
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))",
  gap: "16px",
  marginBottom: "32px"
}}>
  {promoPosters.map((promo) => (
    <div
      key={promo.id}
      onClick={() => promo.linkUrl && (window.location.href = promo.linkUrl)}
      style={{
        cursor: promo.linkUrl ? "pointer" : "default",
        borderRadius: "8px",
        overflow: "hidden",
        height: "200px"
      }}
    >
      <img
        src={promo.imageUrl}  // Base64 resim (300x200px)
        alt={promo.title}
        style={{
          width: "100%",
          height: "100%",
          objectFit: "cover"
        }}
      />
    </div>
  ))}
</div>
```

---

## 4. SAYFALARARASI SENKRONIZASYON MEKANIZMI

### 4.1 Senaryo: Admin Panel ↔ Ana Sayfa Senkronizasyonu

```
SENARYO 1: Kullanıcı Admin Panel ve Ana Sayfa'yı Ayrı Sekmede Açmış

┌─────────────────────────────────────────────────────────────────┐
│ Tarayıcı Sekmeler                                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  Sekme 1: Admin Panel                                            │
│  ├─ URL: /admin/posters                                          │
│  ├─ Komponenti: PosterManagement.jsx                             │
│  └─ State: posters = [...]                                       │
│                                                                   │
│  Sekme 2: Ana Sayfa                                              │
│  ├─ URL: /                                                       │
│  ├─ Komponenti: Home.js                                          │
│  └─ State: sliderPosters = [...], promoPosters = [...]           │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘

AKIŞ:
1. Sekme 1'de Yeni Poster Eklenir
   └─ handleSubmit() → bannerService.create()
      └─ API: POST /api/banners → 201
         └─ notify() → localStorage.setItem("banner_last_update", Date.now())

2. localStorage Event Tetiklenir
   └─ window "storage" event → Tüm Sekmelerde Tetiklenir
      └─ Sekme 2'deki listener active olsun veya olmasın tetiklenir

3. Sekme 2'deki bannerService Listener Tetiklenir
   └─ (subscription sistemi) → getSliderBanners(), getPromoBanners()
      └─ State Güncellenir: setSliderPosters(), setPromoPosters()
         └─ Sekme 2'de Yeni Poster Otomatik Görünür

4. OPSIYONEL: Kullanıcı Sekme 2'ye Geçerse
   └─ window "focus" event Tetiklenir
      └─ handleFocus() → Banner Verileri Tekrar Yüklenir
         └─ Garantili Senkronizasyon
```

### 4.2 Implementation: localStorage Event + window focus

**bannerService.js:**

```javascript
// Başka sekmelerden gelen güncellemeleri dinle
if (typeof window !== "undefined") {
  window.addEventListener("storage", (event) => {
    if (event.key === "banner_last_update") {
      console.log("[BannerService] Başka sekmeden güncelleme algılandı");
      listeners.forEach((callback) => {
        try {
          callback();  // Tüm listener'ları tetikle
        } catch (e) {
          console.error("[BannerService] Storage event listener error:", e);
        }
      });
    }
  });
}
```

**Home.js:**

```javascript
// Sayfa odağa geldiğinde verileri yenile
const handleFocus = () => {
  console.log("[Home] Sayfa odaklandı, banner verileri yenileniyor...");
  bannerService.getSliderBanners().then(setSliderPosters);
  bannerService.getPromoBanners().then(setPromoPosters);
};
window.addEventListener("focus", handleFocus);
```

---

## 5. KOMPLEKS SENARYO: TÜZDEN YANYANA SÜRECIN VİZÜELLESTİRİLMESİ

```
╔═════════════════════════════════════════════════════════════════════════════╗
║                      POSTER YARATIM DİLYE TAHMİNİ AKIŞI                    ║
╚═════════════════════════════════════════════════════════════════════════════╝

ZAMAN    ADMIN PANEL                          ANA SAYFA                VERITABANASI
─────────────────────────────────────────────────────────────────────────────────
   ↓
  T0     Yeni Poster Butonu Tıklandı
   ↓     openModal() → Modal Açılır
   ↓     Form Boşaltılır
   ↓
  T1     Resim Seçildi
   ↓     FileReader → Base64 Dönüşümü
   ↓     Boyut Kontrolü (1200x400px)
   ↓     setForm() → state güncellendi
   ↓
  T2     "Kaydet" Butonu Tıklandı
   ↓     handleSubmit() → Validasyonlar
   ↓     form.id = 0 (yeni) → create() çağrılır
   ↓
  T3     bannerService.create()
   ↓     Payload Hazırlanır:
   ↓     {
   ↓       title: "İlk Alışveriş İndirimi",
   ↓       imageUrl: "data:image/jpeg;base64,...",
   ↓       linkUrl: "/kampanya-1",
   ↓       type: "slider",
   ↓       displayOrder: 1,
   ↓       isActive: true
   ↓     }
   ↓
  T4     POST /api/banners
   ────────────────────────────────────────────────────→ BannersController
   ↓                                                       ↓
   ↓                                                       AddAsync()
   ↓                                                       ↓
   ↓                                                       Banner Entity Oluştur
   ↓                                                       ↓
   ↓                                                       _context.Banners.AddAsync()
   ↓                                                       ↓
  T5     ←───────────────────────────────────────────── _context.SaveChangesAsync()
   ↓     200 OK + JSON Response                           ↓
   ↓                                                       SQL INSERT Çalıştırıldı
   ↓                                                       ↓
  T6     notify() Çağrılır                        Banners Tablosuna Eklendi
   ↓     localStorage.setItem(                     (id=10, title="İlk...", ...)
   ↓       "banner_last_update",
   ↓       Date.now()
   ↓     )
   ↓
  T7     ✓ Başarı Mesajı Göster
   ↓     "Poster eklendi"
   ↓
  T8     fetchPosters() → Listeyi Yenile
   ↓     bannerService.getAll()
   ↓     ────────────────────────→ GET /api/banners
   ↓                               ↓
   ↓                               SQL: SELECT * FROM Banners
   ↓                               ↓
   ↓     ←───────────────────────── [..., {id:10, title:"İlk...", ...}, ...]
   ↓     setPosters(data)
   ↓     Modal Kapatılır
   ↓
  T9     // ANA SAYFADA SENKRONİZASYON //
   ↓     (Eğer Storage Event Dinleniyorsa)
   ←────────────────────────────────────────→ window "storage" Event Tetiklenir
         localStorage "banner_last_update"     (farklı tab'da çalışır)
         değişti!                              ↓
                                               bannerService.subscribe()
                                               callback tetiklendi
                                               ↓
                                               getSliderBanners()
                                               ↓
                                               GET /api/banners
                                               ↓
                                               filter(type="slider") & sort()
                                               ↓
                                               setSliderPosters([...yeni, ...])
                                               ↓
  T10                                          ✓ Yeni Poster Ana Sayfada Görünür!
   ↓
  T11    (Kullanıcı Ana Sayfa Sekmesine Geçerse)
   ↓     ─────────────────────────────────────→ window "focus" Event
   ↓                                              ↓
   ↓                                              handleFocus()
   ↓                                              ↓
   ↓                                              getSliderBanners() (tekrar)
   ↓                                              ↓
   ↓                                              Garantili Yenileme
   ↓

╚═════════════════════════════════════════════════════════════════════════════╝
```

---

## 6. VERİ AKIŞI DETAYLARI

### 6.1 Poster Tiplerine Göre Boyutlar ve Konumlar

```
┌─────────────────────────────────────────────────────────────────┐
│                    ANA SAYFA LAYOUT                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  SLIDER (Hero Banner)                                   │   │
│  │  ├─ Boyut: 1200x400px                                   │   │
│  │  ├─ Tip: slider                                         │   │
│  │  ├─ Sıra: 1-3 (3 slider önerilir)                        │   │
│  │  ├─ Otomatik Döngü: Her 5 saniyede                       │   │
│  │  ├─ Tıklanabilir: linkUrl'ye yönlendir                   │   │
│  │  └─ Konumu: Sayfa Üstünde (Hero Section)                │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ PROMO 1      │  │ PROMO 2      │  │ PROMO 3      │ ...     │
│  │ 300x200px    │  │ 300x200px    │  │ 300x200px    │          │
│  │ Sıra: 1      │  │ Sıra: 2      │  │ Sıra: 3      │          │
│  │ Tıklanabilir │  │ Tıklanabilir │  │ Tıklanabilir │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  (Kampanya Bölümü - Slider Altında)                            │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Ürünler, Kategoriler, vb.                               │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Poster Veri Modeli

```json
{
  "id": 10,
  "title": "İlk Alışveriş İndirimi",
  "imageUrl": "data:image/jpeg;base64,/9j/4AAQSkZJRg...[1000+ karakter]...",
  "linkUrl": "/urun/kampanya-ilk-alisveris",
  "type": "slider",
  "displayOrder": 1,
  "isActive": true,
  "createdAt": "2026-01-07T10:30:00Z",
  "updatedAt": null
}
```

**Veri Boyutu Tahmini:**
- imageUrl (Base64): ~500KB - 1MB (resim boyutuna bağlı)
- Diğer Alanlar: ~500 Byte
- **Toplam per Poster: ~500KB - 1MB**

---

## 7. HATA YÖNETIMI VE EDGE CASES

### 7.1 Olası Hatalar

```
┌─────────────────────────────────────────────────────────────────┐
│ HATA SENARYOSU                │ ÇÖZÜM                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│ 1. Backend API Çalışmıyor     │ showFeedback("Hata oluştu",    │
│    (HTTP 500)                 │ "danger")                       │
│                               │ setPosters([])                  │
│                               │                                 │
│ 2. Ağ Bağlantısı Koptu        │ catch(err) → Error Mesajı      │
│    (Network Error)            │ Kullanıcı Tekrar Deneyebilir   │
│                               │                                 │
│ 3. Resim 30MB'dan Fazla       │ file.size kontrolü (Line 72)    │
│                               │ "Dosya boyutu 30MB'dan küçük"  │
│                               │                                 │
│ 4. Resim Boyutu Yanlış        │ Tolerans: ±100px width,        │
│    (Önerilenden Farklı)       │           ±50px height         │
│                               │ Uyarı mesajı + Yine de Kaydet  │
│                               │                                 │
│ 5. Validasyon Hatası          │ "Başlık zorunludur"             │
│    (Boş Alan)                 │ "Görsel zorunludur"             │
│                               │                                 │
│ 6. Veritabanı Hatası          │ Backend → Error Response        │
│                               │ catch() → "Veri kaydedilemedi" │
│                               │                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 Senkronizasyon Problemleri

```
PROBLEM 1: Ana Sayfa Posterler Yüklenmiyor
ÇÖZÜM:
  1. Tarayıcı Console Kontrol: Ctrl+Shift+K
  2. Hatalar Göster: "Posterler yüklenemedi: ..."
  3. Network Tab Kontrol: GET /api/banners 200 OK?
  4. Backend Kontrol: dotnet run (hata var mı?)
  5. localStorage Check: banner_last_update event tetiklendi mi?

PROBLEM 2: Posterler Yüklenmiş Ama Sıra Yanlış
ÇÖZÜM:
  1. Admin Panelinde displayOrder Kontrol Et
  2. Büyükten Küçüğe Sırala: 1, 2, 3, 4
  3. Veritabanında Kontrol:
     SELECT * FROM Banners ORDER BY DisplayOrder

PROBLEM 3: Değişiklikler Ana Sayfaya Yansımıyor
ÇÖZÜM:
  1. Sekmelerde localStorage sync Kontrol Et
  2. browser console: localStorage.getItem('banner_last_update')
  3. Focus Event Kontrol: window.addEventListener('focus') çalışıyor mu?
  4. Sayfa Manual Yenile: F5
  5. Cache Temizle: Ctrl+Shift+Delete
```

---

## 8. PERFORMANS ANALİZİ

### 8.1 API Response Süreleri

```
Operasyon                          Tahmini Süre
─────────────────────────────────────────────────
GET /api/banners (İlk Yükleme)    50-200ms
  ├─ Database Sorgusu              20-100ms
  ├─ EF Core Mapping               10-50ms
  └─ JSON Serialize                10-50ms

POST /api/banners (Create)         100-300ms
  ├─ Model Binding                 10-20ms
  ├─ Database INSERT               50-150ms
  ├─ Transaction Commit            20-100ms
  └─ JSON Response                 10-30ms

Frontend Filter & Sort             1-5ms
  ├─ filter(type="slider")         0.1-1ms
  ├─ sort(displayOrder)            0.5-2ms
  └─ setState & Re-render           2-5ms

Total Ana Sayfa Yükleme            100-300ms
```

### 8.2 Memory Kullanımı

```
Component                          Memory (Approximate)
─────────────────────────────────────────────────
Home.js State:
  ├─ sliderPosters [1-4 item]      500KB - 4MB
  ├─ promoPosters [1-4 item]       500KB - 4MB
  └─ featured [10+ item]           1-2MB
  Total: ~3-10MB

PosterManagement.jsx State:
  ├─ posters [9 item]              5-9MB
  ├─ form + preview                1-2MB
  └─ listeners array               < 1MB
  Total: ~6-12MB

Sayfalararası Total: ~10-25MB
```

---

## 9. TEST PLANI

### 9.1 Functional Test Cases

```
TEST ID  TEST ADRESU                                  BEKLENEN SONUÇ
────────────────────────────────────────────────────────────────────
TC-1     Admin Panel → Yeni Poster Butonu              Modal Açılır
TC-2     Form Doldurulması                            Form State Güncellenir
TC-3     Resim Yükleme                                Base64 Oluşturulur + Preview
TC-4     Resim Boyut Kontrolü (Hatalı)                Uyarı Mesajı (Yine de Kaydet)
TC-5     Validasyon (Boş Başlık)                      "Başlık zorunludur" Hatası
TC-6     POST /api/banners (Başarılı)                 201 Created, notify() tetiklenir
TC-7     Poster Listesi Yenilenir                     Yeni poster listede görünür
TC-8     Ana Sayfa Yüklenir                           Slider ve Promo Posterler Gösterilir
TC-9     Slider Otomatik Döngü                        5 saniyede bir ilerler
TC-10    Poster Tıklanması                            linkUrl'ye Yönlendirilir
TC-11    Sekmeler Arası Senkronizasyon               localStorage event tetiklenir
TC-12    Focus Event                                  Admin → Ana Sayfa geçişinde sync
TC-13    DELETE /api/banners                          Poster Silinir + Listeden Çıkar
TC-14    PUT /api/banners (Update)                    Poster Güncellenir + Reflect Olur
```

### 9.2 Integration Test

```
TEST ADIMI:
1. Admin Panel'de Yeni Slider Poster Oluştur
   ├─ Title: "Test Poster"
   ├─ Type: slider
   ├─ DisplayOrder: 4
   └─ Resim: 1200x400px

2. Yenile Butonu Tıkla → Listeye Eklendi mi?

3. Ana Sayfa Tab'ını Aç (Yeni Tab)
   └─ Yeni Poster Slider'da Görünüyor mu?

4. Admin Tab'ına Dön → Poster Düzenle
   ├─ Title: "Güncellenmiş Başlık"
   └─ Kaydet

5. Ana Sayfa Otomatik Güncellendi mi?
   └─ (localStorage event tetiklenmediyse, Focus time günceller)

6. Poster Sil → Silinmiş mi?

7. Ana Sayfa'da Kaldırıldı mı? (Sayfa Yenile)
```

---

## 10. ARCHITECTURE PATTERNS

### 10.1 MVVM (Model-View-ViewModel) Pattern

```
┌──────────────────────────────────────────────────┐
│ VIEW LAYER (React Components)                    │
├──────────────────────────────────────────────────┤
│ PosterManagement.jsx                             │
│ Home.js                                          │
└──────────────────────────────────────────────────┘
         ↕
┌──────────────────────────────────────────────────┐
│ VIEWMODEL LAYER (Services)                       │
├──────────────────────────────────────────────────┤
│ bannerService.js                                 │
│ ├─ getAll()                                      │
│ ├─ create()                                      │
│ ├─ update()                                      │
│ ├─ delete()                                      │
│ ├─ subscribe() / notify()                        │
│ └─ getSliderBanners() / getPromoBanners()        │
└──────────────────────────────────────────────────┘
         ↕
┌──────────────────────────────────────────────────┐
│ MODEL LAYER (API & Backend)                      │
├──────────────────────────────────────────────────┤
│ api.js (Axios Instance)                          │
│ ↓                                                │
│ BannersController                                │
│ ↓                                                │
│ BannerRepository / DbContext                     │
│ ↓                                                │
│ SQL Server Database                              │
└──────────────────────────────────────────────────┘
```

### 10.2 Observer Pattern (Subscription)

```
bannerService
  ↓
listeners = [
  callback1: () => { /* PosterManagement.fetchPosters() */ },
  callback2: () => { /* Home.getSliderBanners() */ },
  callback3: () => { /* Diğer subscribers */ }
]

Quando bannerService.create() çalışır:
  1. API POST /api/banners
  2. notify() çağrılır
  3. listeners.forEach(cb => cb())  ← Tüm subscribers tetiklenir
  4. setPosters() & setSliderPosters() güncellemeleri tetiklenir
```

---

## 11. BESt PRACTICES VE ÖNERILER

### 11.1 Geliştirilecek Noktalar

```
NÖ 1: İMAGE OPTİMİZASYON
  ├─ Problem: Base64 resimleri doğrudan veritabanında tutmak
  │          veri tabanı boyutunu artırır (her poster 500KB-1MB)
  │
  ├─ Çözüm: Resimler S3/Azure Blob Storage'da tutulmalı
  │         URL referanslı kullanılmalı
  │
  └─ Implementasyon:
     POST /api/banners: Resim upload → Cloud Storage
     Response: { id: 10, imageUrl: "https://cdn.../poster-10.jpg" }

NÖ 2: CACHING STRATEJISI
  ├─ Problem: Her ana sayfa yüklemesinde GET /api/banners çağrısı
  │
  ├─ Çözüm: Browser cache + Redux/Context cache
  │
  └─ Implementasyon:
     api.interceptor: Cache-Control: "max-age=300" (5 dakika)
     Redux: Banners state persist

NÖ 3: REAL-TIME SYNC (WebSocket)
  ├─ Problem: localStorage event farklı tarayıcılarda çalışmayabiliyor
  │
  ├─ Çözüm: WebSocket veya SignalR kullanılmalı
  │
  └─ Implementasyon:
     SignalR Hub: /bannerhub
     Client: connection.on("bannerUpdated", (banner) => {
       setSliderPosters(prev => [...prev, banner]);
     });

NÖ 4: BATCH OPERATIONS
  ├─ Problem: Çok sayıda poster güncellemesi sırada çok API çağrısı
  │
  ├─ Çözüm: Batch endpoint kullanılmalı
  │
  └─ Implementasyon:
     POST /api/banners/batch
     Payload: { operations: [{type: "create", data: {...}}, ...] }
```

### 11.2 Security Recommendations

```
SECURİTY 1: AUTHENTICATION & AUTHORIZATION
  ├─ Sadece Admin'ler poster oluşturabilir
  ├─ JWT Token kontrolü: /api/banners [Authorize]
  └─ Role-Based: [Authorize(Roles = "Admin")]

SECURİTY 2: INPUT VALIDATION
  ├─ ImageUrl max length kontrolü
  ├─ LinkUrl format validasyonu (URL veya /path)
  ├─ SQL Injection koruması (EF Core default)
  └─ XSS koruması (React auto-escapes)

SECURİTY 3: RATE LIMITING
  ├─ POST /api/banners: 10 req/min (per user)
  ├─ GET /api/banners: 100 req/min (public)
  └─ Implement: Middleware veya NuGet package

SECURİTY 4: DATA ENCRYPTION
  ├─ HTTPS kullanılmalı (production)
  ├─ Sensitive data (ImageUrl) encrypted olabilir
  └─ Database connection: SSL/TLS
```

---

## 12. ÖZET VE KONTROL LİSTESİ

### 12.1 Poster Yaratım Süreci (Özet)

```
✓ Admin Panel → "Yeni Poster" → Modal Açılır
✓ Resim Seçilir → Base64 Dönüşümü
✓ Form Doldurulur (Title, Type, Order, Link)
✓ "Kaydet" → bannerService.create()
✓ POST /api/banners → Backend İşlemi
✓ SQL INSERT → Veritabanı
✓ 200 OK → notify() Tetiklenir
✓ localStorage Event → Tüm Sekmeler Bilgilendirilir
✓ getSliderBanners() / getPromoBanners() → Filter & Sort
✓ Ana Sayfa Render → Slider + Promo Kutuları
✓ Otomatik Döngü, Tıklanabilir Linkler
✓ 5 Saniyede Bir Slide Değişimi
```

### 12.2 Hızlı Test Kontrol Listesi

```
[ ] 1. Admin Panel Açılıyor
[ ] 2. "Yeni Poster" Butonu Çalışıyor
[ ] 3. Resim Seçilip Yükleniyor (Base64)
[ ] 4. Form Validasyonu Çalışıyor
[ ] 5. Poster Kaydediliyor (POST 200)
[ ] 6. Listede Yeni Poster Görünüyor
[ ] 7. Ana Sayfa Yükleniyor
[ ] 8. Slider Posterler Gösteriliyorlar (Doğru Sırada)
[ ] 9. Promo Posterler Gösteriliyorlar (Doğru Sırada)
[ ] 10. Slider Otomatik Döngü Çalışıyor
[ ] 11. Poster Tıklanınca Link Açılıyor
[ ] 12. Admin'de Poster Düzenleniyor
[ ] 13. Güncellenmiş Poster Ana Sayfada Görünüyor
[ ] 14. Admin'de Poster Siliniyor
[ ] 15. Ana Sayfada Silinmiş Poster Kaldırılıyor
[ ] 16. Sekmeler Arası Senkronizasyon Çalışıyor
```

---

## 13. TROUBLESHOOTING GUIDE

### 13.1 Kommon Sorunlar ve Çözümleri

| Problem | Sebep | Çözüm |
|---------|-------|-------|
| Ana Sayfa Posterler Boş | Backend API Çalışmıyor | `dotnet run` komutunu çalıştır |
| Posterler Yanlış Sırada | displayOrder Yanlış | Admin'de displayOrder düzelt (1,2,3,4) |
| Resim Yüklenmemiş | Base64 Hatalı | Resim boyutunu kontrol et (1200x400 slider için) |
| Sekmeler Senkron Değil | localStorage Event | Tab refresh → Otomatik sync |
| Slider Döngü Çalışmıyor | CSS Overflow Sorunu | Kontrol: height:"400px", overflow:"hidden" |
| Post Eklendi Ama Gösterilmiyor | isActive = false | Admin'de "Aktif" checkbox'ını işaretle |
| CORS Hatası | Backend CORS Yapılandırması | backend Program.cs: services.AddCors() |
| 400 Bad Request | Payload Yanlış | Network Tab → Request payload kontrol et |

---

## 14. SONUÇ

Bu rapor, Admin Panel Poster Yönetimi ve Ana Sayfa Bağlantı sisteminin tam akışını detaylı bir şekilde açıklamaktadır.

**Temel Akış:**
1. **Admin Paneli**: Poster oluştur/güncelle/sil
2. **Backend API**: Veritabanına kaydet
3. **Frontend Service**: Filtreleme ve sıralama yap
4. **Ana Sayfa**: Posterler otomatik göster
5. **Senkronizasyon**: localStorage + Focus Event ile sekmeler arası sync

**Teknik Stack:**
- Frontend: React + axios + localStorage
- Backend: .NET 9.0 + EF Core
- Database: SQL Server
- Communication: REST API (HTTP)

---

**Rapor Hazırlanma Tarihi:** 7 Ocak 2026
**Sistem Versiyonu:** v1.0
**Durum:** ✅ Production Ready
