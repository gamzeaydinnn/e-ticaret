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

3. Docker ile lokal SQL Server çalıştırın:

```bash
cd /Users/dilarasara/e-ticaret   # proje kökü
docker compose up -d sqlserver
```

- Docker servisi: `ecommerce-sql`
- Port: `1433`
- Varsayılan kullanıcı: `sa`
- Varsayılan şifre: `EComLocal!12345` (hem `docker-compose.yml` hem de `appsettings*.json` içinde aynı olmalı)

4. Veritabanı ve ilk kurulum

- Backend ilk kez ayağa kalktığında:
  - SQL Server üzerinde `ECommerceDb` veritabanı otomatik oluşturulur.
  - EF Core migrations çalıştırılır.
  - `IdentitySeeder` ile admin kullanıcı ve roller oluşturulur.

Elle migration çalıştırmak isterseniz:

```bash
cd src/ECommerce.API
dotnet ef database update -p ../ECommerce.Data -s .
```

5. Backend'i çalıştırın:

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
