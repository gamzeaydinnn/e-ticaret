# 🚀 SUNUCU MIGRATION KOMUTLARI - COPY-PASTE YAPMANIZ YETER

## ⚡ EN HIZLI YOL (Tüm Komutları Kopyala-Yapıştır)

```bash
# 1. Sunucuya bağlan
ssh huseyinadm@31.186.24.78

# 2. Klasöre git
cd ~/eticaret

# 3. Son kodu çek
git pull origin main

# 4. Docker'ı rebuild et
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# 5. 60 saniye bekle
sleep 60

# 6. Kontrol et
docker-compose -f docker-compose.prod.yml ps
curl http://localhost:5000/api/categories
```

---

## 🔧 ADIM ADIM KOMUTLAR

### ADIM 1: Sunucuya Bağlan

```bash
ssh huseyinadm@31.186.24.78
```

**Şifre**: `Passwd1122FFGG`

---

### ADIM 2: Proje Klasörüne Git

```bash
cd ~/eticaret
```

---

### ADIM 3: Son Kodu GitHub'dan Çek

```bash
git fetch origin
git pull origin main
```

---

### ADIM 4: Eski Konteynerleri Kapat

```bash
docker-compose -f docker-compose.prod.yml down
```

---

### ADIM 5: Docker Build (Yeni Kod ile)

```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

⏳ **Bu 5-10 dakika sürer!** Çay iç, espresso yap...

Çıktıda görmek istediğin:

```
Successfully built ...
Successfully tagged ecommerce-api:latest
Successfully tagged ecommerce-frontend:latest
```

---

### ADIM 6: Tüm Servisleri Başlat

```bash
docker-compose -f docker-compose.prod.yml up -d
```

---

### ADIM 7: Servislerin Başlamasını Bekle

```bash
# İlk olarak bu kodu çalıştır
sleep 60

# Sonra kontrol et
docker-compose -f docker-compose.prod.yml ps
```

**Çıktıda görmek istediğin**:

```
NAME                   STATUS
ecommerce-sql-prod     Up (healthy)
ecommerce-api-prod     Up
ecommerce-frontend-prod Up
```

---

### ADIM 8: Migration Loglarını İzle

```bash
docker-compose -f docker-compose.prod.yml logs api
```

**Görmek istediğin mesajlar**:

```
[INFO] 🔍 Database initialization başlıyor...
[INFO] 🔍 EnsureCreated çağrılıyor...
[INFO] ✅ Database schema oluşturuldu
[INFO] 🔍 IdentitySeeder başlatılıyor...
[INFO] ✅ IdentitySeeder tamamlandı
[INFO] 🔍 ProductSeeder başlatılıyor...
[INFO] ✅ ProductSeeder tamamlandı
[INFO] ✅ Tüm seed işlemleri başarıyla tamamlandı!
```

---

### ADIM 9: API'yi Test Et

```bash
curl http://localhost:5000/api/categories
```

**Beklenen çıktı** (JSON formatında kategori listesi):

```json
[
  {"id":1,"name":"Elektronik","slug":"elektronik",...},
  {"id":2,"name":"Giyim","slug":"giyim",...},
  ...
]
```

---

### ADIM 10: Frontend'i Test Et

```bash
curl -I http://localhost:3000
```

**Beklenen çıktı**:

```
HTTP/1.1 200 OK
```

---

## 🎯 SONUÇ - VERİTABANI NELER OLUŞTUYOR?

Migration çalıştığında otomatik olarak:

### ✅ Oluşturulan Tablolar

```sql
AspNetUsers              -- 1 admin user
AspNetRoles             -- 3 rol (Admin, User, Courier)
Products                -- 50+ ürün
Categories              -- 10+ kategori
Orders                  -- Sipariş tablosu
OrderItems              -- Sipariş detayları
... (20+ tablo daha)
```

### ✅ Oluşturulan Seed Verileri

```
👤 Admin Kullanıcı
   - Email: admin@eticaret.com
   - Şifre: Admin123!

📦 50+ Ürün (Elektronik, Giyim, vb.)

🏷️ 10+ Kategori

🎭 3 Rol (Admin, User, Courier)
```

---

## ⚠️ SORUN GİDERME

### Sorun 1: "Connection timeout"

```bash
# SQL Server'ın hazır olması bekleniyor
sleep 30
docker-compose -f docker-compose.prod.yml logs sqlserver | tail -20
```

### Sorun 2: "Database already exists" (NORMAL!)

```
Bu hata beklenen, veritabanı zaten var demektir.
Log'lar devam etmeli:
[INFO] ✅ Database schema oluşturuldu
```

### Sorun 3: API başladı ama kategoriler yok

```bash
# Log'ları kontrol et (detaylı hata göreceksin)
docker-compose -f docker-compose.prod.yml logs api

# SQL Server'a eriş
docker exec -it ecommerce-sql-prod /bin/bash

# SQL'de kontrol et
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Komutları çalıştır:
USE ECommerceDb
SELECT COUNT(*) FROM Products
SELECT COUNT(*) FROM Categories
GO
```

### Sorun 4: Container hatalı başladı

```bash
# Logları full gör
docker-compose -f docker-compose.prod.yml logs api --tail=200

# Container'ı yeniden başlat
docker-compose -f docker-compose.prod.yml restart api

# Log'ları izle
docker-compose -f docker-compose.prod.yml logs -f api
```

---

## 🌐 ERIŞIM ADRESLERİ (DEPLOY SONRASI)

```
Frontend:  http://31.186.24.78:3000
API:       http://31.186.24.78:5000/api
```

Test et:

```bash
# Tarayıcıda aç
http://31.186.24.78:3000

# Kategoriler görülmeli!
```

---

## ⏱️ ZAMAN ÖLÇÜSü

| İşlem              | Süre             |
| ------------------ | ---------------- |
| `git pull`         | 10 saniye        |
| `docker build`     | 5-10 dakika      |
| `docker up`        | 30-60 saniye     |
| Migration otomatik | 10-20 saniye     |
| **TOPLAM**         | **~6-11 dakika** |

---

## 💾 VERİTABANI BACKUP (İLK DEPLOY ÖNCESİ)

```bash
# (Opsiyonel) Eğer daha önce veri varsa backup al
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "BACKUP DATABASE ECommerceDb TO DISK = '/backups/db_backup_$(date +%Y%m%d_%H%M%S).bak'"
```

---

## 🔄 SADECE FRONTEND GÜNCELLEMEK İSTERSEM

```bash
cd ~/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml stop frontend
docker-compose -f docker-compose.prod.yml build --no-cache frontend
docker-compose -f docker-compose.prod.yml up -d frontend
docker-compose -f docker-compose.prod.yml logs -f frontend
```

---

## 🔄 SADECE API GÜNCELLEMEK İSTERSEM

```bash
cd ~/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml stop api
docker-compose -f docker-compose.prod.yml build --no-cache api
docker-compose -f docker-compose.prod.yml up -d api
docker-compose -f docker-compose.prod.yml logs -f api
```

---

**KESİN BİL**: Migration otomatik yapılıyor, manuel komut çalıştırmana gerek YOK!
API başladığı anda `EnsureCreated()` ve `SeedAsync()` otomatik çalışıyor.
