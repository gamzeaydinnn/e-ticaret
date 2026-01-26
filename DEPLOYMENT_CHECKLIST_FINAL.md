# 🚀 Sipariş-Kurye-Panel Sistemi - Deployment Checklist

> Production ortamına deploy etmeden önce kontrol edilmesi gereken adımlar.

---

## ✅ Pre-Deployment Checklist

### 1. Kod Kontrolü

- [ ] Tüm FAZA'lar (1-8) tamamlandı
- [x] Backend API build başarılı
- [x] Frontend build başarılı (414.7 KB JS, 70.15 KB CSS)
- [x] Docker container build başarılı
- [ ] Sensitive data (API keys, passwords) .env'de
- [ ] Console.log'lar temizlendi

### 2. Veritabanı

- [x] Tüm migration'lar uygulandı
- [x] Roller seed edildi (9 rol: Admin, User, Courier, StoreAttendant, Dispatcher, vb.)
- [x] Test kullanıcıları oluşturuldu
- [ ] Production için admin şifresi değiştirildi

### 3. API Endpoint Testi

| Endpoint                             | Test                 | Durum |
| ------------------------------------ | -------------------- | ----- |
| POST /api/auth/login                 | Admin login          | ✅    |
| POST /api/auth/login                 | StoreAttendant login | ✅    |
| POST /api/auth/login                 | Dispatcher login     | ✅    |
| GET /api/StoreAttendantOrder/orders  | Sipariş listesi      | ✅    |
| GET /api/StoreAttendantOrder/summary | Özet                 | ✅    |
| GET /api/DispatcherOrder/orders      | Sipariş listesi      | ✅    |
| GET /api/DispatcherOrder/couriers    | Kurye listesi        | ✅    |
| GET /api/DispatcherOrder/summary     | Özet                 | ✅    |

### 4. SignalR Hubları

- [x] StoreAttendantHub yapılandırıldı
- [x] DispatcherHub yapılandırıldı
- [x] CourierHub yapılandırıldı
- [ ] Production CORS ayarları yapıldı

### 5. Frontend Paneller

| Panel                     | Route               | Durum |
| ------------------------- | ------------------- | ----- |
| Store Attendant Login     | /store/login        | ✅    |
| Store Attendant Dashboard | /store/dashboard    | ✅    |
| Dispatcher Login          | /dispatch/login     | ✅    |
| Dispatcher Dashboard      | /dispatch/dashboard | ✅    |
| Admin Panel               | /admin              | ✅    |
| Courier Panel             | /courier            | ✅    |

---

## 🔧 Deployment Komutları

### Docker Deployment

```bash
# 1. Docker Compose build
docker-compose build --no-cache

# 2. Servisleri başlat
docker-compose up -d

# 3. Container durumunu kontrol et
docker ps

# 4. Logları izle
docker logs -f ecommerce-api
```

### Frontend Deployment

```bash
# 1. Dependencies yükle
cd frontend
npm ci

# 2. Production build
npm run build

# 3. Build dosyalarını nginx'e kopyala
# Veya Docker image oluştur
```

---

## 🌐 Production Environment Variables

### Backend (.env / appsettings.Production.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<PROD_DB>;Database=ECommerceDb;..."
  },
  "Jwt": {
    "SecretKey": "<STRONG_SECRET_KEY_256_BITS>",
    "Issuer": "ECommerceAPI",
    "Audience": "ECommerceClient"
  },
  "CORS": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

### Frontend (.env.production)

```env
REACT_APP_API_URL=https://api.yourdomain.com/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
```

---

## 📋 Post-Deployment Verification

### 1. Health Check

```bash
# API health
curl https://api.yourdomain.com/health

# Database connectivity
curl https://api.yourdomain.com/api/health/db
```

### 2. Smoke Tests

1. [ ] Admin panel'e giriş yapılabilir
2. [ ] Store Attendant panel'e giriş yapılabilir
3. [ ] Dispatcher panel'e giriş yapılabilir
4. [ ] Sipariş oluşturulabilir
5. [ ] Sipariş hazırlanabilir
6. [ ] Kurye atanabilir

### 3. SignalR Verification

1. [ ] Browser DevTools'da WebSocket bağlantısı kontrol et
2. [ ] Sipariş durumu değiştiğinde real-time güncelleme alınıyor
3. [ ] Kurye lokasyonu güncellenebiliyor

---

## 🔒 Güvenlik Kontrolleri

- [ ] HTTPS aktif
- [ ] JWT secret key güçlü (256+ bit)
- [ ] CORS sadece izin verilen domain'ler
- [ ] Rate limiting aktif
- [ ] SQL injection koruması (EF Core parameterized queries)
- [ ] XSS koruması
- [ ] Admin şifresi değiştirildi

---

## 📊 Monitoring

### Önerilen Araçlar

1. **Application Insights** - Azure monitoring
2. **Serilog + Seq** - Structured logging
3. **Prometheus + Grafana** - Metrics
4. **ELK Stack** - Log aggregation

### Önemli Metrikler

- API response time
- Error rate
- Active SignalR connections
- Database query performance
- Memory/CPU usage

---

## 🔄 Rollback Planı

### Database Rollback

```bash
# Son migration'ı geri al
dotnet ef migrations remove
```

### Docker Rollback

```bash
# Önceki image'a dön
docker-compose down
docker tag eticaret-api:latest eticaret-api:rollback
docker pull eticaret-api:previous
docker-compose up -d
```

---

## 📞 Destek İletişim

- **Geliştirici**: [Email]
- **DevOps**: [Email]
- **Acil Durum**: [Phone]

---

## 📅 Deployment Notları

- **Son Güncelleme**: 26 Ocak 2026
- **Versiyon**: v1.0.0 - FAZA 10
- **Değişiklikler**:
  - Store Attendant Panel eklendi
  - Dispatcher Panel eklendi
  - Mobile responsive tasarım
  - SignalR real-time notifications
