# 🛡️ SUNUCUYA DEPLOY SONRASI VERİ KORUMA REHBERİ

## ⚠️ SORUN: Her Deploy'da Veriler Sıfırlanıyor

### 🔍 Tespit Edilen Problemler

1. ❌ **ProductSeeder** her başlangıçta kategorileri kontrol edip farklıysa SİLİYORDU
2. ❌ **IdentitySeeder** her seferinde çalışıyordu (şifre değişebiliyordu)
3. ✅ **BannerSeeder** zaten korunuyordu (`Any()` kontrolü vardı)
4. ✅ **Görseller** volume mapping ile korunuyordu (`./uploads:/app/uploads`)

---

## ✅ ÇÖZÜM: Akıllı Seed Sistemi

### 📋 Yeni Mantık

```
IF veritabanında HERHANGI bir veri varsa:
    ➡️ Seed işlemini ATLA (veriler KORUNUR)
ELSE:
    ➡️ Varsayılan verileri ekle (ilk kurulum için)
```

### 🔧 Güncellenmiş Dosyalar

#### 1️⃣ ProductSeeder.cs

```csharp
// ⚠️ GÜVENLİK: Veritabanında HERHANGI BİR kategori veya ürün varsa ASLA seed yapma!
var hasAnyCategory = await dbContext.Categories.AnyAsync();
var hasAnyProduct = await dbContext.Products.AnyAsync();

if (hasAnyCategory || hasAnyProduct)
{
    Console.WriteLine("ℹ️ ProductSeeder: Mevcut veriler var, seed ATLANILIYOR");
    return;
}
```

**Eskiden:** Slug kontrolü yapıp eşleşmezse VERİLERİ SİLİYORDU ❌  
**Şimdi:** Herhangi bir veri varsa ASLA seed yapMAZ ✅

#### 2️⃣ IdentitySeeder.cs

```csharp
// ⚠️ GÜVENLİK KONTROL: Eğer admin rolü varsa seed'i atla
var adminRole = await roleManager.FindByNameAsync("Admin");
if (adminRole != null)
{
    Console.WriteLine("ℹ️ IdentitySeeder: Roller mevcut, seed ATLANILIYOR");
    return;
}
```

**Eskiden:** Her seferinde çalışıyordu (şifre resetlenebilirdi) ❌  
**Şimdi:** Admin rolü varsa ASLA seed yapMAZ ✅

#### 3️⃣ BannerSeeder.cs

```csharp
// Zaten korunuyordu ✅
if (context.Banners.Any())
{
    Console.WriteLine("ℹ️ BannerSeeder: Banner'lar mevcut, seed atlanıyor");
    return;
}
```

---

## 📦 Docker Volume Mapping (Görseller İçin)

### docker-compose.prod.yml

```yaml
services:
  api:
    volumes:
      - ./logs:/app/logs # ✅ Log dosyaları korunur
      - ./uploads:/app/uploads # ✅ Yüklenen görseller korunur

  sqlserver:
    volumes:
      - sqlserver-data:/var/opt/mssql # ✅ Veritabanı dosyaları korunur
      - ./backups:/backups # ✅ Backup'lar korunur
```

**Sonuç:** Görseller container yeniden oluşturulsa bile HOST makinede korunur ✅

---

## 🚀 Sunucuya Deploy İşlemi

### 1️⃣ İlk Kurulum (Veritabanı Boş)

```bash
# 1. Projeyi çek
git pull origin main

# 2. Container'ları başlat
docker-compose -f docker-compose.prod.yml up -d --build

# 3. Migration otomatik çalışır
# 4. Seeder'lar otomatik çalışır (veritabanı boş olduğu için)
# ✅ Varsayılan kategoriler, ürünler, admin kullanıcısı oluşturulur
```

### 2️⃣ Sonraki Deploy'lar (Veritabanı Dolu)

```bash
# 1. Kod güncellemeleri çek
git pull origin main

# 2. Container'ları yeniden başlat
docker-compose -f docker-compose.prod.yml up -d --build

# 3. Migration otomatik çalışır (sadece yeni migration'lar uygulanır)
# 4. Seeder'lar ATLANIR (veritabanında veri olduğu için)
# ✅ Mevcut kategoriler, ürünler, kullanıcılar KORUNUR
```

---

## 🧪 Test Senaryosu

### Manuel Test

```bash
# 1. İlk kurulum
docker-compose -f docker-compose.prod.yml up -d --build

# 2. Admin panelinden ürün ekle, kategori güncelle
# 3. Görsel yükle

# 4. Container'ları yeniden başlat
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build

# ✅ BEKLENEN: Tüm veriler ve görseller korunmalı
```

---

## 📊 Log Çıktıları

### İlk Kurulum

```
🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...
🆕 IdentitySeeder: Roller ve admin kullanıcısı oluşturuluyor...
✅ IdentitySeeder tamamlandı

🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...
🆕 ProductSeeder: Veritabanı boş, varsayılan veriler ekleniyor...
✅ ProductSeeder tamamlandı

🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...
📝 BannerSeeder: Varsayılan banner'lar oluşturuluyor...
✅ BannerSeeder tamamlandı
```

### Sonraki Başlangıçlar (Veriler Korunur)

```
🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ IdentitySeeder: Roller zaten mevcut, seed ATLANILIYOR (kullanıcılar KORUNUYOR)
✅ IdentitySeeder tamamlandı

🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ ProductSeeder: Veritabanında mevcut veriler var, seed ATLANILIYOR (veriler KORUNUYOR)
✅ ProductSeeder tamamlandı

🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ BannerSeeder: Veritabanında zaten banner mevcut, seed atlanıyor
✅ BannerSeeder tamamlandı
```

---

## 🔄 Migration Stratejisi

### Database.Migrate() - Güvenli

```csharp
db.Database.Migrate(); // ✅ Sadece yeni migration'ları uygular
                       // ✅ Mevcut verileri korur
                       // ✅ Production-safe
```

**ASLA kullanma:**

```csharp
db.Database.EnsureCreated();  // ❌ Migration'ları bypass eder
db.Database.EnsureDeleted();  // ❌ Veritabanını siler!
```

---

## 📂 Korunan Veriler

| Veri Tipi          | Korunma Yöntemi              | Durum |
| ------------------ | ---------------------------- | ----- |
| Kategoriler        | Seed kontrolü (`AnyAsync()`) | ✅    |
| Ürünler            | Seed kontrolü (`AnyAsync()`) | ✅    |
| Kullanıcılar       | Seed kontrolü (Admin rolü)   | ✅    |
| Roller & İzinler   | Seed kontrolü (Admin rolü)   | ✅    |
| Banner'lar         | Seed kontrolü (`Any()`)      | ✅    |
| Yüklenen Görseller | Volume mapping               | ✅    |
| Veritabanı         | Volume mapping               | ✅    |
| Log Dosyaları      | Volume mapping               | ✅    |

---

## 🎯 Özet

### ✅ Artık Güvenli:

1. ✅ Her deploy sonrası **veriler korunur**
2. ✅ Görseller **volume'de saklanır**
3. ✅ Seeder'lar **sadece ilk kurulumda çalışır**
4. ✅ Admin şifresi **değişmez**
5. ✅ Kullanıcı eklediği ürünler **korunur**

### 🚀 Sunucuya Deploy Komutu

```bash
cd /home/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml up -d --build
```

### 📝 Not

- İlk kurulumda varsayılan veriler oluşturulur
- Sonraki deploy'larda tüm veriler korunur
- Görseller `./uploads` klasöründe saklanır
- Veritabanı `sqlserver-data` volume'ünde saklanır

---

## 🆘 Acil Durum: Veritabanını Sıfırlamak İsterseniz

```bash
# ⚠️ DİKKAT: TÜM VERİLER SİLİNİR!

# 1. Container'ları durdur
docker-compose -f docker-compose.prod.yml down

# 2. Volume'leri sil
docker volume rm eticaret_sqlserver-data

# 3. Yeniden başlat
docker-compose -f docker-compose.prod.yml up -d --build

# ✅ Veritabanı sıfırdan oluşturulur
# ✅ Seeder'lar yeniden çalışır
```

---

## 📞 Destek

Herhangi bir sorun yaşarsanız:

1. Log'ları kontrol edin: `docker logs ecommerce-api-prod`
2. Seeder mesajlarını kontrol edin
3. Volume mapping'i kontrol edin: `docker volume ls`
