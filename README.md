# E-Ticaret Projesi

Modern ve profesyonel bir e-ticaret sistemi. Admin paneli, mikroservis entegrasyonu ve responsive frontend ile birlikte geliştirilmiştir.

## 🚀 Özellikler

### Frontend

- ✅ Modern React.js uygulaması
- ✅ Responsive design (Bootstrap)
- ✅ Professional orange theme
- ✅ Ürün kataloğu ve filtreleme
- ✅ Sepet yönetimi
- ✅ Sipariş takip sistemi
- ✅ Ödeme sayfası
- ✅ Admin paneli (Dashboard, Ürünler, Siparişler, Kullanıcılar)

### Backend

- ✅ ASP.NET Core Web API (.NET 9.0)
- ✅ Clean Architecture (Core, Business, Data, Infrastructure)
- ✅ Entity Framework Core (SQL Server)
- ✅ Repository Pattern & Dependency Injection
- ✅ Admin API endpoints
- ✅ Mikroservis entegrasyonu hazır
- ✅ JWT Authentication altyapısı

## 📁 Proje Yapısı

```
eticaret/
├── frontend/                    # React.js Frontend
│   ├── src/
│   │   ├── admin/              # Admin Panel Components
│   │   ├── components/         # UI Components
│   │   ├── services/           # API Services
│   │   └── hooks/              # Custom Hooks
│   └── public/
├── src/                        # Backend (.NET)
│   ├── ECommerce.API/          # Web API Layer
│   ├── ECommerce.Business/     # Business Logic Layer
│   ├── ECommerce.Core/         # Core Entities & DTOs
│   ├── ECommerce.Data/         # Data Access Layer
│   └── ECommerce.Infrastructure/ # External Services
└── README.md
```

## 🛠️ Teknolojiler

### Frontend

- React.js 18
- Bootstrap 5
- Axios (HTTP Client)
- React Hooks

### Backend

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core (SQL Server)
- Repository Pattern
- Dependency Injection

## ⚡ Kurulum ve Çalıştırma

### Ön Gereksinimler

- Node.js (v18+)
- .NET 9.0 SDK
- Git

### Backend Kurulumu

1. Repository'yi klonlayın:

```bash
git clone https://github.com/gamzeaydinnn/e-ticaret.git
cd e-ticaret
```

2. Backend bağımlılıklarını yükleyin:

```bash
cd src/ECommerce.API
dotnet restore
```

3. Veritabanı sağlayıcısı ve ilk kurulum

- Varsayılan: SQL Server. `src/ECommerce.API/appsettings.json` içinde:

```
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": { "DefaultConnection": "Server=localhost;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;" }
```

 

- İlk şema seçenekleri:
  - Migration ile:
    ```bash
    cd src/ECommerce.API
    dotnet ef migrations add InitialCreate -p ../ECommerce.Data -s .
    dotnet ef database update -p ../ECommerce.Data -s .
    ```
  - Geliştirme kolaylığı: Migration yoksa Development ortamında uygulama açılışında şema `EnsureCreated` ile oluşturulur.

4. Backend'i çalıştırın:

```bash
dotnet run
```

Backend: http://localhost:5153 adresinde çalışacak

### Frontend Kurulumu

1. Frontend klasörüne geçin:

```bash
cd frontend
```

2. Bağımlılıkları yükleyin:

```bash
npm install
```

3. Frontend'i çalıştırın:

```bash
npm start
```

Frontend: http://localhost:3000 adresinde çalışacak

## 👨‍💼 Admin Paneli

Admin paneline erişim için:

- URL: http://localhost:3000/admin
- Kullanıcı Adı: `admin`
- Şifre: `admin123`

### Admin Panel Özellikleri

- 📊 Dashboard (İstatistikler, grafikler)
- 📦 Ürün Yönetimi (CRUD, stok takibi)
- 🛒 Sipariş Yönetimi (durum güncelleme, detaylar)
- 👥 Kullanıcı Yönetimi (arama, filtreleme)

## 🔌 API Endpoints

### Genel API

- `GET /api/products` - Ürünleri listele
- `GET /api/products/{id}` - Ürün detayı
- `POST /api/orders` - Sipariş oluştur
- `GET /api/users/{id}/orders` - Kullanıcı siparişleri

### Admin API

- `GET /api/admin/dashboard/stats` - Dashboard istatistikleri
- `POST /api/admin/products` - Ürün oluştur
- `PUT /api/admin/products/{id}` - Ürün güncelle
- `DELETE /api/admin/products/{id}` - Ürün sil
- `GET /api/admin/orders` - Tüm siparişleri listele
- `PUT /api/admin/orders/{id}/status` - Sipariş durumu güncelle

## 🧪 Test

### Backend Test

```bash
cd src/ECommerce.Tests
dotnet test
```

### Frontend Test

```bash
cd frontend
npm test
```

## 📝 Geliştirme

### Branch Yapısı

- `main` - Production branch
- `feature/backend-entities-and-context` - Development branch

### Commit Mesaj Formatı

```
feat: yeni özellik ekleme
fix: hata düzeltme
docs: dokümantasyon güncelleme
style: kod formatı değişikliği
refactor: kod iyileştirme
test: test ekleme/güncelleme
```

## 🚀 Deployment

### Production Build

```bash
# Frontend build
cd frontend
npm run build

# Backend publish
cd src/ECommerce.API
dotnet publish -c Release
```

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişiklikleri commit edin (`git commit -m 'feat: Add amazing feature'`)
4. Branch'i push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## 📄 Lisans

Bu proje MIT lisansı altındadır.

## 📞 İletişim

- GitHub: [@gamzeaydinnn](https://github.com/gamzeaydinnn)
- Repository: [e-ticaret](https://github.com/gamzeaydinnn/e-ticaret)

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!
