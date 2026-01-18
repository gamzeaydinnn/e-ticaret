# 🚀 SUNUCUYA DEPLOY KONTROL LİSTESİ

## ✅ ÖN KONTROLLER (Deploy Öncesi)

### 1️⃣ Backend Kontrolü

```powershell
cd c:\Users\GAMZE\Desktop\eticaret
dotnet build
```

**Beklenen:** 0 HATA, sadece warning'ler olmalı ✅

### 2️⃣ Frontend Kontrolü

```powershell
cd c:\Users\GAMZE\Desktop\eticaret\frontend
npm run build
```

**Beklenen:** "Compiled successfully!" ✅

### 3️⃣ Migration Kontrolü

```powershell
cd c:\Users\GAMZE\Desktop\eticaret
dotnet ef migrations list --project src/ECommerce.Data --startup-project src/ECommerce.API
```

**Beklenen:** Son migration: `AddCouponSystemTables` ✅

---

## 📋 API ROUTE KONTROL

### Kupon API Endpoint'leri

| Endpoint                   | Method | Controller       | Durum |
| -------------------------- | ------ | ---------------- | ----- |
| `/api/coupon/check/{code}` | GET    | CouponController | ✅    |
| `/api/coupon/validate`     | POST   | CouponController | ✅    |
| `/api/coupon/active`       | GET    | CouponController | ✅    |

### Kontrol Komutu

```powershell
# API çalıştır
cd c:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API
dotnet run

# Başka bir terminal'de test et:
curl http://localhost:5000/api/coupon/active
```

**Beklenen:**

```json
{
  "success": true,
  "data": [],
  "message": "Aktif kuponlar getirildi"
}
```

---

## 🔄 GIT KONTROL

### Değiştirilen Dosyalar

```bash
git status
```

**Değişen dosyalar:**

1. ✅ `src/ECommerce.API/Infrastructure/ProductSeeder.cs` - Veri koruma
2. ✅ `src/ECommerce.API/Infrastructure/IdentitySeeder.cs` - Veri koruma
3. ✅ `src/ECommerce.API/Program.cs` - Log mesajları
4. ✅ `src/ECommerce.Data/Context/ECommerceDbContext.cs` - CouponUsage, CouponProduct DbSet
5. ✅ `src/ECommerce.Data/Migrations/*AddCouponSystemTables*` - Yeni migration
6. ✅ `src/ECommerce.API/Controllers/CouponController.cs` - Kupon API
7. ✅ `frontend/src/components/CartPage.jsx` - Profesyonel UI
8. ✅ `frontend/src/components/CartPage.css` - Mobil uyumlu CSS
9. ✅ `frontend/src/services/cartService.js` - Kupon metodları

### Commit ve Push

```bash
git add .
git commit -m "feat: Kupon sistemi tamamlandı + veri koruma + profesyonel sepet UI

- ProductSeeder ve IdentitySeeder'da veri koruma
- Kupon sistemi API endpoint'leri
- CouponUsage ve CouponProduct entity'leri
- AddCouponSystemTables migration
- CartPage profesyonel ve mobil uyumlu tasarım
- Kupon doğrulama ve uygulama sistemi
- Volume mapping ile görseller korunuyor"

git push origin main
```

---

## 🐳 SUNUCUDA DEPLOY İŞLEMİ

### 1️⃣ Sunucuya Bağlan

```bash
ssh root@31.186.24.78
# veya PuTTY ile bağlan
```

### 2️⃣ Proje Dizinine Git

```bash
cd /home/eticaret
```

### 3️⃣ Git Pull

```bash
git pull origin main
```

### 4️⃣ Container'ları Yeniden Başlat

```bash
# Önce durdur
docker-compose -f docker-compose.prod.yml down

# Yeniden başlat (build ile)
docker-compose -f docker-compose.prod.yml up -d --build
```

### 5️⃣ Log Kontrol

```bash
# API log'larını izle
docker logs -f ecommerce-api-prod

# Son 100 satır
docker logs --tail 100 ecommerce-api-prod
```

**Beklenen Log Mesajları:**

```
🔍 Database.Migrate() çağrılıyor...
✅ Database migrations uygulandı

🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ IdentitySeeder: Roller zaten mevcut, seed ATLANILIYOR (kullanıcılar KORUNUYOR)
✅ IdentitySeeder tamamlandı

🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ ProductSeeder: Veritabanında mevcut veriler var, seed ATLANILIYOR (veriler KORUNUYOR)
✅ ProductSeeder tamamlandı

🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ BannerSeeder: Veritabanında zaten banner mevcut, seed atlanıyor
✅ BannerSeeder tamamlandı

✅✅✅ TÜM SEED İŞLEMLERİ BAŞARIYLA TAMAMLANDI! ✅✅✅
```

---

## 🧪 SUNUCUDA TEST

### 1️⃣ API Health Check

```bash
curl http://localhost:5000/health
# veya
curl http://31.186.24.78:5000/health
```

**Beklenen:**

```
Healthy
```

### 2️⃣ Kupon API Test

```bash
# Aktif kuponları listele
curl http://localhost:5000/api/coupon/active

# Kupon kodu kontrol
curl http://localhost:5000/api/coupon/check/WELCOME10
```

### 3️⃣ Frontend Test

```
http://31.186.24.78:3000
```

**Kontrol Edilecekler:**

- ✅ Ana sayfa yükleniyor mu?
- ✅ Ürünler gösteriliyor mu?
- ✅ Sepete ekleme çalışıyor mu?
- ✅ Sepet sayfası profesyonel görünüyor mu?
- ✅ Kupon kodu giriş alanı var mı?
- ✅ Mobil görünüm düzgün mü?

---

## 📊 VERİTABANI KONTROL

### Container İçinden SQL Bağlantısı

```bash
# SQL Server container'ına bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C

# Tabloları kontrol et
SELECT name FROM sys.tables ORDER BY name;
GO

# Kupon tablosunu kontrol et
SELECT Id, Code, Type, Value, IsActive, StartDate, ExpirationDate FROM Coupons;
GO

# CouponUsage tablosu var mı?
SELECT COUNT(*) FROM CouponUsages;
GO

# CouponProducts tablosu var mı?
SELECT COUNT(*) FROM CouponProducts;
GO

# Çıkış
EXIT
```

---

## 🔒 GÜVENLİK KONTROL

### 1️⃣ JWT Secret

```bash
# docker-compose.prod.yml içinde:
- Jwt__Key=YourVeryStrongSecretKeyMinimum32CharactersLong!!!
```

✅ Üretim ortamı için **mutlaka değiştirilmeli**

### 2️⃣ Database Password

```bash
# docker-compose.prod.yml içinde:
- SA_PASSWORD=${DB_PASSWORD:-ECom1234}
```

✅ Üretim ortamı için **mutlaka değiştirilmeli**

### 3️⃣ SMS API Credentials

```bash
# docker-compose.prod.yml içinde:
- NetGsm__UserCode=8503078774
- NetGsm__Password=123456Z-M
```

✅ Gerçek credentials kullanılıyor

---

## 🗂️ VOLUME KONTROL

### Volume Listesi

```bash
docker volume ls
```

**Beklenen:**

```
eticaret_sqlserver-data    # Veritabanı dosyaları
```

### Volume İçeriğini Kontrol

```bash
# Uploads klasörünü kontrol et
ls -lah /home/eticaret/uploads/

# Banners var mı?
ls -lah /home/eticaret/uploads/banners/
```

---

## 📱 MOBİL TEST

### Test Cihazlar

1. ✅ iPhone Safari
2. ✅ Android Chrome
3. ✅ Tablet

### Kontrol Edilecekler

- ✅ Sepet sayfası düzgün görünüyor mu?
- ✅ Kupon kodu giriş alanı kullanılabilir mi?
- ✅ Butonlar dokunulabilir mi?
- ✅ Kargo seçimi çalışıyor mu?
- ✅ Scroll performansı iyi mi?

---

## 🎯 SENARYO TESTLERİ

### Test 1: Kupon Oluşturma ve Kullanma

```
1. Admin paneline gir: http://31.186.24.78:3000/admin
2. Kupon Yönetimi > Yeni Kupon Ekle
3. Kod: WELCOME10
4. Tip: Yüzde İndirim
5. Değer: 10
6. Min. Sipariş: 2000
7. Kaydet

8. Sepete ürün ekle (2000₺ üzeri)
9. Sepet sayfasında kupon kodunu gir: WELCOME10
10. "Uygula" butonuna tıkla

✅ Beklenen: "Kupon uygulandı! X₺ indirim kazandınız."
```

### Test 2: Sunucuya Yeniden Deploy (Veri Koruma)

```
1. Admin panelinden yeni ürün ekle
2. Kategori oluştur
3. Görsel yükle
4. Kupon ekle

5. Sunucuda deploy:
   docker-compose -f docker-compose.prod.yml down
   docker-compose -f docker-compose.prod.yml up -d --build

6. Tekrar kontrol et

✅ Beklenen: TÜM veriler ve görseller korunmalı
```

### Test 3: Migration Kontrolü

```
1. Yeni migration ekle (local):
   dotnet ef migrations add TestMigration --project src/ECommerce.Data --startup-project src/ECommerce.API

2. Git push
3. Sunucuda git pull
4. Container'ları yeniden başlat

✅ Beklenen: Migration otomatik uygulanmalı, veriler kaybolmamalı
```

---

## 🚨 SORUN GİDERME

### Sorun 1: API Başlamıyor

```bash
# Log'ları kontrol et
docker logs ecommerce-api-prod

# Veritabanı bağlantısını test et
docker exec -it ecommerce-api-prod dotnet --version
```

### Sorun 2: Frontend Yüklenmiyor

```bash
# Frontend log'larını kontrol et
docker logs ecommerce-frontend-prod

# Container çalışıyor mu?
docker ps -a
```

### Sorun 3: Kupon "Geçersiz" Hatası

```bash
# Veritabanında kupon var mı?
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "SELECT * FROM Coupons WHERE Code = 'WELCOME10'"

# API endpoint'i çalışıyor mu?
curl http://localhost:5000/api/coupon/check/WELCOME10
```

### Sorun 4: Görseller Görünmüyor

```bash
# Volume mapping doğru mu?
docker inspect ecommerce-api-prod | grep -A 10 "Mounts"

# Uploads klasörü var mı?
ls -lah /home/eticaret/uploads/
```

---

## 📈 PERFORMANS İZLEME

### Container Resource Kullanımı

```bash
docker stats ecommerce-api-prod ecommerce-frontend-prod ecommerce-sql-prod
```

### Disk Kullanımı

```bash
df -h
docker system df
```

---

## ✅ DEPLOY BAŞARILI KONTROL LİSTESİ

- [ ] Backend derlendi (0 hata)
- [ ] Frontend derlendi (0 hata)
- [ ] Migration oluşturuldu
- [ ] Git commit & push yapıldı
- [ ] Sunucuda git pull çalıştırıldı
- [ ] Container'lar yeniden başlatıldı
- [ ] API log'larında hata yok
- [ ] API health check çalışıyor
- [ ] Frontend erişilebilir
- [ ] Kupon API çalışıyor
- [ ] Admin paneli açılıyor
- [ ] Kupon oluşturma çalışıyor
- [ ] Kupon uygulama çalışıyor
- [ ] Mobil görünüm düzgün
- [ ] Veriler korunuyor (test edildi)
- [ ] Görseller korunuyor (test edildi)

---

## 📞 ACİL DURUM KİŞİLERİ

**Teknik Destek:** [Telefon/Email]  
**Sunucu Yöneticisi:** [Telefon/Email]  
**Database Admin:** [Telefon/Email]

---

## 🎉 BAŞARILI DEPLOY!

Tüm kontroller geçildiyse:

```
🎊 TEBRİKLER! 🎊

✅ Backend başarıyla deploy edildi
✅ Frontend başarıyla deploy edildi
✅ Kupon sistemi çalışıyor
✅ Veriler korunuyor
✅ Profesyonel sepet UI aktif

🚀 Sistem hazır!
```

---

## 📝 SONRAKI ADIMLAR

1. [ ] SSL sertifikası kurulumu (HTTPS)
2. [ ] Domain bağlantısı
3. [ ] Monitoring sistemi (Prometheus/Grafana)
4. [ ] Otomatik backup planı
5. [ ] Staging environment kurulumu
6. [ ] CI/CD pipeline (GitHub Actions)
