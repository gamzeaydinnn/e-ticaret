# 🖥️ SUNUCU KOMUTLARI

## 📌 Bağlantı Bilgileri
- **IP:** 31.186.24.78
- **Port:** 22
- **User:** huseyinadm
- **Pass:** Passwd1122FFGG

---

## 🔴 ACIL KONTROL KOMUTLARI

### 1️⃣ Container Durumunu Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```

### 2️⃣ Frontend Loglarını Görüntüle
```bash
docker-compose -f docker-compose.prod.yml logs frontend
```

### 3️⃣ API Loglarını Görüntüle
```bash
docker-compose -f docker-compose.prod.yml logs api
```

### 4️⃣ Tüm Container Loglarını Görüntüle
```bash
docker-compose -f docker-compose.prod.yml logs
```

---

## 🚀 BAŞLAT/DURDUR KOMUTLARI

### ⚡ Tüm Servisleri Başlat
```bash
cd ~/eticaret
docker-compose -f docker-compose.prod.yml up -d
```

### ⚡ Tüm Servisleri Yeniden Başlat
```bash
docker-compose -f docker-compose.prod.yml restart
```

### ⚡ Tüm Servisleri Durdur
```bash
docker-compose -f docker-compose.prod.yml down
```

### ⚡ Servisleri Yeniden Oluştur ve Başlat
```bash
docker-compose -f docker-compose.prod.yml up -d --build
```

---

## 🔄 DEPLOYMENT KOMUTLARI

### Son Değişiklikleri Çek
```bash
cd ~/eticaret
git pull origin main
```

### Yalnız Frontend'i Yeniden Oluştur
```bash
docker-compose -f docker-compose.prod.yml build frontend
docker-compose -f docker-compose.prod.yml up -d frontend
```

### Yalnız API'yi Yeniden Oluştur
```bash
docker-compose -f docker-compose.prod.yml build api
docker-compose -f docker-compose.prod.yml up -d api
```

### Her İkisini Yeniden Oluştur
```bash
docker-compose -f docker-compose.prod.yml build
docker-compose -f docker-compose.prod.yml up -d
```

---

## 🗄️ DATABASE KOMUTLARI

### SQL Server'a Bağlan
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

### Veritabanı Seed Data Yükle
```bash
cat seed-products.sql | docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

### Tabloları Temizle
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "DELETE FROM ECommerceDb.dbo.Products; DELETE FROM ECommerceDb.dbo.Categories;"
```

---

## 📊 DURUM KONTROL KOMUTLARI

### Docker Container'larını Listele
```bash
docker ps -a
```

### Disk Kullanımını Kontrol Et
```bash
df -h
```

### Memory Kullanımını Kontrol Et
```bash
free -h
```

### Sunucu Uptime'ı Kontrol Et
```bash
uptime
```

---

## 🔧 HATA AYIKLAMA KOMUTLARI

### Frontend Container'ının Detaylı Bilgisi
```bash
docker inspect ecommerce-frontend-prod
```

### API Container'ının Detaylı Bilgisi
```bash
docker inspect ecommerce-api-prod
```

### Network Durumunu Kontrol Et
```bash
docker network ls
docker network inspect ecommerce-network
```

### Container İçindeki Dosyaları Görüntüle
```bash
docker exec -it ecommerce-frontend-prod ls -la /usr/share/nginx/html
```

---

## 🧹 TEMİZLEME KOMUTLARI

### Kullanılmayan Image'ları Sil
```bash
docker image prune -a
```

### Kullanılmayan Container'ları Sil
```bash
docker container prune
```

### Volume'leri Sil (⚠️ DİKKAT: VERİ SİLİNECEK)
```bash
docker volume prune
```

---

## 📝 HIZLI REFERANS

| İşlem | Komut |
|-------|-------|
| Container'ları Listele | `docker ps` |
| Log Görüntüle | `docker logs container_name` |
| Container Restart | `docker restart container_name` |
| Container Stop | `docker stop container_name` |
| Container Start | `docker start container_name` |

---

## 💡 İPUÇLARI

1. **Logları Canlı Takip Et:** `docker-compose -f docker-compose.prod.yml logs -f frontend`
2. **Belirli Satır Sayısı Gör:** `docker-compose -f docker-compose.prod.yml logs --tail=50 frontend`
3. **Zaman Damgası Ekle:** `docker-compose -f docker-compose.prod.yml logs -t frontend`

