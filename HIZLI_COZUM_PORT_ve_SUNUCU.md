# ⚡ HIZLI ÇÖZÜM - PORT 3001 VE SUNUCU KATEGORİLER SORUNU

## 🔴 SORUN

- Port 3000: ✅ Kategoriler var
- Port 3001: ❌ Kategoriler yok
- Sunucu: ❌ Kategoriler yok
- **Hata mesajı yok** ama kategoriler boş

---

## 💡 SEBEP (KISA)

**Port 3001** ve **3000** aynı kodu çalıştırıyor **ama**:

- Browser cache farklı (port bazlı)
- Process.env farklı yüklenmiş olabilir

**Sunucu** farklı çünkü:

- Docker build sırasında `.env` yanlış değer ile build olmuş
- `REACT_APP_API_URL` build-time variable (runtime değişmez!)

---

## ✅ ÇÖZÜM 1: PORT 3001 İÇİN (YEREL)

### Adım 1: Backend Başlat

```powershell
# Terminal 1
cd C:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API
dotnet run
```

**Görmek istediğin**:

```
Now listening on: http://localhost:5000
Application started.
```

### Adım 2: Frontend Temiz Başlat

```powershell
# Terminal 2
cd C:\Users\GAMZE\Desktop\eticaret\frontend

# Temizle
Remove-Item -Recurse -Force node_modules
Remove-Item -Force package-lock.json

# Install
npm install --legacy-peer-deps

# Port 3001'de başlat
$env:PORT=3001
npm start
```

### Adım 3: Browser Cache Temizle

```
1. http://localhost:3001 aç
2. F12 (DevTools)
3. Application tab > Clear Storage > Clear site data
4. Ctrl + Shift + R (Hard refresh)
```

### Adım 4: Console Test

Browser Console'da:

```javascript
console.log("API URL:", process.env.REACT_APP_API_URL);
// Çıktı: http://localhost:5000/api olmalı
```

---

## ✅ ÇÖZÜM 2: SUNUCU İÇİN

```bash
# 1. Sunucuya bağlan
ssh huseyinadm@31.186.24.78

# 2. Klasöre git
cd ~/eticaret

# 3. .env dosyası oluştur
cat > .env << 'EOF'
REACT_APP_API_URL=http://api:5000/api
DB_PASSWORD=ECom1234
DB_PORT=1435
FRONTEND_PORT=3000
ASPNETCORE_ENVIRONMENT=Production
EOF

# 4. Eski container'ları sil
docker stop ecommerce-frontend-prod ecommerce-api-prod ecommerce-sql-prod
docker rm ecommerce-frontend-prod ecommerce-api-prod ecommerce-sql-prod

# 5. Temiz build
docker system prune -af
docker-compose -f docker-compose.prod.yml build --no-cache

# 6. Başlat
docker-compose -f docker-compose.prod.yml up -d

# 7. 60 saniye bekle
sleep 60

# 8. API testi
curl http://localhost:5000/api/categories

# 9. Frontend testi
curl http://localhost:3000

# 10. Kontrol
docker-compose -f docker-compose.prod.yml ps
docker-compose -f docker-compose.prod.yml logs frontend | grep -i error
```

---

## 🔍 DEBUG: NEDEN GÖZÜKMÜYOR?

### Browser DevTools > Network Sekmesi

**Port 3001'de göreceksin**:

```
GET http://localhost:5000/api/categories
Status: 200 OK (✅ başarılı)
Response: [{"id":1,"name":"Elektronik",...}]
```

Eğer:

- ❌ `GET https://golkoygurme.com.tr/...` → Yanlış URL
- ❌ `Failed to fetch` → Backend çalışmıyor
- ❌ `CORS error` → Backend CORS ayarı hatalı

### Console'da Test

```javascript
// 1. API URL kontrolü
console.log("REACT_APP_API_URL:", process.env.REACT_APP_API_URL);

// 2. Direkt fetch testi
fetch("http://localhost:5000/api/categories")
  .then((r) => r.json())
  .then((data) => {
    console.log("✅ Kategoriler:", data);
    console.log("📊 Sayı:", data.length);
  })
  .catch((err) => console.error("❌ Hata:", err));
```

### React State Kontrolü

Header component'te categories state'i boş olabilir:

**App.js'e geçici log ekle** (satır 96):

```javascript
React.useEffect(() => {
  const loadCategories = async () => {
    try {
      const cats = await categoryServiceReal.getActive();
      console.log("🔍 Yüklenen kategoriler:", cats); // BU SATIRI EKLE
      console.log("📊 Kategori sayısı:", cats?.length || 0); // BU SATIRI EKLE
      setCategories(cats || []);
    } catch (err) {
      console.error("❌ Hata:", err);
    }
  };
  loadCategories();
}, []);
```

Kaydet ve console'da çıktıya bak.

---

## 🎯 EN HIZLI YOL (COPY-PASTE)

### Yerel PC (Port 3001)

```powershell
# Terminal 1: Backend
cd C:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API ; dotnet run

# Terminal 2: Frontend
cd C:\Users\GAMZE\Desktop\eticaret\frontend ; Remove-Item -Recurse -Force node_modules ; npm install --legacy-peer-deps ; $env:PORT=3001 ; npm start
```

### Sunucu

```bash
ssh huseyinadm@31.186.24.78
cd ~/eticaret
cat > .env << 'EOF'
REACT_APP_API_URL=http://api:5000/api
DB_PASSWORD=ECom1234
FRONTEND_PORT=3000
EOF
docker stop $(docker ps -q)
docker rm $(docker ps -aq)
docker system prune -af
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
sleep 60
curl http://localhost:5000/api/categories
```

---

## 📊 ÖZET TABLO

| Durum   | Port 3000          | Port 3001          | Sunucu             | Sebep            |
| ------- | ------------------ | ------------------ | ------------------ | ---------------- |
| Cache   | ✅ Temiz           | ❌ Eski            | ❌ Eski            | Browser cache    |
| API URL | ✅ Doğru           | ❓ Kontrol et      | ❌ Yanlış          | .env yüklenmemiş |
| Backend | ✅ Çalışıyor       | ✅ Aynı            | ❓ Kontrol et      | -                |
| Sonuç   | ✅ Kategoriler var | ❌ Kategoriler yok | ❌ Kategoriler yok | -                |

---

## ⚠️ NEDEN 2 PORT FARKLI DAVRANIR?

### React Environment Variables = BUILD TIME!

```javascript
// Build sırasında değer enjekte edilir
const API_URL = process.env.REACT_APP_API_URL;

// Bu kod runtime'da şuna dönüşür:
const API_URL = "http://localhost:5000/api"; // HARDCODED!
```

**Yani**:

- Port 3000 → `.env` ile build oldu → `http://localhost:5000/api`
- Port 3001 → Aynı build kullanıyor (node_modules'de cached)
- Ama browser cache farklı → Eski production URL'si cached olabilir!

**Çözüm**: `node_modules` temizle + yeniden build et!

---

**TL;DR**:

1. Backend'i çalıştır
2. Frontend node_modules'ü sil
3. npm install --legacy-peer-deps
4. npm start
5. Browser cache temizle
6. ✅ Çalışacak!
