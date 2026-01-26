# 🐳 Docker Log Kontrol Komutları

## Sunucu Bağlantısı

```bash
# SSH ile sunucuya bağlan
ssh root@31.186.24.78
```

---

## 📋 Temel Log Komutları

### 1. Backend API Logları

```bash
# Son 50 satırı göster
docker logs ecommerce-api-prod | tail -50

# Son 100 satırı göster
docker logs ecommerce-api-prod | tail -100

# Gerçek zamanlı logları izle (canlı)
docker logs -f ecommerce-api-prod

# Son 1 saat içindeki logları göster
docker logs --since 1h ecommerce-api-prod

# Son 30 dakika içindeki logları göster
docker logs --since 30m ecommerce-api-prod
```

### 2. Frontend Logları

```bash
# Frontend container'ı kontrol et
docker logs ecommerce-frontend-prod | tail -50

# Canlı frontend logları
docker logs -f ecommerce-frontend-prod
```

### 3. Database Logları

```bash
# SQL Server logları
docker logs ecommerce-db-prod | tail -50

# Canlı DB logları
docker logs -f ecommerce-db-prod
```

### 4. Nginx Logları

```bash
# Nginx access logları
docker logs ecommerce-nginx-prod | tail -50

# Canlı Nginx logları
docker logs -f ecommerce-nginx-prod
```

---

## 🔍 Gelişmiş Log Arama

### Hata Arama

```bash
# "error" kelimesini ara
docker logs ecommerce-api-prod 2>&1 | grep -i error

# "exception" kelimesini ara
docker logs ecommerce-api-prod 2>&1 | grep -i exception

# Belirli bir tarihe göre ara
docker logs ecommerce-api-prod 2>&1 | grep "2026-01-27"

# Birden fazla kelime ara
docker logs ecommerce-api-prod 2>&1 | grep -E "error|warning|exception"
```

### Log Çıktılarını Dosyaya Kaydet

```bash
# Logları dosyaya kaydet
docker logs ecommerce-api-prod > api-logs.txt 2>&1

# Logları dosyaya ekle (append)
docker logs ecommerce-api-prod >> api-logs.txt 2>&1

# Tüm container loglarını kaydet
docker logs ecommerce-api-prod > api.log && \
docker logs ecommerce-frontend-prod > frontend.log && \
docker logs ecommerce-db-prod > db.log && \
docker logs ecommerce-nginx-prod > nginx.log
```

---

## 🐋 Container Durumu Kontrol

### Çalışan Container'ları Listele

```bash
# Tüm container'ları göster
docker ps -a

# Sadece çalışan container'ları göster
docker ps

# Formatlanmış liste
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### Container Bilgileri

```bash
# Container detaylarını göster
docker inspect ecommerce-api-prod

# Container'ın stats'ini göster (CPU, Memory)
docker stats ecommerce-api-prod

# Tüm container'ların stats'ini göster
docker stats

# Container network bilgileri
docker network inspect bridge
```

---

## 🔄 Tüm Logları Birden Kontrol

### Hızlı Kontrol Script

```bash
# 1. API logları kontrol et
echo "=== API LOGLAR ===" && \
docker logs ecommerce-api-prod 2>&1 | tail -30 && \
echo "" && \

# 2. Frontend logları kontrol et
echo "=== FRONTEND LOGLAR ===" && \
docker logs ecommerce-frontend-prod 2>&1 | tail -30 && \
echo "" && \

# 3. DB logları kontrol et
echo "=== DATABASE LOGLAR ===" && \
docker logs ecommerce-db-prod 2>&1 | tail -20 && \
echo "" && \

# 4. Nginx logları kontrol et
echo "=== NGINX LOGLAR ===" && \
docker logs ecommerce-nginx-prod 2>&1 | tail -20
```

---

## 📊 Hata Teşhis

### 400 Bad Request Hatası

```bash
# API'de 400 hatalarını ara
docker logs ecommerce-api-prod 2>&1 | grep -i "400\|bad request\|validation"

# Nginx'te 400 hatalarını ara
docker logs ecommerce-nginx-prod 2>&1 | grep "400"
```

### Database Bağlantı Sorunu

```bash
# DB connection hatalarını ara
docker logs ecommerce-api-prod 2>&1 | grep -i "connection\|timeout\|database"

# SQL Server loglarında hata ara
docker logs ecommerce-db-prod 2>&1 | grep -i "error\|failed"
```

### Memory/Resource Sorunu

```bash
# Container'ın kullanımını kontrol et
docker stats ecommerce-api-prod --no-stream

# Disk alanını kontrol et
df -h

# Docker disk kullanımı
docker system df
```

---

## 🚀 Deployment Kontrol Checklist

```bash
# 1. Tüm container'lar çalışıyor mu?
docker ps --format "table {{.Names}}\t{{.Status}}"

# 2. API loglarında hata var mı?
docker logs ecommerce-api-prod 2>&1 | tail -50 | grep -i "error"

# 3. Frontend çalışıyor mu?
curl -I http://localhost/

# 4. API çalışıyor mu?
curl -I http://localhost/api/health

# 5. Database bağlantısı sağlam mı?
docker logs ecommerce-api-prod 2>&1 | grep -i "database\|connected"

# 6. Nginx ayağa kalkacak mı?
docker logs ecommerce-nginx-prod 2>&1 | tail -20
```

---

## 💾 Log Dosyaları (Container İçinde)

### API Log Dosyaları

```bash
# API container'ı içinde log dosyalarını ara
docker exec ecommerce-api-prod find /app/logs -name "*.log" 2>/dev/null

# Log dosyasını oku
docker exec ecommerce-api-prod cat /app/logs/latest.log

# Log dosyasını bilgisayarına indir
docker cp ecommerce-api-prod:/app/logs/latest.log ./api-latest.log
```

---

## 🔧 Sorun Giderme

### Container Yeniden Başlat

```bash
# API'yi yeniden başlat
docker restart ecommerce-api-prod

# Tüm servisleri yeniden başlat
docker-compose -f docker-compose.prod.yml restart

# Container'ı tamamen sil ve yeniden oluştur
docker-compose -f docker-compose.prod.yml down && \
docker-compose -f docker-compose.prod.yml up -d
```

### Log Temizle

```bash
# Belirli container'ın loglarını temizle
docker logs ecommerce-api-prod --tail 0 > /dev/null

# Tüm logları temizle
docker system prune -a
```

---

## 📝 Örnek Kontrol Senaryoları

### Kurye oluşturma hatası

```bash
docker logs ecommerce-api-prod 2>&1 | grep -i "courier\|password\|user" | tail -20
```

### Order status hatası

```bash
docker logs ecommerce-api-prod 2>&1 | grep -i "status\|order" | tail -20
```

### SignalR hatası

```bash
docker logs ecommerce-api-prod 2>&1 | grep -i "signalr\|hub\|connection" | tail -20
```

### Database hatası

```bash
docker logs ecommerce-api-prod 2>&1 | grep -i "database\|sql\|connection" | tail -20
docker logs ecommerce-db-prod 2>&1 | tail -30
```

---

## 🎯 Hızlı Referans

| Komut                                     | Açıklama                            |
| ----------------------------------------- | ----------------------------------- |
| `docker ps`                               | Çalışan container'ları listele      |
| `docker logs <name>`                      | Container loglarını göster          |
| `docker logs -f <name>`                   | Canlı logları izle                  |
| `docker logs <name> \| tail -50`          | Son 50 satırı göster                |
| `docker stats <name>`                     | Container kaynak kullanımını göster |
| `docker exec <name> <komut>`              | Container'da komut çalıştır         |
| `docker inspect <name>`                   | Container detaylarını göster        |
| `docker restart <name>`                   | Container'ı yeniden başlat          |
| `docker logs <name> 2>&1 \| grep "error"` | Hata arama                          |
