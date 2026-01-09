# 🎯 ÜRÜN/KATEGORİ/POSTER GÖRÜNMEME SORUNU - ÇÖZÜM

## 🔴 SORUN ANALİZİ

API test sonucu:
- ✅ 13 ürün veritabanında var
- ✅ 7+ kategori veritabanında var
- ✅ API `/api/categories` çalışıyor
- ✅ API `/api/products` çalışıyor
- ❌ Frontend'de görünmüyor

**ROOT CAUSE:** Frontend API'ye istek yapamıyor veya istek başarısız oluyor.

---

## 🔧 ÖLÇELECEKLERİNİZ (SUNUCUDA)

### ADIM 1: Frontend Container'ını Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml logs frontend --tail=50
```
**Arası:** "Listening on" veya hata mesajı

### ADIM 2: Frontend'in Nginx Config'ini Kontrol Et
```bash
docker exec ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf
```
**Arası:** API proxy ayarları `/api` -> `http://ecommerce-api-prod:5000`

### ADIM 3: Frontend Build'ini Kontrol Et
```bash
docker exec ecommerce-frontend-prod ls -la /usr/share/nginx/html/
```
**Arası:** `index.html` ve `static/` klasörü olmalı

### ADIM 4: Frontend Index.html'ini Kontrol Et
```bash
docker exec ecommerce-frontend-prod head -20 /usr/share/nginx/html/index.html
```
**Arası:** React app mount point görünmeli

### ADIM 5: API URL Ortamını Kontrol Et
```bash
docker exec ecommerce-frontend-prod env | grep REACT_APP
```
**Beklenen:** `REACT_APP_API_URL=https://golkoygurme.com.tr/api`

---

## 🔨 TEMEL SORUNLAR VE FİXLER

### SORUN 1: Frontend .env.production Boş API URL
**Bulma:**
```bash
docker exec ecommerce-frontend-prod env | grep REACT_APP_API_URL
```

**Çözüm (Yerel Makinede - Development):**
```bash
# Yerel makinede
echo "REACT_APP_API_URL=https://golkoygurme.com.tr/api" >> frontend/.env.production
git add frontend/.env.production
git commit -m "Fix: Add production API URL"
git push origin main
```

Sonra sunucuda:
```bash
cd /home/huseyinadm/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml build frontend --no-cache
docker-compose -f docker-compose.prod.yml up -d frontend
```

---

### SORUN 2: Nginx Proxy Ayarı Yanlış
**Kontrol:**
```bash
docker exec ecommerce-frontend-prod curl -s http://localhost:3000/api/categories
```

**Beklenen:** Hata değil, kategoriler dönecek

**Eğer hata ise:**
```bash
# Nginx config'i düzelt
docker exec ecommerce-frontend-prod cat > /etc/nginx/conf.d/default.conf << 'EOF'
upstream api {
    server ecommerce-api-prod:5000;
}

server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri /index.html;
    }

    location /api {
        proxy_pass http://api;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
EOF

# Nginx'i yeniden başlat
docker exec ecommerce-frontend-prod nginx -s reload
```

---

### SORUN 3: Frontend Build'inde API URL Sabit Kodlanmış
**Kontrol (Yerel):**
```bash
grep -r "localhost:5153\|localhost:7221" frontend/src/
```

**Eğer bulursan, sil ve environment variable kullan:**
```bash
# Yerel makinede apiConfig.js'yi kontrol et
cat frontend/src/config/apiConfig.js

# Eğer hardcoded URL varsa, bunu düzelt:
# const baseUrl = process.env.REACT_APP_API_URL || "https://golkoygurme.com.tr/api"
```

---

### SORUN 4: CORS Hatası (Browser Console'da Görünecek)
**Test:**
```bash
curl -v -H "Origin: http://localhost:3000" http://localhost:5000/api/categories
```

**Beklenen:** `Access-Control-Allow-Origin` header'ı

**Eğer yoksa, .env dosyasını kontrol et:**
```bash
docker exec ecommerce-api-prod env | grep CORS
```

**Düzelt:**
```bash
# Sunucuda .env'yi güncelle
cd /home/huseyinadm/eticaret
echo "CORS__ALLOWEDORIGINS__0=http://localhost:3000" >> .env
echo "CORS__ALLOWEDORIGINS__1=https://golkoygurme.com.tr" >> .env

docker-compose -f docker-compose.prod.yml restart api
```

---

## 🚀 HIZLI FIX (TÜM BUNLAR BIRDEN)

Sunucuda çalıştırın:

```bash
cd /home/huseyinadm/eticaret

# 1. Kodu güncelle
git pull origin main

# 2. Frontend'i yeniden build et
docker-compose -f docker-compose.prod.yml build frontend --no-cache

# 3. Container'ları başlat
docker-compose -f docker-compose.prod.yml up -d

# 4. Wait for stabilization
sleep 15

# 5. Test et
curl http://localhost:3000/
curl http://localhost:5000/api/categories

# 6. Log'ları kontrol et
docker-compose -f docker-compose.prod.yml logs frontend --tail=20
docker-compose -f docker-compose.prod.yml logs api --tail=20
```

---

## 📋 KONTROL LİSTESİ

- [ ] Frontend container çalışıyor mı? (`docker ps`)
- [ ] API URL environment variable'ı ayarlanmış mı? (`env | grep REACT_APP`)
- [ ] Frontend build file'ları var mı? (`ls /usr/share/nginx/html`)
- [ ] Nginx proxy'si yapılandırılmış mı? (`curl localhost:3000/api/categories`)
- [ ] CORS header'ları gözüküyor mü? (`curl -v`)
- [ ] Veritabanı veri dolu mu? (SQL sorgusu)
- [ ] API port 5000'de açık mı? (`curl localhost:5000/api/health`)

---

## 🧪 BROWSER CONSOLE KONTROL (Frontend'de)

Tarayıcıda F12 açtıktan sonra Console sekmesinde:

```javascript
// API URL'yi kontrol et
fetch('/api/categories')
  .then(r => r.json())
  .then(d => console.log(d))
```

**Beklenen:** Kategoriler konsola yazılacak

**Hata alırsan:**
- 404 → API endpoint yok
- 401 → Authorization problemi
- CORS error → CORS ayarları yanlış
- Network error → Bağlantı yok

---

## 🎯 SONUÇ

**Eğer hepsi test edip sorun çözmez ise:**

Sunucuda test komutlarını çalıştırın ve sonuçlarını paylaşın:
1. `docker-compose ps`
2. `docker-compose logs frontend --tail=50`
3. `curl http://localhost:3000`
4. `curl http://localhost:5000/api/categories`
5. `curl http://localhost:3000/api/categories`

**Sonra eksik olan şeyi bulabilirim!** 🚀
