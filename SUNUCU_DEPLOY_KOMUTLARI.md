# 🚀 SUNUCUYA DEPLOY KOMUTLARI

## ✅ API Route Kontrolü - HER ŞEY UYUMLU!

### Frontend → Backend Yapılandırması

```
Frontend                Nginx Proxy              Backend
---------------------------------------------------------------------
localhost:3000     →    /api/*          →    localhost:5000/api/*
localhost:3000     →    /uploads/*      →    localhost:5000/uploads/*

REACT_APP_API_URL=""  (boş = relative path, nginx proxy kullan)
```

### Kupon API Route'ları

```
Frontend Call:                Backend Controller:
GET  /api/coupon/check/{code}    → CouponController.cs [Route("api/[controller]")]
POST /api/coupon/validate         → CouponController.ValidateAsync()
GET  /api/coupon/active           → CouponController.GetActiveCouponsAsync()
```

**✅ SONUÇ: 404 ALMAYACAKSINIZ, TÜM ROUTE'LAR UYUMLU!**

---

## 🛡️ VERİ KORUMA GARANTİSİ

### Sunucuda Değişiklik Yaptığınızda:

```
Senaryolar:
1. Admin panelden ürün ekleme      → ✅ KORUNUR (DB'de kalır)
2. Görsel yükleme                  → ✅ KORUNUR (./uploads volume'de)
3. Kupon oluşturma                 → ✅ KORUNUR (DB'de kalır)
4. Kategori düzenleme              → ✅ KORUNUR (DB'de kalır)
5. Kullanıcı ekleme                → ✅ KORUNUR (DB'de kalır)
```

### Nasıl Korunuyor?

**1. Veritabanı:** `sqlserver-data` Docker volume ile saklanıyor

```yaml
volumes: sqlserver-data:/var/opt/mssql # ✅ Container silinse de veriler kalır
```

**2. Görseller:** `./uploads` klasörü HOST makineye mount ediliyor

```yaml
volumes:
  - ./uploads:/app/uploads # ✅ Container silinse de görseller kalır
```

**3. Seeder'lar:** Sadece ilk kurulumda çalışır

```csharp
// ✅ Eğer kategori/ürün VARSA seed çalışmaz
if (hasAnyCategory || hasAnyProduct) return;
```

---

## 📋 DEPLOY KOMUTLARI (CHAT İÇİN)

### 🔹 Adım 1: SSH Bağlantısı

```bash
ssh root@31.186.24.78
```

**Şifre:** (Putty'de kayıtlı)

---

### 🔹 Adım 2: Proje Dizinine Git

```bash
cd /home/eticaret
```

---

### 🔹 Adım 3: Mevcut Durumu Kontrol Et (Opsiyonel)

```bash
# Container'ların durumunu gör
docker ps

# Son deployment'tan beri ne değişti?
git fetch
git status
```

---

### 🔹 Adım 4: Git Pull (Kod Güncellemeleri)

```bash
git pull origin main
```

**Beklenen Çıktı:**

```
Updating a1b2c3d..e4f5g6h
Fast-forward
 src/ECommerce.API/Infrastructure/ProductSeeder.cs | 15 ++++++++-------
 frontend/src/components/CartPage.jsx             | 120 ++++++++++++++-----
 ...
 X files changed, Y insertions(+), Z deletions(-)
```

---

### 🔹 Adım 5: Container'ları Yeniden Başlat

```bash
# Önce durdur (veriler KORUNUR)
docker-compose -f docker-compose.prod.yml down

# Yeniden başlat ve rebuild et
docker-compose -f docker-compose.prod.yml up -d --build
```

**⏱️ Süre:** ~3-5 dakika

**Beklenen Çıktı:**

```
Building api...
Building frontend...
Creating network "eticaret_ecommerce-network" done
Creating volume "eticaret_sqlserver-data" done
Creating ecommerce-sql-prod ... done
Creating ecommerce-api-prod ... done
Creating ecommerce-frontend-prod ... done
```

---

### 🔹 Adım 6: Log'ları İzle (VERİ KORUMA KONTROLÜ)

```bash
docker logs -f ecommerce-api-prod
```

**CTRL+C ile çıkabilirsiniz**

**✅ ARANACAK MESAJLAR:**

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

**❌ EĞER BU MESAJLARI GÖRMEZSENİZ:**
Seed'ler çalışmış olabilir - ama bu sadece DB boşsa olur (ilk kurulumda)

---

### 🔹 Adım 7: API Health Check

```bash
curl http://localhost:5000/health
```

**Beklenen:**

```
Healthy
```

---

### 🔹 Adım 8: Kupon API Test

```bash
curl http://localhost:5000/api/coupon/active
```

**Beklenen:**

```json
{
  "success": true,
  "data": [...],
  "message": "Aktif kuponlar getirildi"
}
```

---

### 🔹 Adım 9: Container Durumu Kontrol

```bash
docker ps
```

**Beklenen:**

```
CONTAINER ID   IMAGE                    STATUS         PORTS
abc123def      ecommerce-frontend:latest   Up 2 minutes   0.0.0.0:3000->80/tcp
ghi456jkl      ecommerce-api-prod          Up 2 minutes   0.0.0.0:5000->5000/tcp
mno789pqr      ecommerce-sql-prod          Up 2 minutes   0.0.0.0:1435->1433/tcp
```

---

### 🔹 Adım 10: Frontend Test

Tarayıcıda aç:

```
http://31.186.24.78:3000
```

**Kontrol Listesi:**

- [ ] Ana sayfa açılıyor
- [ ] Ürünler gösteriliyor
- [ ] Sepete ekleme çalışıyor
- [ ] Sepet sayfası profesyonel görünüyor
- [ ] Kupon giriş alanı var
- [ ] Admin panel açılıyor (http://31.186.24.78:3000/admin)

---

## 🎯 TEST SENARYOSU: VERİ KORUMA

### Senaryo 1: Kupon Testi

```bash
# 1. Admin panelde kupon oluştur
http://31.186.24.78:3000/admin/coupons
Kod: TEST2025
Tip: Yüzde
Değer: 15
Min: 1000₺

# 2. Sepete ürün ekle
# 3. Kupon uygula: TEST2025
# 4. İndirim göreceksin: 150₺ (1000₺'nin %15'i)
```

### Senaryo 2: Veri Koruma Testi

```bash
# 1. Admin panelden yeni ürün ekle
# 2. Görsel yükle
# 3. Kupon oluştur

# 4. Deploy komutu:
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build

# 5. Siteyi tekrar aç
# ✅ BEKLENEN: TÜM VERİLER VE GÖRSELLER KORUNMALI
```

---

## 🚨 SORUN GİDERME

### ❌ Sorun: API başlamıyor

```bash
# Log'lara bak
docker logs ecommerce-api-prod

# Veritabanı erişimi test et
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "SELECT 1"
```

### ❌ Sorun: Frontend boş sayfa

```bash
# Frontend log'larına bak
docker logs ecommerce-frontend-prod

# Nginx config'i kontrol et
docker exec -it ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf
```

### ❌ Sorun: 404 hatası (kupon)

```bash
# Backend'de endpoint var mı?
docker exec -it ecommerce-api-prod ls /app

# API'ye direkt istek at
curl http://localhost:5000/api/coupon/active
```

### ❌ Sorun: Görseller görünmüyor

```bash
# Uploads klasörü var mı?
ls -lah /home/eticaret/uploads/

# Volume mapping doğru mu?
docker inspect ecommerce-api-prod | grep -A 5 "Mounts"
```

---

## 🎉 BAŞARILI DEPLOY KONTROL LİSTESİ

- [ ] SSH bağlantısı kuruldu
- [ ] Git pull yapıldı
- [ ] Container'lar yeniden başlatıldı
- [ ] Log'larda "seed ATLANILIYOR" mesajı görüldü (veriler korundu)
- [ ] Health check başarılı
- [ ] Frontend açılıyor
- [ ] API endpoint'leri çalışıyor
- [ ] Kupon sistemi test edildi
- [ ] Mobil görünüm kontrol edildi

---

## 📊 PERFORMANS İZLEME

### Container Resource Kullanımı

```bash
docker stats --no-stream
```

### Disk Kullanımı

```bash
df -h
du -sh /home/eticaret/uploads
```

### Log Boyutu

```bash
du -sh /home/eticaret/logs
```

---

## 🔄 İLK KURULUM vs GÜNCELLEME

### İLK KURULUM (Sunucu Boş):

```
1. git clone
2. docker-compose up -d --build
3. ✅ Seeder'lar çalışır (varsayılan veriler eklenir)
4. ✅ Admin kullanıcısı oluşturulur
5. ✅ Kategoriler ve örnek ürünler eklenir
```

### GÜNCELLEME (Sunucuda Veri Var):

```
1. git pull
2. docker-compose down
3. docker-compose up -d --build
4. ✅ Seeder'lar ATLANIR (veriler korunur)
5. ✅ Migration'lar uygulanır (yeni tablolar eklenir)
6. ✅ Mevcut ürünler, kullanıcılar, kuponlar KORUNUR
```

---

## 💾 BACKUP ÖNERİSİ

### Manuel Backup (Haftada 1):

```bash
# Veritabanı backup
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "BACKUP DATABASE ECommerceDb TO DISK = '/backups/ecommerce_$(date +%Y%m%d).bak'"

# Görseller backup
tar -czf /home/eticaret/backups/uploads_$(date +%Y%m%d).tar.gz /home/eticaret/uploads
```

---

## 📞 DESTEK

Herhangi bir sorun yaşarsanız:

1. Log dosyalarını kontrol edin
2. Container'ların çalıştığını doğrulayın
3. API endpoint'lerini test edin
4. Volume'lerin mount edildiğini kontrol edin

---

## ✅ ÖZET

```
VERİ KORUMA:     ✅ Docker Volume + Smart Seeder
API ROUTE:       ✅ /api/coupon/* (uyumlu)
GÖRSELLER:       ✅ ./uploads (mount edildi)
NGINX PROXY:     ✅ /api → backend:5000
FRONTEND .ENV:   ✅ REACT_APP_API_URL="" (relative)

🚀 SUNUCUYA DEPLOY YAPABİLİRSİNİZ!
🛡️ VERİLERİNİZ HER DEPLOY'DA KORUNACAK!
```
