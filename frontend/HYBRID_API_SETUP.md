# 🎯 Hybrid API Mimarisi - Kurulum Tamamlandı

## 📋 Mimari Özet

Bu proje **hybrid API mimarisi** kullanmaktadır:

### ✅ Gerçek Backend API (Kalıcı)
- **Posterler/Bannerlar** → `/api/admin/banners`
- **Kategoriler** → `/api/admin/categories`
- **Kullanıcılar, Siparişler, vb.** → Mevcut backend

### 🔄 Geçici Mock API (Mikro API gelene kadar)
- **Ürünler** → JSON Server (`http://localhost:3005/products`)

---

## 📂 Servis Yapısı

### 1. API Client'lar

#### `apiBackend.js`
```javascript
// Gerçek backend API için
baseURL: https://localhost:7221 (veya REACT_APP_API_URL)
Kullanım: Posterler, Kategoriler
```

#### `apiProducts.js`
```javascript
// JSON Server için (GEÇİCİ)
baseURL: http://localhost:3005
Kullanım: Sadece Ürünler
```

### 2. Servisler

| Servis | Bağlandığı API | Durum |
|--------|----------------|-------|
| `posterService.js` | apiBackend → Backend API | ✅ Kalıcı |
| `categoryService.js` | apiBackend → Backend API | ✅ Kalıcı |
| `productServiceTemp.js` | apiProducts → JSON Server | 🔄 Geçici |

---

## 🚀 Çalıştırma

### 1. JSON Server'ı Başlat (Sadece Ürünler İçin)

```bash
cd frontend
node node_modules/json-server/lib/bin.js mock-db.json --port 3005
```

### 2. Backend API'yi Başlat

```bash
cd src/ECommerce.API
dotnet run
```

### 3. React Uygulamasını Başlat

```bash
cd frontend
npm start
```

---

## 🔧 Endpoint Yapısı

### Backend API Endpoints

#### Posterler/Bannerlar
```
GET    /api/banners/slider          # Public: Aktif slider'lar
GET    /api/banners/promo           # Public: Aktif promo'lar
GET    /api/admin/banners           # Admin: Tüm posterler
POST   /api/admin/banners           # Admin: Yeni poster
PUT    /api/admin/banners/{id}      # Admin: Poster güncelle
DELETE /api/admin/banners/{id}      # Admin: Poster sil
PATCH  /api/admin/banners/{id}/toggle # Admin: Aktif/Pasif
```

#### Kategoriler
```
GET    /api/categories              # Public: Tüm kategoriler
GET    /api/categories/{slug}       # Public: Slug'a göre
GET    /api/admin/categories        # Admin: Tüm kategoriler
POST   /api/admin/categories        # Admin: Yeni kategori
PUT    /api/admin/categories/{id}   # Admin: Kategori güncelle
DELETE /api/admin/categories/{id}   # Admin: Kategori sil
PATCH  /api/admin/categories/{id}/toggle # Admin: Aktif/Pasif
```

### JSON Server Endpoints (Geçici - Sadece Ürünler)

```
GET    /products                    # Tüm ürünler
GET    /products/{id}               # ID'ye göre ürün
POST   /products                    # Yeni ürün
PUT    /products/{id}               # Ürün güncelle
DELETE /products/{id}               # Ürün sil
PATCH  /products/{id}               # Kısmi güncelleme

# Filtreler
GET    /products?categoryId=1       # Kategoriye göre
GET    /products?isActive=true      # Aktif ürünler
GET    /products?q=searchterm       # Arama
```

---

## 🔄 Mikro API Geçişi (Gelecek)

Mikro API hazır olduğunda **sadece 1 dosya değişecek**:

### `productServiceTemp.js` → `productService.js`

```javascript
// ŞİMDİ (Geçici)
import apiProducts from "./apiProducts";  // JSON Server

// MİKRO API GELDİĞİNDE
import apiMikro from "./apiMikro";         // Mikro API
```

Endpoint path'lerini güncelle:
```javascript
// Şimdi
"/products" → await apiProducts.get("/products")

// Mikro API sonrası
"/api/v1/items" → await apiMikro.get("/api/v1/items")
```

**Başka hiçbir şey değişmeyecek!** ✨

---

## 📁 Dosya Konumları

```
frontend/
├── src/
│   ├── services/
│   │   ├── apiBackend.js           ✅ Gerçek backend client
│   │   ├── apiProducts.js          🔄 JSON Server client (geçici)
│   │   ├── posterService.js        ✅ Backend API'ye bağlı
│   │   ├── categoryService.js      ✅ Backend API'ye bağlı
│   │   └── productServiceTemp.js   🔄 JSON Server'a bağlı (geçici)
│   │
│   ├── pages/Admin/
│   │   ├── PosterManagement.jsx    ✅ Backend API kullanıyor
│   │   ├── AdminCategories.jsx     ✅ Backend API kullanıyor
│   │   └── AdminProducts.jsx       🔄 JSON Server kullanıyor
│   │
│   └── config/
│       └── apiConfig.js             ⚙️ API yapılandırması
│
├── mock-db.json                     📄 JSON Server veritabanı
├── mock-db.defaults.json            📄 Varsayılan veriler
└── scripts/
    └── reset-mock-db.js             🔄 DB sıfırlama scripti
```

---

## 🛠 Yardımcı Komutlar

### Mock DB'yi Sıfırla
```bash
cd frontend
node scripts/reset-mock-db.js
```

### JSON Server'ı Restart Et
```bash
# Ctrl+C ile durdur, sonra:
node node_modules/json-server/lib/bin.js mock-db.json --port 3005
```

---

## ⚠️ Önemli Notlar

1. **Posterler ve Kategoriler** → Backend API'nizi kullanır (gerçek DB)
2. **Ürünler** → Şimdilik JSON Server kullanır (geçici, dosya bazlı)
3. Admin panelde poster/kategori değişiklikleri **kalıcıdır** (gerçek DB'ye kaydedilir)
4. Admin panelde ürün değişiklikleri **mock-db.json**'a kaydedilir
5. Mikro API hazır olduğunda **sadece productServiceTemp.js** değişecek

---

## 🎉 Avantajları

✅ **Hybrid Mimari** - Gerçek ve mock API'ler bir arada  
✅ **Minimum Değişiklik** - Mikro API geldiğinde tek dosya güncellenecek  
✅ **Profesyonel Yapı** - Domain-based service separation  
✅ **Kalıcı Veri** - Posterler ve kategoriler gerçek DB'de  
✅ **Kolay Geçiş** - Migration süreci çok basit  

---

## 📞 Backend API Gereksinimleri

Backend'inizde şu endpoint'lerin olması gerekiyor:

### Posterler
- `GET /api/banners/slider`
- `GET /api/banners/promo`
- `GET /api/admin/banners`
- `POST /api/admin/banners`
- `PUT /api/admin/banners/{id}`
- `DELETE /api/admin/banners/{id}`
- `PATCH /api/admin/banners/{id}/toggle`

### Kategoriler
- `GET /api/categories`
- `GET /api/categories/{slug}`
- `GET /api/admin/categories`
- `POST /api/admin/categories`
- `PUT /api/admin/categories/{id}`
- `DELETE /api/admin/categories/{id}`
- `PATCH /api/admin/categories/{id}/toggle`

Bu endpoint'ler yoksa, backend'de eklemeniz gerekir!

---

**✨ Kurulum tamamlandı! Happy coding! 🚀**
