# 🔍 LOCALHOST 3000 vs 3001 FARK ANALIZ VE ÇÖZÜM

## ❌ PROBLEM
- ✅ localhost:3000 = Kategoriler görünüyor
- ❌ localhost:3001 = Kategoriler gözükmüyor
- ❌ Sunucu 31.186.24.78:3000 = Kategoriler gözükmüyor

---

## 🔎 ROOT CAUSE (Asıl Sebep)

### 1. .env Dosyası Yanlış
```env
# ❌ YANLIŞ (.env dosyasında)
REACT_APP_API_URL=https://golkoygurme.com.tr/api

# ✅ DOĞRU (Yerel dev için)
REACT_APP_API_URL=http://localhost:5000/api
```

### 2. Port 3001'de Ne Oluyor?
- Port 3001'de `ecommerce/frontend` (eski kod) çalışıyor
- Port 3000'de `frontend` (doğru kod) çalışıyor
- `ecommerce/` klasörü SİLİNDİ ama sunucudaki eski uygulamalar hala çalışıyor

### 3. Sunucuda Niye Gözükmüyor?
- Docker build sırasında `.env.production` kullanılıyor
- `.env.production` hala `https://golkoygurme.com.tr/api` gösteriyor
- Sunucuda API'nin gerçek URL'si olması gerekir

---

## ✅ ÇÖZÜM

### ADIM 1: Yerel Development İçin .env.development Oluştur

```bash
cd ~/eticaret/frontend

# .env.development dosyası oluştur
cat > .env.development << 'EOF'
REACT_APP_API_URL=http://localhost:5000/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
REACT_APP_AUTH_ENABLED=false
EOF
```

**Dosyada ne olmalı**:
```
localhost:3000     → http://localhost:5000/api
localhost:3001     → http://localhost:5000/api (aynı API)
localhost:8000     → http://localhost:5000/api (aynı API)
```

---

### ADIM 2: Sunucu İçin .env.production Kontrol Et

```bash
cd ~/eticaret/frontend

# .env.production'da ne var kontrol et
cat .env.production
```

**Dosyada olması gereken** (sunucuya göre):

#### SEÇENEK A: Sunucu IP Kullanıyorsa
```env
REACT_APP_API_URL=http://31.186.24.78:5000/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
```

#### SEÇENEK B: Domain Kullanıyorsa
```env
REACT_APP_API_URL=https://golkoygurme.com.tr/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
```

#### SEÇENEK C: Docker Container'da (Önerilen)
```env
REACT_APP_API_URL=http://api:5000/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
```

---

### ADIM 3: Frontend Rebuild et (Yerel)

```bash
cd ~/eticaret/frontend

# node_modules temizle
rm -rf node_modules

# Yeniden install et
npm install --legacy-peer-deps

# Development başlat (.env.development kullanacak)
npm start

# Tarayıcıda aç
http://localhost:3000
# ✅ Kategoriler görülmeli!
```

---

### ADIM 4: npm start port seçimine dikkat

```bash
# Port 3000'de başlatmak için
npm start

# Eğer port 3000 meşgulse, başka port öner
# Output'ta göreceksin:
# You can now view frontend in the browser.
# Local:            http://localhost:3000 (veya 3001, 3002...)
```

---

## 🖥️ SUNUCU ÇÖZÜMÜ

### Adım 1: Sunucuda .env.production Düzelt

```bash
ssh huseyinadm@31.186.24.78
cd ~/eticaret/frontend

# Mevcut kontrol et
cat .env.production

# Düzelt (örnek - IP tabanlı)
cat > .env.production << 'EOF'
REACT_APP_API_URL=http://31.186.24.78:5000/api
REACT_APP_BACKEND_ENABLED=true
REACT_APP_USE_MOCK_DATA=false
EOF
```

### Adım 2: Frontend Rebuild

```bash
cd ~/eticaret

# Docker rebuild (yeni .env.production ile)
docker-compose -f docker-compose.prod.yml build --no-cache frontend

# Konteyner başlat
docker-compose -f docker-compose.prod.yml up -d frontend

# Log'ları izle
docker-compose -f docker-compose.prod.yml logs -f frontend
```

### Adım 3: Browser Cache Temizle

```bash
# Tarayıcıda
1. Ctrl+Shift+Delete (DevTools Cache Temizle)
2. http://31.186.24.78:3000
3. Kategoriler görülmeli!
```

---

## 📊 ENV VARİYATÖRLERİ TABLOSU

| Ortam | Dosya | API_URL | Port | Kullanım |
|-------|-------|---------|------|----------|
| Dev (local) | .env.development | http://localhost:5000/api | 3000 | `npm start` |
| Build (local) | .env | http://localhost:5000/api | 3000 | `npm run build` |
| Prod (server) | .env.production | http://31.186.24.78:5000/api | 3000 (Docker) | `docker build` |

---

## 🔧 HIZLI KONTROL KOMANDLARı

### Hangi API URL kullanılıyor kontrol et?

```bash
# Browser DevTools > Console'da çalıştır
fetch('/api/categories')
  .then(r => r.json())
  .then(d => console.log(d))
  .catch(e => console.error(e))
```

### Network sekmesinde API URL'sini görmek için

```javascript
// Browser DevTools > Network sekmesi
// Kategorileri load ettiğinde göreceksin:
GET http://localhost:5000/api/categories  // ✅ DOĞRU
GET https://golkoygurme.com.tr/api/categories  // ❌ YANLIŞ
GET http://31.186.24.78:5000/api/categories  // ✅ SUNUCUDA DOĞRU
```

---

## 🎯 ÖZET

### Localhost 3000 vs 3001 Farkı:
- **3000**: `frontend/` (doğru kod) + `.env.development` (http://localhost:5000/api)
- **3001**: `ecommerce/frontend/` (silindi) + `.env.production` (production URL)

### Niye Kategoriler Gözükmüyor?
- API URL yanlış (production URL gösteriyor yerine development URL göstermeli)
- Ya da API'nin gerçek URL'si yanlış

### Çözüm:
1. `.env.development` oluştur
2. `npm install --legacy-peer-deps`
3. `npm start` (port 3000'de başlayacak)
4. Kategoriler görünecek ✅

### Sunucu İçin:
1. `.env.production` düzelt → `http://31.186.24.78:5000/api`
2. `docker build --no-cache frontend`
3. `docker-compose up -d frontend`
4. Kategoriler görünecek ✅

---

## 🚨 KAÇINILMASI GEREKEN HATALAR

❌ `.env` dosyası production URL'si gösteriyor (localda çalışırken)
❌ `npm start` yaparken `.env.production` kullanılıyor
❌ Docker build sırasında `.env` dosyası yazılmıyor (build args kullanılmalı)
❌ Browser cache'i temizlemeden test etmek

---

**Çözüm**: `frontend/.env.development` dosyası oluşturuldu ✅
