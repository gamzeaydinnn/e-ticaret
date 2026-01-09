# 🔍 SUNUCU TEST KOMUTLARI - ÜRÜN/KATEGORİ/POSTER GÖRÜNMEME SORUNU

## 📊 API SAĞLIĞI KONTROL

### Test 1: API Health Check
```bash
curl http://localhost:5000/api/health
```
**Beklenen:** `{"status":"Healthy"}` veya 200 OK

### Test 2: Kategoriler Yükleniyor mu?
```bash
curl http://localhost:5000/api/categories
```
**Beklenen:** JSON dizi (7+ kategori)

### Test 3: Ürünler Yükleniyor mu?
```bash
curl http://localhost:5000/api/products
```
**Beklenen:** JSON dizi (13+ ürün)

### Test 4: Posterler (Bannerlar) Yükleniyor mu?
```bash
curl http://localhost:5000/api/banners
```
**Beklenen:** JSON dizi (0 veya daha fazla banner)

### Test 5: Tüm Kategoriler (Admin)
```bash
curl http://localhost:5000/api/admin/categories
```
**Beklenen:** JSON dizi

---

## 🌐 FRONTEND KONTROL

### Test 6: Frontend Çalışıyor mu?
```bash
curl -I http://localhost:3000
```
**Beklenen:** `HTTP/1.1 200 OK` veya `HTTP/1.1 301`

### Test 7: Frontend HTML'ini İndir
```bash
curl http://localhost:3000 | head -50
```
**Beklenen:** HTML içeriği, `<script>` tagları

### Test 8: Frontend Asset'leri Kontrol
```bash
curl -I http://localhost:3000/static/js/main.js
```
**Beklenen:** 200 OK

---

## 🔗 CORS KONTROL

### Test 9: CORS Header'ları Kontrol Et
```bash
curl -I -H "Origin: https://golkoygurme.com.tr" http://localhost:5000/api/categories
```
**Beklenen:** `Access-Control-Allow-Origin` header'ı gözükmeli

### Test 10: CORS Preflight Kontrol
```bash
curl -X OPTIONS http://localhost:5000/api/categories \
  -H "Origin: https://golkoygurme.com.tr" \
  -H "Access-Control-Request-Method: GET" \
  -v
```
**Beklenen:** 200 OK ve CORS header'ları

---

## 🗄️ VERITABANI KONTROL

### Test 11: Ürünleri Sayarak Kontrol Et
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) as [Ürün Sayısı] FROM ECommerceDb.dbo.Products"
```
**Beklenen:** 13 sonuç

### Test 12: Kategorileri Kontrol Et
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) as [Kategori] FROM ECommerceDb.dbo.Categories"
```
**Beklenen:** 7+ kategori

### Test 13: Posterları Kontrol Et
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) as [Banner] FROM ECommerceDb.dbo.Banners"
```
**Beklenen:** 0 veya daha fazla

### Test 14: Kategori Detayları
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT id, name, slug FROM ECommerceDb.dbo.Categories ORDER BY id"
```
**Beklenen:** Kategori listesi

---

## 🐳 CONTAINER DURUMU

### Test 15: Container Logları (API)
```bash
docker-compose -f docker-compose.prod.yml logs api --tail=30
```
**Beklenen:** Hata yoktur, info logları

### Test 16: Container Logları (Frontend)
```bash
docker-compose -f docker-compose.prod.yml logs frontend --tail=30
```
**Beklenen:** Server başlatıldı mesajı

### Test 17: Container Kaynak Kullanımı
```bash
docker stats --no-stream
```
**Beklenen:** Tüm container'lar aktif

### Test 18: Network Kontrol
```bash
docker network inspect eticaret_ecommerce-network
```
**Beklenen:** 3 container connected

---

## 🧪 INTEGRATION TEST

### Test 19: Frontend'den API'ye İstek (Simüle)
```bash
curl -X GET http://localhost:3000/api/categories \
  -H "Accept: application/json"
```
**Beklenen:** 404 (frontend'den değil, direkt API'den)

### Test 20: API Endpoint'i Node Üzerinden Test Et
```bash
docker exec ecommerce-frontend-prod curl -s http://ecommerce-api-prod:5000/api/categories | head -c 200
```
**Beklenen:** JSON başlangıcı

---

## 🚨 SORUN GİDERME KOMUTLARI

### Problem: API 404 Dönüyor
```bash
# API endpoint'leri listele
docker exec ecommerce-api-prod curl -s http://localhost:5000/swagger/v1/swagger.json | grep "\"paths\"" | head -20
```

### Problem: Frontend Boş Yükleniyor
```bash
# Build file'ını kontrol et
docker exec ecommerce-frontend-prod ls -la /usr/share/nginx/html/
```

### Problem: CORS Hatası
```bash
# .env dosyasını kontrol et
docker exec ecommerce-api-prod env | grep CORS
```

### Problem: Database Connection
```bash
# Connection string kontrol
docker exec ecommerce-api-prod env | grep Connection
```

---

## 📋 HIZLI TEST SCRIPT

Tüm testleri sırayla çalıştırmak için:

```bash
#!/bin/bash

echo "🔍 API Kontrol..."
curl -s http://localhost:5000/api/health | jq .

echo -e "\n🔍 Kategoriler..."
curl -s http://localhost:5000/api/categories | jq '.[0:2]'

echo -e "\n🔍 Ürünler..."
curl -s http://localhost:5000/api/products | jq '.[0:2]'

echo -e "\n🔍 Frontend..."
curl -s -I http://localhost:3000 | grep "HTTP"

echo -e "\n🔍 Veritabanı Ürün Sayısı..."
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products"

echo -e "\n✅ Testler tamamlandı!"
```

Sunucuda çalıştırmak için:
```bash
bash /home/huseyinadm/eticaret/test.sh
```

---

## 🎯 OLASI SORUNLAR VE ÇÖZÜMLER

| Sorun | Test Komutu | Çözüm |
|-------|------------|-------|
| API 404 | `curl http://localhost:5000/api/categories` | Veritabanı seed'lenmiş mi kontrol et |
| Frontend boş | `curl http://localhost:3000` | Build'i kontrol et |
| CORS hatası | Browser console | .env CORS ayarlarını kontrol et |
| Veri yüklenmedi | SQL sorgusu | Migration loglarını kontrol et |
| API timeout | `curl -v http://localhost:5000/api/health` | Container'ın resource'ı yeterli mi? |

---

**Şimdi test komutlarını çalıştırın ve sonuçları paylaşın!** 🚀
