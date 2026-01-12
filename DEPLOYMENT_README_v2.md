# 🚀 SUNUCU DEPLOYMENT REHBERİ - v2.0

## Admin Panel Fix + Full Production Deploy

---

## 📋 DEPLOYMENT ÖNCESİ KONTROL LİSTESİ

### ✅ Yapılan Düzeltmeler ve İyileştirmeler

#### 1. **Admin Panel Routing Sorunu Çözüldü** ✅

- **Sorun:** Sunucuda `/admin` path'i çalışmıyordu
- **Çözüm:** Frontend Dockerfile'daki nginx config'e admin routing eklendi

```nginx
location /admin {
    try_files $uri $uri/ /index.html;
}
```

#### 2. **Banner/Poster API Entegrasyonu Düzeltildi** ✅

- **Sorun:** App.js'de yanlış API endpoint kullanılıyordu (`/banners` yerine `/api/banners`)
- **Çözüm:** `bannerService` kullanılarak doğru API çağrıları yapıldı
- Admin panelinden yapılan değişiklikler artık ana sayfaya yansıyor

#### 3. **RefreshToken Implementasyonu Doğrulandı** ✅

- Database'de `RefreshTokens` tablosu mevcut
- `AuthController.cs`'de refresh token endpoint'leri aktif
- Login response'da hem `token` hem de `refreshToken` dönüyor

#### 4. **Database Migration ve Seed** ✅

- `Program.cs`'de otomatik migration aktif (`db.Database.Migrate()`)
- Seed işlemleri:
  - IdentitySeeder → Admin kullanıcısı ve roller
  - ProductSeeder → Örnek ürünler
  - BannerSeeder → Ana sayfa slider ve promo görselleri
  - CategorySeeder → Kategoriler

#### 5. **Nginx Konfigürasyonu Güncellemesi** ✅

- Frontend container nginx config güncellendi
- Host nginx config template'i hazırlandı (HTTPS desteği ile)
- `/api` ve `/uploads` proxy ayarları yapılandırıldı

---

## 🎯 DEPLOYMENT ADIMLARI

### ADIM 1: Yedekleme (ZORUNLU!)

```bash
cd /root/eticaret  # veya projenin bulunduğu dizin
chmod +x sunucu-yedekle.sh
./sunucu-yedekle.sh
```

**Çıktı:** `backup_YYYYMMDD_HHMMSS.tar.gz` dosyası oluşur

---

### ADIM 2: Deployment

```bash
chmod +x sunucu-deploy-sifirdan.sh
./sunucu-deploy-sifirdan.sh
```

Bu script şunları yapar:

1. ✅ Mevcut sistemi yedekler
2. ✅ Eski container'ları temizler
3. ✅ Frontend build eder (admin panel fix ile)
4. ✅ Nginx config'i günceller
5. ✅ Docker images'ları build eder
6. ✅ Database başlatır ve migration çalıştırır
7. ✅ Backend başlatır ve seed işlemlerini yapar
8. ✅ Frontend başlatır
9. ✅ Host nginx'i günceller (HTTPS)
10. ✅ Health check'leri yapar

**Süre:** Yaklaşık 5-10 dakika

---

### ADIM 3: Deployment Sonrası Kontrol

#### 3.1 Container Durumu

```bash
docker-compose -f docker-compose.prod.yml ps
```

**Beklenen:** Tüm container'lar `Up` durumda

#### 3.2 Backend Health Check

```bash
curl http://localhost:5000/api/health
```

**Beklenen:** `200 OK`

#### 3.3 Frontend Erişimi

```bash
curl -I http://localhost:3000
```

**Beklenen:** `200 OK`

#### 3.4 Admin Panel Erişimi

Tarayıcıda: `https://golkoygurme.com.tr/admin`
**Beklenen:** Admin login sayfası açılmalı

**Test Kullanıcısı:**

- Email: `admin@admin.com`
- Şifre: `admin123`

#### 3.5 Banner API Kontrolü

```bash
curl http://localhost:5000/api/banners/slider
curl http://localhost:5000/api/banners/promo
```

**Beklenen:** JSON array döner (3 slider, 4 promo banner)

---

## 🔧 SORUN GİDERME

### Backend çalışmıyor

```bash
docker logs ecommerce-api-prod --tail 100
```

**Yaygın sorunlar:**

- SQL Server bağlantı hatası → `docker logs ecommerce-sql-prod`
- Migration hatası → Log'larda "❌ SEED HATASI" ara

### Frontend çalışmıyor

```bash
docker logs ecommerce-frontend-prod --tail 100
```

### Admin panel açılmıyor

1. Container'ın çalıştığından emin ol
2. Nginx config'i kontrol et:
   ```bash
   docker exec ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf | grep admin
   ```
   **Beklenen:** `location /admin` bloğu görmeli

### API çağrıları çalışmıyor (ECONNREFUSED)

```bash
# Backend port kontrolü
netstat -tulpn | grep 5000

# Frontend port kontrolü
netstat -tulpn | grep 3000

# Proxy test
curl -v http://localhost:3000/api/health
```

---

## ⚠️ ACİL GERİ ALMA

Deployment sonrası ciddi sorun çıkarsa:

```bash
chmod +x sunucu-geri-yukle.sh
./sunucu-geri-yukle.sh backup_YYYYMMDD_HHMMSS
```

Bu işlem:

- Eski container'ları geri yükler
- Veritabanını eski haline döndürür
- Nginx config'i geri yükler

**Süre:** 2-3 dakika

---

## 📝 ÖNEMLİ NOTLAR

### 1. Environment Variables

Production ortamında hassas bilgiler `.env` dosyasında:

```bash
DB_PASSWORD=ECom1234
JWT_KEY=YourVeryStrongSecretKeyMinimum32CharactersLong!!!
```

### 2. SSL Sertifikası Yenileme

Let's Encrypt sertifikaları 90 günde bir yenilenmeli:

```bash
sudo certbot renew --dry-run  # Test
sudo certbot renew            # Gerçek yenileme
```

### 3. Database Backup Otomasyonu

Günlük otomatik yedekleme için cron job:

```bash
crontab -e

# Her gece 02:00'de yedek al
0 2 * * * /root/eticaret/sunucu-yedekle.sh >> /var/log/backup.log 2>&1
```

### 4. Log Yönetimi

Loglar zamanla büyür, düzenli temizleme gerekir:

```bash
# Log boyutlarını kontrol et
du -sh ./logs/*

# 30 günden eski logları sil
find ./logs -name "*.log" -mtime +30 -delete
```

### 5. Container Resource Limitleri

Gerekirse `docker-compose.prod.yml`'de resource limit'leri ayarla:

```yaml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: "1.0"
          memory: 1G
        reservations:
          memory: 512M
```

---

## 🎉 BAŞARILI DEPLOYMENT SONRASI

Aşağıdaki servislere erişim sağlanmalı:

| Servis          | URL                                   | Durum                                   |
| --------------- | ------------------------------------- | --------------------------------------- |
| Ana Sayfa       | https://golkoygurme.com.tr            | ✅ Slider ve promo banner'lar görünmeli |
| Admin Panel     | https://golkoygurme.com.tr/admin      | ✅ Login sayfası açılmalı               |
| API Health      | https://golkoygurme.com.tr/api/health | ✅ 200 OK                               |
| Poster Yönetimi | Admin → Poster Yönetimi               | ✅ Banner CRUD işlemleri                |

---

## 📞 DESTEK

Sorun yaşarsan:

1. Logları kontrol et: `docker logs ecommerce-api-prod -f`
2. Container durumlarını kontrol et: `docker ps -a`
3. Yedekten geri dön: `./sunucu-geri-yukle.sh`

---

## ✅ CHECKLIST

- [ ] Yedekleme alındı
- [ ] Deployment scripti çalıştırıldı
- [ ] Tüm container'lar ayakta
- [ ] Backend health check başarılı
- [ ] Frontend erişilebilir
- [ ] Admin panel açılıyor
- [ ] Admin login çalışıyor
- [ ] Banner'lar görünüyor
- [ ] Poster yönetimi çalışıyor
- [ ] SSL sertifikası geçerli

---

**Hazırlayan:** Senior Developer  
**Tarih:** 2026-01-12  
**Versiyon:** 2.0.0  
**Son Güncelleme:** Admin Panel Routing Fix
