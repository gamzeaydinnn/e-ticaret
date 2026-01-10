# 🗄️ SUNUCU MIGRATION SÜRECİ - KAPSAMLI REHBER

## 📌 ÖZET

API sunucuda çalıştığında **otomatik olarak**:

1. ✅ Veritabanını oluşturur (`ECommerceDb`)
2. ✅ Tüm tabloları oluşturur (schema)
3. ✅ Başlangıç verisini yükler (seed)

**Manuel komut çalıştırmaya ihtiyaç YOK!**

---

## 🔍 KOD: Program.cs'deki Otomatik Migration

```csharp
// Program.cs satır ~480-530

// 1. DbContext alınır
var db = services.GetRequiredService<ECommerceDbContext>();

// 2. Veritabanı schema oluşturulur
db.Database.EnsureCreated();

// 3. Identity (user/role) verisi yüklenir
IdentitySeeder.SeedAsync(services).GetAwaiter().GetResult();

// 4. Ürün/kategori verisi yüklenir
ProductSeeder.SeedAsync(services).GetAwaiter().GetResult();
```

**Sonuç**: Uygulama başladığı anda veritabanı tamamen hazır!

---

## 🖥️ SUNUCUDA NELER OLUR?

### ADIM 1: Docker Container Başlangıcı

```bash
docker-compose -f docker-compose.prod.yml up -d
```

### ADIM 2: SQL Server Container Hazırlanır

- Port 1433'ü dinlemeye başlar
- ~30 saniye beklenir (healthcheck)

### ADIM 3: API Container Başlar

```dockerfile
# Dockerfile
ENTRYPOINT ["dotnet", "ECommerce.API.dll"]
```

### ADIM 4: Program.cs Çalışır

```
[INFO] 🔍 DbContext alınıyor...
[INFO] ✅ DbContext alındı
[INFO] 🔍 Database initialization başlıyor...
[INFO] 🔍 EnsureCreated çağrılıyor...
[INFO] ✅ Database schema oluşturuldu
[INFO] 🔍 IdentitySeeder başlatılıyor...
[INFO] ✅ IdentitySeeder tamamlandı
[INFO] 🔍 ProductSeeder başlatılıyor...
[INFO] ✅ ProductSeeder tamamlandı
[INFO] ✅ Tüm seed işlemleri başarıyla tamamlandı!
```

### ADIM 5: API Hazır!

```
[INFO] Application started. Press Ctrl+C to shut down.
[INFO] Hosting environment: Production
[INFO] Content root path: /app
```

---

## ⏱️ TOPLAM SÜRE

| Aşama               | Süre              |
| ------------------- | ----------------- |
| SQL Server başlatma | 20-30s            |
| API startup         | 10-15s            |
| Migration + Seed    | 5-10s             |
| **TOPLAM**          | **~45-60 saniye** |

---

## 📊 VERITABANINDA OLUŞTULAN İÇERİK

### Tablolar

```sql
-- Aşağıdaki tüm tablolar otomatik oluşturulur:
AspNetUsers              -- Kullanıcılar
AspNetRoles             -- Roller (Admin, User, Courier)
AspNetUserRoles         -- Kullanıcı-Rol ilişkisi
Products                -- Ürünler
Categories              -- Kategoriler
Orders                  -- Siparişler
OrderItems              -- Sipariş detayları
... (30+ tablo toplamda)
```

### Seed Verisi

```sql
-- Otomatik yüklenen veriler:

-- Roller
INSERT INTO AspNetRoles VALUES ('admin', 'Admin')
INSERT INTO AspNetRoles VALUES ('user', 'User')
INSERT INTO AspNetRoles VALUES ('courier', 'Courier')

-- Test Kullanıcıları
INSERT INTO AspNetUsers VALUES (
    'admin@eticaret.com',
    'Admin Kullanıcısı',
    ...
)

-- Kategoriler
INSERT INTO Categories VALUES ('Elektronik', 'elektronik', ...)
INSERT INTO Categories VALUES ('Giyim', 'giyim', ...)
... (10+ kategori)

-- Ürünler
INSERT INTO Products VALUES ('Samsung Galaxy S25', 'samsung-galaxy-s25', ...)
... (50+ ürün)
```

---

## ✅ BAŞARILI MIGRATION KONTROL

### Log'ları İzle (Gerçek Zamanda)

```bash
ssh huseyinadm@31.186.24.78
cd ~/eticaret

# API log'larını izle (Migration mesajlarını göreceksin)
docker-compose -f docker-compose.prod.yml logs -f api

# Çıktı:
# [INFO] ✅ Database schema oluşturuldu
# [INFO] ✅ IdentitySeeder tamamlandı
# [INFO] ✅ ProductSeeder tamamlandı
```

### SQL Server'da Doğrula

```bash
# Container'a gir
docker exec -it ecommerce-sql-prod /bin/bash

# SQL sorgusu çalıştır
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# SQL'de çalıştır
SELECT COUNT(*) FROM ECommerceDb.dbo.Products
SELECT COUNT(*) FROM ECommerceDb.dbo.Categories
SELECT COUNT(*) FROM ECommerceDb.dbo.AspNetRoles
```

### API Health Check

```bash
# Categories endpoint'ten kontrol et
curl http://31.186.24.78:5000/api/categories

# Cevap örneği:
# [
#   {"id":1,"name":"Elektronik","slug":"elektronik",...},
#   {"id":2,"name":"Giyim","slug":"giyim",...},
#   ...
# ]
```

---

## ⚠️ SORUN GİDERME

### Problem: API başladı ama kategoriler yok

```bash
# 1. Log'ları kontrol et
docker-compose -f docker-compose.prod.yml logs api

# 2. Seed error görmüyorsan manual load et
docker exec -it ecommerce-sql-prod /bin/bash

# 3. SQL'de kontrol et
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# SQL komutları
SELECT COUNT(*) FROM ECommerceDb.dbo.Products
SELECT * FROM ECommerceDb.dbo.Categories
```

### Problem: SQL Server bağlantı hatası

```bash
# 1. SQL Server çalışıyor mu?
docker ps | grep sql

# 2. Bağlantı testi
docker exec ecommerce-sql-prod \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT 1"

# 3. Eğer hata varsa log'ları kontrol et
docker logs ecommerce-sql-prod | tail -50
```

### Problem: "Database already exists" hatası

Bu **normal ve beklenen**. İlk çalıştırmada:

```
🔍 EnsureCreated çağrılıyor...
✅ Database schema oluşturuldu
```

İkinci çalıştırmada veya container restart'ta:

```
// Veritabanı zaten var, bir şey yapılmaz
// Sadece seed'ler tekrar çalışabilir
```

**Çözüm**: Seed'ler idempotent (aynı sonucu verir), sorun değil.

---

## 🔄 MIGRATION GÜNCELLEME (Yeni Kod Çıkmazsa)

### Senaryo: Yeni tablo/column ekledim, nasıl migrate ederim?

**1. Yerel Geliştirme**

```bash
cd src/ECommerce.Data

# Migration oluştur
dotnet ef migrations add AddNewColumn

# Kontrol et
ls Migrations/ | tail -1  # Yeni migration dosyası görmelisın

# Uygula (geliştirme)
dotnet ef database update
```

**2. Sunucuya Deploy Et**

```bash
# Migration dosyaları otomatik push olur
git add -A
git commit -m "feat: Add new column"
git push origin main

# Sunucuda
ssh huseyinadm@31.186.24.78
cd ~/eticaret
git pull origin main

# API build'i migration'ları otomatik uygulaması için rebuild et
docker-compose -f docker-compose.prod.yml build --no-cache api
docker-compose -f docker-compose.prod.yml up -d api

# Log'ları izle
docker-compose -f docker-compose.prod.yml logs -f api
```

**3. Kontrol Et**

```bash
# Yeni column var mı?
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Products'"
```

---

## 🎯 ÖZET - MIGRATION NASIL ÇALIŞIR?

```
┌─────────────────────────────────────────┐
│  docker-compose up -d api               │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  SQL Server kontrol et (retry logic)   │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  ECommerceDbContext oluştur             │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  db.Database.EnsureCreated()            │
│  ➜ Tüm tabloları oluştur               │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  IdentitySeeder.SeedAsync()             │
│  ➜ Roller ve Users seed'le             │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  ProductSeeder.SeedAsync()              │
│  ➜ Ürün ve Kategori seed'le            │
└─────────────┬───────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────┐
│  ✅ API HAZIR - 5000/api dinliyor      │
└─────────────────────────────────────────┘
```

---

**Son Güncelleme**: 2026-01-10
**Sorumlu Dosya**: `src/ECommerce.API/Program.cs` (satır 480-530)
