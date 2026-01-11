# 🔍 PORT 3001 SORUN TESPİT VE ÇÖZÜM

## ❌ PROBLEM

- ✅ localhost:3000 → Kategoriler görünüyor
- ❌ localhost:3001 → Kategoriler gözükmüyor
- ❌ Sunucu 31.186.24.78:3000 → Kategoriler gözükmüyor
- **Hata mesajı YOK** ama kategoriler boş

---

## 🔎 MUHTEMEL SEBEPLER

### 1. **Browser Cache (En Olası)**

Port 3001'de önceki çalıştırmalardan kalan cache olabilir.

**ÇÖZÜM**:

```bash
# Browser'da
1. F12 (DevTools aç)
2. Network sekmesi
3. "Disable cache" işaretle
4. Ctrl + Shift + R (Hard Refresh)
5. Kategorileri kontrol et
```

### 2. **API URL Environment Variable Yüklenmemiş**

React .env dosyası başlangıçta yüklenir, runtime'da değişiklik olmaz.

**ÇÖZÜM**:

```bash
# Terminal'de port 3001 çalışıyorsa DURDUR
# Ctrl+C ile durdur

cd C:\Users\GAMZE\Desktop\eticaret\frontend

# Node_modules temizle
Remove-Item -Recurse -Force node_modules
Remove-Item -Recurse -Force .cache

# Yeniden install
npm install --legacy-peer-deps

# Port 3001'de başlat
$env:PORT=3001
npm start
```

### 3. **Console'da API URL'sini Kontrol Et**

Browser DevTools > Console'da:

```javascript
// Console'a yapıştır ve Enter
console.log("REACT_APP_API_URL:", process.env.REACT_APP_API_URL);

// Kategori fetch testi
fetch("http://localhost:5000/api/categories")
  .then((r) => r.json())
  .then((data) => {
    console.log("Kategoriler:", data);
    console.log("Kategori sayısı:", data.length);
  })
  .catch((err) => console.error("API Hatası:", err));
```

### 4. **API Server Çalışıyor mu?**

Backend sunucusunun çalışıp çalışmadığını kontrol et:

```bash
# Yeni PowerShell terminalinde
cd C:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API

# API'yi başlat
dotnet run

# Görmek istediğin:
# Now listening on: http://localhost:5000
```

**API Test**:

```bash
# Başka bir terminalde
curl http://localhost:5000/api/categories

# Veya browser'da
http://localhost:5000/api/categories
```

---

## ✅ ADIM ADIM ÇÖZÜM

### ADIM 1: Backend API'yi Başlat

```powershell
# Terminal 1
cd C:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API
dotnet run
```

### ADIM 2: Frontend Port 3000'i Durdur

```powershell
# Terminal 2 (port 3000 çalışıyorsa)
# Ctrl+C ile durdur
```

### ADIM 3: Frontend Temizle ve Başlat (Port 3001)

```powershell
# Terminal 2
cd C:\Users\GAMZE\Desktop\eticaret\frontend

# Temizle
Remove-Item -Recurse -Force node_modules, .cache -ErrorAction SilentlyContinue

# Install
npm install --legacy-peer-deps

# Port 3001'de başlat
$env:PORT=3001
npm start
```

### ADIM 4: Browser'ı Temizle

```
1. http://localhost:3001 aç
2. F12 (DevTools)
3. Application sekmesi
4. Clear Storage
5. Ctrl+Shift+R (Hard refresh)
```

### ADIM 5: Console'da Kontrol Et

```javascript
// DevTools > Console
console.log("API URL:", process.env.REACT_APP_API_URL);

// Fetch test
fetch("http://localhost:5000/api/categories")
  .then((r) => r.json())
  .then((d) => console.log("Kategoriler:", d));
```

---

## 🖥️ SUNUCU SORUNU (31.186.24.78:3000)

Sunucuda kategoriler gözükmüyorsa **farklı bir sebep**:

### Kontrol 1: API Çalışıyor mu?

```bash
ssh huseyinadm@31.186.24.78

# API kontrol et
curl http://localhost:5000/api/categories

# Çıktıda JSON array görmeli:
# [{"id":1,"name":"Elektronik",...}]
```

### Kontrol 2: Frontend API URL'si Doğru mu?

```bash
# Sunucuda
cd ~/eticaret

# Container'a gir
docker exec -it ecommerce-frontend-prod /bin/sh

# Environment variable kontrol et
cat /usr/share/nginx/html/static/js/main.*.js | grep -o 'REACT_APP_API_URL[^"]*' | head -1
```

### Kontrol 3: Docker Build Args Kullanıldı mı?

```bash
# Sunucuda rebuild
cd ~/eticaret

# Stop
docker-compose -f docker-compose.prod.yml stop frontend

# .env dosyası oluştur
cat > .env << 'EOF'
REACT_APP_API_URL=http://api:5000/api
DB_PASSWORD=ECom1234
DB_PORT=1435
FRONTEND_PORT=3000
EOF

# Build (build arg'lar .env'den alınır)
docker-compose -f docker-compose.prod.yml build --no-cache frontend

# Start
docker-compose -f docker-compose.prod.yml up -d frontend

# Log izle
docker-compose -f docker-compose.prod.yml logs -f frontend
```

---

## 🎯 EN HIZLI ÇÖZÜM (3 Komut)

### Yerel (localhost:3001)

```powershell
cd C:\Users\GAMZE\Desktop\eticaret\frontend
Remove-Item -Recurse -Force node_modules, .cache -ErrorAction SilentlyContinue
npm install --legacy-peer-deps ; $env:PORT=3001 ; npm start
```

### Sunucu (31.186.24.78:3000)

```bash
ssh huseyinadm@31.186.24.78
cd ~/eticaret
cat > .env << 'EOF'
REACT_APP_API_URL=http://api:5000/api
DB_PASSWORD=ECom1234
FRONTEND_PORT=3000
EOF
docker-compose -f docker-compose.prod.yml build --no-cache frontend
docker-compose -f docker-compose.prod.yml up -d frontend
```

---

## 🔧 DEBUG: Network Sekmesinde Göreceksin

| Durum     | Network'te Göreceğin                                         | Sonuç                         |
| --------- | ------------------------------------------------------------ | ----------------------------- |
| ✅ DOĞRU  | `GET http://localhost:5000/api/categories → 200 OK`          | Kategoriler görünür           |
| ❌ YANLIŞ | `GET https://golkoygurme.com.tr/api/categories → CORS error` | Kategoriler yok               |
| ❌ YANLIŞ | `GET http://localhost:5000/api/categories → Failed`          | Backend çalışmıyor            |
| ❌ YANLIŞ | Hiçbir istek yok                                             | Frontend API çağrısı yapmıyor |

---

## 🚨 ÖZEL DURUM: HİÇBİR HATA YOK AMA KATEGORİLER YOK

Eğer:

- Console'da hata yok
- Network'te 200 OK
- Ama kategoriler gözükmüyor

**O zaman**:

```javascript
// DevTools > Console
// React state'i kontrol et
// Header component içinde categories state'ine bak

// App.js içinde console.log ekle (geçici)
// Satır 93-107 arası
React.useEffect(() => {
  const loadCategories = async () => {
    try {
      const cats = await categoryServiceReal.getActive();
      console.log('✅ Kategoriler yüklendi:', cats);  // BU SATIRI EKLE
      setCategories(cats || []);
    } catch (err) {
      console.error('❌ Kategoriler yüklenemedi:', err);  // BU SATIRI EKLE
    }
  };
  loadCategories();
  ...
}, []);
```

Kaydet ve tekrar test et. Console'da kategorileri göreceksin.

---

**SON ÇARE**: Git'ten temiz başlat

```powershell
cd C:\Users\GAMZE\Desktop\eticaret
git pull origin main
cd frontend
Remove-Item -Recurse -Force node_modules
npm install --legacy-peer-deps
npm start
```
