# 🚀 SUNUCUYA DEPLOY - SİPARİŞ DURUMU DÜZELTMESİ

## ✅ ÖN KONTROLLER TAMAMLANDI

### 1. Backend Endpoint Kontrolleri

- ✅ `/api/courier/*` - CourierController.cs mevcut
- ✅ `/api/admin/orders/{id}/status` - AdminOrdersController.cs mevcut
- ✅ Status normalizasyonu eklendi (out_for_delivery → OutForDelivery)
- ✅ Enum.TryParse case-insensitive yapıldı
- ✅ AllowedTransitions güncellendi (Pending → Preparing izni eklendi)

### 2. Frontend Kontrolleri

- ✅ `.env.production` → `REACT_APP_API_URL=` (BOŞ - DOĞRU ✓)
- ✅ Nginx proxy kullanılacak (relative path)
- ✅ AdminOrders.jsx status dropdown'ları çalışıyor

### 3. Nginx Konfigürasyonu

```nginx
# Mevcut nginx config - DEĞİŞİKLİK GEREKMİYOR
location /api/ {
    proxy_pass http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

**NOT:** `/api/courier/` tüm backend endpoint'leri `/api/` ile başladığı için otomatik proxy'lenir.

---

## 📋 DEPLOY KOMUTLARI

### 1️⃣ SSH Bağlantısı

```bash
ssh root@31.186.24.78
```

### 2️⃣ Proje Dizinine Git

```bash
cd /home/eticaret
```

### 3️⃣ Mevcut Durumu Kaydet (Opsiyonel - Güvenlik)

```bash
# Aktif container'ları listele
docker ps

# Hangi branch'deyiz?
git branch

# Son commit
git log -1 --oneline
```

### 4️⃣ Git Pull - Kod Güncellemeleri Al

```bash
git pull origin main
```

**Beklenen Çıktı:**

```
Updating abc1234..def5678
Fast-forward
 src/ECommerce.Business/Services/Managers/OrderManager.cs | 45 +++++++++++++++---
 frontend/src/pages/Admin/AdminOrders.jsx                | 12 ++---
 2 files changed, 47 insertions(+), 10 deletions(-)
```

### 5️⃣ Container'ları Durdur (Veriler KORUNUR)

```bash
docker-compose -f docker-compose.prod.yml down
```

**Beklenen Çıktı:**

```
Stopping ecommerce-frontend-prod ... done
Stopping ecommerce-api-prod      ... done
Stopping ecommerce-sql-prod      ... done
Removing ecommerce-frontend-prod ... done
Removing ecommerce-api-prod      ... done
Removing ecommerce-sql-prod      ... done
Removing network eticaret_ecommerce-network
```

**✅ ÖNEMLİ:** `Removing volumes` mesajı GELMEMELI (veriler korunuyor)

### 6️⃣ Container'ları Yeniden Başlat ve Rebuild Et

```bash
docker-compose -f docker-compose.prod.yml up -d --build
```

**⏱️ Süre:** 3-5 dakika  
**Ne Yapılıyor:**

- Backend .NET imajı build ediliyor
- Frontend React build alınıyor
- Container'lar başlatılıyor

**Beklenen Çıktı:**

```
Building api...
Step 1/12 : FROM mcr.microsoft.com/dotnet/aspnet:9.0
...
Successfully built abc123def456
Successfully tagged ecommerce-api-prod:latest

Building frontend...
Step 1/8 : FROM node:20-alpine as build
...
Successfully built ghi789jkl012
Successfully tagged ecommerce-frontend-prod:latest

Creating network "eticaret_ecommerce-network" ... done
Creating ecommerce-sql-prod      ... done
Creating ecommerce-api-prod      ... done
Creating ecommerce-frontend-prod ... done
```

### 7️⃣ Backend Log'larını İzle - VERİ KORUMA KONTROLÜ

```bash
docker logs -f ecommerce-api-prod
```

**✅ ARANACAK MESAJLAR:**

```
🔍🔍🔍 Database initialization başlıyor...
✅ Database migrations uygulandı

🔍 IdentitySeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ IdentitySeeder: Roller mevcut, seed devam ediyor (eksikler tamamlanacak)
✅ IdentitySeeder tamamlandı

🔍 ProductSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ ProductSeeder: Veritabanında mevcut veriler var, seed ATLANILIYOR (veriler KORUNUYOR)
✅ ProductSeeder tamamlandı

🖼️ BannerSeeder başlatılıyor (sadece DB boşsa çalışır)...
ℹ️ BannerSeeder: Veritabanında zaten banner mevcut, seed ATLANILIYOR (banner'lar KORUNUYOR)
✅ BannerSeeder tamamlandı

✅✅✅ TÜM SEED İŞLEMLERİ BAŞARIYLA TAMAMLANDI! ✅✅✅

Now listening on: http://0.0.0.0:5000
Application started. Press Ctrl+C to shut down.
```

**CTRL+C ile çık**

### 8️⃣ Container Durumu Kontrol

```bash
docker ps
```

**Beklenen:**

```
CONTAINER ID   IMAGE                        STATUS         PORTS
abc123         ecommerce-frontend-prod      Up 1 minute    0.0.0.0:3000->80/tcp
def456         ecommerce-api-prod           Up 1 minute    0.0.0.0:5000->5000/tcp
ghi789         ecommerce-sql-prod           Up 1 minute    0.0.0.0:1435->1433/tcp
```

### 9️⃣ API Health Check

```bash
curl http://localhost:5000/health
```

**Beklenen:**

```
Healthy
```

### 🔟 Admin Orders API Test

```bash
# Token al
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"admin123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

# Siparişleri listele
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/admin/orders | head -20
```

**Beklenen:** JSON response ile sipariş listesi

### 1️⃣1️⃣ Kurye API Test

```bash
curl http://localhost:5000/api/courier/orders
```

**Beklenen:** 401 Unauthorized (giriş gerekli - normal)

---

## 🎯 TARAYICIDA TEST

### 1. Ana Sayfa

```
https://golkoygurme.com.tr
```

- ✅ Ürünler gösteriliyor
- ✅ Sepete ekleme çalışıyor

### 2. Admin Paneli

```
https://golkoygurme.com.tr/admin
```

**Login:** admin@admin.com / admin123

**Test Edilecekler:**

- ✅ Giriş başarılı
- ✅ Siparişler sayfası açılıyor (`/admin/orders`)
- ✅ Sipariş durumu dropdown'ları var
- ✅ "Hazırlanıyor" butonuna bas → durum değişiyor mu?
- ✅ "Hazır" butonuna bas → durum değişiyor mu?
- ✅ Modal açılıyor ve ortalı mı?

### 3. Kurye Paneli (Varsa)

```
https://golkoygurme.com.tr/courier
```

### 4. Mağaza Görevlisi Paneli (Varsa)

```
https://golkoygurme.com.tr/store
```

---

## 🛠️ SORUN GİDERME

### ❌ Sipariş Durumu Güncellenmiyor

**Hata Senaryosu:** Admin panelde "Hazırlanıyor" butonuna basıldığında durum değişmiyor.

**Çözüm 1: Backend Log Kontrol**

```bash
docker logs ecommerce-api-prod | grep -i "status"
```

**Çözüm 2: Browser Console Log Kontrol**
Tarayıcıda F12 → Console → Ne hata var?

**Çözüm 3: API Manuel Test**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"admin123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

# Sipariş durumunu güncelle
curl -X PUT http://localhost:5000/api/admin/orders/1011/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status":"preparing"}'

# Kontrol et
curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/api/admin/orders/1011 | grep status
```

### ❌ 404 Not Found Hatası

**Senaryo:** `/api/courier/orders` → 404

**Kontrol 1: Backend Route'ları Listele**

```bash
docker exec -it ecommerce-api-prod dotnet --version
```

**Kontrol 2: Nginx Config Kontrol**

```bash
cat /etc/nginx/sites-available/golkoygurme | grep location
```

**Kontrol 3: Backend'e Direkt Erişim Test**

```bash
# Nginx'i bypass et, direkt backend'e git
curl http://localhost:5000/api/courier/orders
```

### ❌ Frontend Build Hatası

**Senaryo:** Frontend container başlamıyor

**Log Kontrol:**

```bash
docker logs ecommerce-frontend-prod
```

**Çözüm: Frontend'i manuel build et**

```bash
cd /home/eticaret/frontend
docker build -t ecommerce-frontend-prod .
```

### ❌ Database Bağlantı Hatası

**Senaryo:** Backend "Cannot connect to SQL Server"

**Çözüm 1: SQL Container Kontrolü**

```bash
docker ps | grep sql
docker logs ecommerce-sql-prod
```

**Çözüm 2: Connection String Kontrol**

```bash
docker exec -it ecommerce-api-prod env | grep ConnectionStrings
```

---

## 📊 DEPLOY SONRASI KONTROL LİSTESİ

- [ ] **Backend API:** `curl http://localhost:5000/health` → Healthy
- [ ] **Frontend:** `https://golkoygurme.com.tr` → Açılıyor
- [ ] **Admin Login:** `https://golkoygurme.com.tr/admin` → Giriş yapılıyor
- [ ] **Sipariş Listesi:** `/admin/orders` → Siparişler gösteriliyor
- [ ] **Durum Değiştir:** "Hazırlanıyor" butonuna bas → Durum değişiyor
- [ ] **Modal:** Sipariş detayına tıkla → Modal ortalanmış mı?
- [ ] **Kurye API:** `/api/courier/orders` → 401 (giriş gerekli - normal)
- [ ] **Database:** Eski veriler var mı? → Ürünler, siparişler korunmuş mu?
- [ ] **Görseller:** `/uploads/*` → Ürün resimleri yükleniyor mu?

---

## 🎉 BAŞARILI DEPLOY SONRASI

### Yapılacaklar:

1. **Test Siparişi:** Siteden bir test siparişi ver
2. **Admin Onay:** Admin panelden siparişi onayla
3. **Durum Takip:** Durumu adım adım ilerlet (Preparing → Ready → Assigned → Delivered)
4. **SignalR Test:** Bildirimler gerçek zamanlı geliyor mu?

### Git Commit (Opsiyonel):

```bash
# Sunucuda değişiklik yapmadınız, sadece pull yaptınız
# Bu yüzden commit'e gerek yok
```

---

## 📝 YAPILAN DEĞİŞİKLİKLER ÖZET

### Backend (`OrderManager.cs`):

1. `Enum.TryParse` case-insensitive yapıldı (`ignoreCase: true`)
2. `NormalizeStatusString()` metodu eklendi:
   - `out_for_delivery` → `OutForDelivery`
   - `picked_up` → `PickedUp`
   - vs.
3. `AllowedTransitions` güncellendi:
   - `Pending → Preparing` izni eklendi
   - `Paid → Preparing` izni eklendi

### Frontend (`AdminOrders.jsx`):

1. Modal ortalandı (`style={{ maxWidth: '500px', margin: 'auto' }}`)
2. `updateOrderStatus` fonksiyonu iyileştirildi:
   - Console log'ları eklendi
   - Error handling geliştirildi
   - Selected order state güncelleniyor

### Sonuç:

- ✅ Pending → Preparing geçişi artık çalışıyor
- ✅ Küçük harf status değerleri kabul ediliyor
- ✅ Snake_case değerler (out_for_delivery) normalize ediliyor
- ✅ Modal ortalanmış durumda

---

**🚀 DEPLOY BAŞARIYLA TAMAMLANINCA BU DOSYAYI SİLEBİLİRSİNİZ! 🚀**
