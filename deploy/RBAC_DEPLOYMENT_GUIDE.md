# 🚀 RBAC Sistemi Production Deployment Rehberi

## 📋 İçindekiler

1. [Ön Hazırlık](#ön-hazırlık)
2. [Deployment Adımları](#deployment-adımları)
3. [Doğrulama ve Test](#doğrulama-ve-test)
4. [Rollback Prosedürü](#rollback-prosedürü)
5. [Troubleshooting](#troubleshooting)

---

## 🎯 Ön Hazırlık

### Sistem Gereksinimleri

- ✅ Docker ve Docker Compose kurulu olmalı
- ✅ SQL Server 2022 veya üzeri
- ✅ .NET 9.0 Runtime
- ✅ Minimum 2GB RAM (backend için)
- ✅ Minimum 10GB disk alanı

### Deployment Öncesi Kontrol Listesi

```bash
# 1. Sunucuya SSH bağlantısı
ssh huseyinadm@31.186.24.78

# 2. Mevcut sistem durumunu kaydet
cd /home/huseyinadm/ecommerce
docker ps > pre-deployment-containers.log
docker images > pre-deployment-images.log

# 3. Veritabanı yedeği al (KRİTİK!)
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "BACKUP DATABASE [ECommerceDb] TO DISK = '/backups/ECommerceDb_$(date +%Y%m%d_%H%M%S).bak' WITH INIT"

# 4. Uploads klasörü yedeği
tar -czf /home/huseyinadm/backups/uploads_$(date +%Y%m%d_%H%M%S).tar.gz uploads/

# 5. Logs klasörü yedeği
tar -czf /home/huseyinadm/backups/logs_$(date +%Y%m%d_%H%M%S).tar.gz logs/
```

### Önemli Notlar

⚠️ **ÇOK ÖNEMLİ**: Deployment sırasında sisteme erişim olmayacağı için kullanıcıları önceden bilgilendirin.
⚠️ **Veritabanı Yedeği**: Migration öncesi mutlaka tam yedek alın.
⚠️ **Rollback Planı**: Bu dokümanda açıklanan rollback adımlarını anlayıp hazır olun.

---

## 🔧 Deployment Adımları

### Adım 1: Kod Güncellemesi

```bash
# Git repository'den son değişiklikleri çek
cd /home/huseyinadm/ecommerce
git fetch origin
git pull origin main

# Eğer kod manuel transfer ediliyorsa:
# scp -r C:\Users\GAMZE\Desktop\eticaret/* huseyinadm@31.186.24.78:/home/huseyinadm/ecommerce/
```

### Adım 2: Docker Container'ları Durdurma

```bash
# Tüm container'ları graceful shutdown
docker-compose -f docker-compose.prod.yml down --timeout 30

# Container'ların tamamen durduğunu doğrula
docker ps -a | grep ecommerce
```

### Adım 3: Docker Image'larını Yeniden Build Etme

```bash
# Eski image'ları temizle (opsiyonel - disk alanı için)
docker image prune -f

# Backend image'ını build et (cache'siz - en güvenli yöntem)
docker-compose -f docker-compose.prod.yml build --no-cache api

# Frontend image'ını build et
docker-compose -f docker-compose.prod.yml build --no-cache frontend

# Build loglarını kontrol et - hata var mı?
# Hata varsa deployment'ı DURDUR ve rollback yap!
```

### Adım 4: Veritabanı Migration'ı

```bash
# Container'ları başlat (sadece sqlserver ve api)
docker-compose -f docker-compose.prod.yml up -d sqlserver

# SQL Server'ın hazır olmasını bekle (30 saniye)
sleep 30

# Healthcheck ile doğrula
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -Q 'SELECT 1'

# API container'ını başlat
docker-compose -f docker-compose.prod.yml up -d api

# Migration loglarını izle
docker logs -f ecommerce-api-prod

# Beklenen çıktılar:
# ✅ "IdentitySeeder tüm işlemleri başarıyla tamamladı"
# ✅ "✅ Permissions seed edildi: X eklendi, Y güncellendi"
# ✅ "✅ RolePermissions seed edildi: X atama eklendi"
# ✅ "Now listening on: http://[::]:5000"

# Eğer migration hatası görürseniz:
# ❌ "ALTER TABLE" hatası → Tablo zaten var, seed kısmı çalışmalı
# ❌ "Foreign Key" hatası → Rollback gerekebilir
# ❌ "Login failed" → Connection string kontrol edin
```

### Adım 5: Frontend Container'ı Başlatma

```bash
# Frontend'i başlat
docker-compose -f docker-compose.prod.yml up -d frontend

# Tüm container'ların sağlıklı olduğunu doğrula
docker-compose -f docker-compose.prod.yml ps

# Beklenen çıktı:
# ecommerce-sql-prod      Up (healthy)
# ecommerce-api-prod      Up
# ecommerce-frontend-prod Up (healthy)
```

### Adım 6: Servis Sağlığı Kontrolü

```bash
# Backend health check
curl -f http://localhost:5000/health || echo "Backend HATA!"

# Frontend health check
curl -f http://localhost:3000/ || echo "Frontend HATA!"

# API swagger erişim kontrolü
curl -I http://localhost:5000/swagger/index.html

# Container loglarını kontrol et
docker logs --tail 50 ecommerce-api-prod
docker logs --tail 50 ecommerce-frontend-prod
```

---

## ✅ Doğrulama ve Test

### Fonksiyonel Testler

```bash
# 1. Admin login testi (SuperAdmin)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@local",
    "password": "Admin123!"
  }'

# Beklenen: 200 OK, token içeren response

# 2. Permission endpoint testi
# (Yukarıdaki token'ı <TOKEN> yerine yapıştırın)
curl -X GET http://localhost:5000/api/auth/permissions \
  -H "Authorization: Bearer <TOKEN>"

# Beklenen: Permission listesi JSON array

# 3. Rol yönetimi endpoint testi
curl -X GET http://localhost:5000/api/admin/roles \
  -H "Authorization: Bearer <TOKEN>"

# Beklenen: 5 rol (SuperAdmin, StoreManager, CustomerSupport, Logistics, Customer)

# 4. Frontend admin panel erişim testi
# Tarayıcıda: http://31.186.24.78:3000/admin/login
# - Admin kullanıcı ile giriş yap
# - Dashboard'a erişim sağla
# - /admin/roles sayfasını aç
# - /admin/permissions sayfasını aç
```

### Veritabanı Kontrolü

```bash
# SQL Server'a bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C

# Aşağıdaki SQL sorgularını çalıştır:

-- 1. Permissions tablosu dolu mu?
SELECT COUNT(*) AS PermissionCount FROM Permissions;
-- Beklenen: 60+ kayıt

-- 2. RolePermissions atamaları var mı?
SELECT COUNT(*) AS RolePermissionCount FROM RolePermissions;
-- Beklenen: 100+ kayıt

-- 3. Roller doğru mu?
SELECT r.Name, COUNT(rp.Id) AS PermissionCount
FROM AspNetRoles r
LEFT JOIN RolePermissions rp ON CAST(r.Id AS INT) = rp.RoleId
GROUP BY r.Name
ORDER BY r.Name;
-- Beklenen:
-- SuperAdmin: ~60 izin
-- StoreManager: ~40 izin
-- CustomerSupport: ~10 izin
-- Logistics: ~8 izin
-- Customer: 0 izin

-- 4. Admin kullanıcı SuperAdmin rolünde mi?
SELECT u.Email, u.Role, r.Name AS IdentityRole
FROM Users u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Email = 'admin@local';
-- Beklenen: Email=admin@local, Role=SuperAdmin, IdentityRole=SuperAdmin

GO
EXIT
```

### Performans ve Log Kontrolü

```bash
# CPU ve Memory kullanımı
docker stats --no-stream

# Beklenen:
# ecommerce-api-prod: CPU < 10%, MEM < 500MB (idle durumda)
# ecommerce-frontend-prod: CPU < 5%, MEM < 200MB
# ecommerce-sql-prod: CPU < 20%, MEM < 1.5GB

# Backend loglarında hata var mı?
docker logs ecommerce-api-prod 2>&1 | grep -i "error\|exception\|fail"
# Beklenen: Sadece uyarılar (warning), kritik hata YOK

# Frontend loglarında hata var mı?
docker logs ecommerce-frontend-prod 2>&1 | grep -i "error"
# Beklenen: Nginx access log'ları, hata YOK
```

---

## 🔙 Rollback Prosedürü

⚠️ **Ne Zaman Rollback Yapılmalı:**

- Migration sırasında kritik hata oluşursa
- Backend servisler 5 dakika içinde ayağa kalkmıyorsa
- Veritabanı integrity hatası varsa
- Admin panel'e erişim sağlanamıyorsa

### Hızlı Rollback (Container Seviyesi)

```bash
# 1. Tüm container'ları durdur
docker-compose -f docker-compose.prod.yml down

# 2. Önceki image'ları kullan
docker images | grep ecommerce

# Eğer önceki image'lar varsa (örn: ecommerce-api:previous)
docker tag ecommerce-api-prod:latest ecommerce-api-prod:failed-$(date +%Y%m%d)
docker tag ecommerce-api-prod:previous ecommerce-api-prod:latest

# 3. Container'ları eski image ile başlat
docker-compose -f docker-compose.prod.yml up -d

# 4. Sağlık kontrolü
docker-compose -f docker-compose.prod.yml ps
curl http://localhost:5000/health
```

### Tam Rollback (Veritabanı Dahil)

```bash
# 1. Container'ları durdur
docker-compose -f docker-compose.prod.yml down

# 2. Veritabanı restore (SON ÇARE!)
# Backup dosyası adını kontrol et
ls -lh /backups/

# En son yedekten restore
docker-compose -f docker-compose.prod.yml up -d sqlserver
sleep 30

docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "RESTORE DATABASE [ECommerceDb] FROM DISK = '/backups/ECommerceDb_20260113_120000.bak' WITH REPLACE"

# 3. API ve Frontend'i eski versiyonla başlat
docker-compose -f docker-compose.prod.yml up -d

# 4. Sistem sağlığını doğrula
./deploy/verify-deployment.sh
```

### Kısmi Rollback (Sadece RBAC Sistemini Devre Dışı Bırakma)

Eğer RBAC sistemi sorun çıkarıyorsa ama sistem çalışıyorsa:

```sql
-- SQL Server'a bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C

-- RolePermissions tablosunu temizle (izinler kaldırılır, RBAC devre dışı kalır)
TRUNCATE TABLE RolePermissions;

-- İsteğe bağlı: Permissions tablosunu da temizle
DELETE FROM Permissions;

GO
EXIT

-- Backend'i restart et
docker restart ecommerce-api-prod
```

---

## 🔍 Troubleshooting

### Problem 1: Migration hatası - "Tablo zaten var"

**Belirti:**

```
Microsoft.Data.SqlClient.SqlException: There is already an object named 'Permissions' in the database.
```

**Çözüm:**

```bash
# Migration geçmişini kontrol et
docker exec ecommerce-api-prod dotnet ef migrations list --project ECommerce.Data

# Eğer "AddRBACPermissionSystem" zaten uygulanmışsa
# Seed kısmı çalıştırılmalı - bu normal bir durum
# Hata olarak algılamayın, logları kontrol edin
```

### Problem 2: IdentitySeeder çalışmıyor

**Belirti:**

```
Permissions seed edildi: 0 eklendi, 0 güncellendi
```

**Çözüm:**

```bash
# Program.cs'de seeder çağrısını kontrol et
docker exec ecommerce-api-prod grep -n "IdentitySeeder" /app/Program.cs

# Eğer çağrı yoksa, manuel SQL script ile seed et
docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C < deploy/seed-rbac-data.sql
```

### Problem 3: Admin kullanıcı giriş yapamıyor

**Belirti:**

```
401 Unauthorized - "Invalid email or password"
```

**Çözüm:**

```sql
-- Admin kullanıcı kontrolü
SELECT Email, EmailConfirmed, IsActive, Role FROM Users WHERE Email = 'admin@local';

-- Eğer admin kullanıcı yoksa veya şifre yanlışsa:
-- User Secrets'tan admin şifresini al veya varsayılan şifreyi dene: Admin123!

-- Admin kullanıcıyı yeniden oluştur (SQL)
DELETE FROM Users WHERE Email = 'admin@local';
-- Program.cs'deki IdentitySeeder tekrar çalıştırılacak
docker restart ecommerce-api-prod
```

### Problem 4: Permission endpoint 403 döndürüyor

**Belirti:**

```
GET /api/auth/permissions → 403 Forbidden
```

**Çözüm:**

```csharp
// AuthController.cs kontrol et - [Authorize] attribute olmalı
// [HasPermission] attribute OLMAMALI (herkes kendi izinlerini görebilmeli)

// Eğer kod düzeltme gerekiyorsa:
// 1. AuthController.cs'i düzelt
// 2. docker-compose build api --no-cache
// 3. docker-compose up -d api
```

### Problem 5: Frontend admin panel menüsü boş

**Belirti:**

```
AdminLayout sidebar'da hiçbir menü item görünmüyor
```

**Çözüm:**

```javascript
// AdminLayout.jsx kontrol et
// filteredMenuItems mantığı doğru çalışıyor mu?

// Browser console'da kontrol et:
localStorage.getItem("user"); // permissions array var mı?

// AuthContext.js kontrol et - loadUserPermissions() çağrılıyor mu?
```

### Problem 6: Docker container sürekli restart oluyor

**Belirti:**

```bash
docker ps
# STATUS: Restarting (1) 10 seconds ago
```

**Çözüm:**

```bash
# Hata loglarını detaylı incele
docker logs --tail 100 ecommerce-api-prod

# Olası nedenler:
# 1. Connection string yanlış → docker-compose.prod.yml kontrol
# 2. SQL Server hazır değil → healthcheck bekle
# 3. Port çakışması → netstat -tuln | grep 5000
# 4. Disk alanı dolmuş → df -h

# Geçici çözüm: restart policy'yi değiştir
docker update --restart=no ecommerce-api-prod
```

---

## 📊 Deployment Başarı Kriterleri

✅ **Deployment başarılı sayılır eğer:**

1. Tüm container'lar `Up` ve `healthy` durumda
2. Migration hatasız tamamlandı
3. Permissions tablosunda 60+ kayıt var
4. RolePermissions tablosunda 100+ atama var
5. Admin kullanıcı giriş yapabiliyor
6. `/api/auth/permissions` endpoint 200 döndürüyor
7. Admin panel `/admin/roles` ve `/admin/permissions` sayfaları açılıyor
8. Backend loglarında kritik hata yok
9. CPU ve Memory kullanımı normal seviyelerde
10. Önceki sipariş ve kullanıcı verileri korunmuş

---

## 📞 Destek ve İletişim

**Deployment sırasında kritik sorun yaşarsanız:**

1. Önce bu dokümandaki troubleshooting adımlarını deneyin
2. Rollback yapın (veri kaybını önlemek için)
3. Hata loglarını kaydedin
4. Teknik ekiple iletişime geçin

**Log dosyaları:**

- Backend: `docker logs ecommerce-api-prod > backend-error.log`
- Frontend: `docker logs ecommerce-frontend-prod > frontend-error.log`
- SQL: `docker logs ecommerce-sql-prod > sql-error.log`

---

## 🎉 Deployment Sonrası

Deployment başarılı olduktan sonra:

1. **Kullanıcıları bilgilendirin** - Sistem tekrar erişime açıldı
2. **İlk günü yakından takip edin** - Performans ve hata logları
3. **Yedekleme politikasını güncelleyin** - Artık Permissions ve RolePermissions tablolarını da yedekleyin
4. **Dokümantasyonu güncelleyin** - Deployment tarihi ve notlar
5. **Takım eğitimi planlayın** - RBAC sisteminin nasıl kullanılacağı

**Tebrikler! RBAC sistemi production'da! 🚀**
