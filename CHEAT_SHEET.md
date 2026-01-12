# ⚡ DEPLOYMENT CHEAT SHEET - HIZLI REFERANS

**Sunucu:** 31.186.24.78 | **User:** huseyinadm  
**Bölge:** Türkiye | **Port:** 22 (SSH)

---

## 🔌 BAĞLANMA

```bash
# Windows PowerShell'de
ssh huseyinadm@31.186.24.78

# Linux/Mac'te
ssh -p 22 huseyinadm@31.186.24.78
```

---

## 📁 DİZİN YAPISI (Sunucuda)

```
/root/eticaret/
├── docker-compose.prod.yml      # Compose konfigürasyonu
├── src/                          # Backend source code
│   └── ECommerce.API/
│       ├── Dockerfile
│       └── appsettings.json
├── frontend/
│   ├── Dockerfile
│   ├── build/                    # Production build
│   └── nginx/
├── logs/                         # Uygulama logları
├── uploads/                      # Banner ve resimler
└── backups/                      # Database backup'ları
```

---

## 🐳 DOCKER KOMUTLARI

### Container Yönetimi

```bash
# Tüm servisleri başlat
docker-compose -f docker-compose.prod.yml up -d

# Tüm servisleri durdur
docker-compose -f docker-compose.prod.yml down

# Container durumlarını göster
docker-compose -f docker-compose.prod.yml ps

# Restart
docker-compose -f docker-compose.prod.yml restart api
```

### Log İzleme

```bash
# Backend logları (canlı)
docker logs ecommerce-api-prod -f

# Son 100 satır
docker logs ecommerce-api-prod --tail 100

# Frontend logları
docker logs ecommerce-frontend-prod -f

# Database logları
docker logs ecommerce-sql-prod -f
```

### Image Yönetimi

```bash
# Backend image build
docker build -t ecommerce-api:latest ./src -f ./src/ECommerce.API/Dockerfile

# Frontend image build
docker build -t ecommerce-frontend:latest ./frontend

# İmage'ları listele
docker images | grep ecommerce

# Eski image'ları sil
docker image prune -f
```

---

## 📊 DATABASE KOMUTLARI

### SQL Server'a Bağlan

```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C
```

### Database Bilgisi

```bash
# Veritabanı boyutu
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT db_name() as Database, CAST(SUM(size)*8./1024 as DECIMAL(15,2)) as Size_MB FROM sys.master_files WHERE database_id = DB_ID() GROUP BY database_id;"

# Tablo sayısı
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT COUNT(*) as 'Toplam Tablo' FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';"

# Banner sayısı
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT COUNT(*) as 'Toplam Banner' FROM Banners;"

# Kullanıcı sayısı
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT COUNT(*) as 'Toplam Kullanıcı' FROM Users;"
```

### Backup Al

```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "BACKUP DATABASE [ECommerceDb] TO DISK = '/backups/manual_backup_$(date +%Y%m%d_%H%M%S).bak' WITH FORMAT;"
```

---

## 🌐 NGINX KOMUTLARI

### Status

```bash
sudo systemctl status nginx
sudo nginx -V
sudo nginx -t  # Config test
```

### Restart

```bash
sudo systemctl restart nginx
sudo systemctl reload nginx
```

### Config Dosyası

```bash
# Ana config
sudo nano /etc/nginx/nginx.conf

# Site config
sudo nano /etc/nginx/sites-available/golkoygurme

# Dosyaları kontrol et
sudo ls -la /etc/nginx/sites-enabled/
```

### Access/Error Logları

```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

---

## 🔐 SSL SERTIFIKA

### Status

```bash
sudo certbot certificates
sudo certbot status
```

### Renew (Manuel)

```bash
sudo certbot renew --dry-run  # Test
sudo certbot renew            # Gerçek yenileme
```

### Auto-Renew Enable

```bash
sudo systemctl enable certbot.timer
sudo systemctl start certbot.timer
```

---

## 🔍 HEALTH CHECK'LER

### Backend Health

```bash
curl http://localhost:5000/api/health
curl http://localhost:5000/api/banners
```

### Frontend Health

```bash
curl -I http://localhost:3000
curl -I https://golkoygurme.com.tr
```

### Port Kontrol

```bash
# 3000 portunu kullan mı kontrol et
netstat -tulpn | grep 3000

# 5000 portunu kullan mı kontrol et
netstat -tulpn | grep 5000

# 1435 portunu (Database)
netstat -tulpn | grep 1435
```

---

## 💾 DOSYA YÖNETİMİ (Sunucudan Download)

### Backup İndir

```powershell
# Windows PowerShell'de
scp -r huseyinadm@31.186.24.78:/root/eticaret/backups/* ./backup_folder/
```

### Logları İndir

```powershell
# Windows PowerShell'de
scp -r huseyinadm@31.186.24.78:/root/eticaret/logs/* ./logs_folder/
```

### Upload (Sunucuya)

```powershell
# Windows PowerShell'de
scp -r ./src huseyinadm@31.186.24.78:/root/eticaret/
scp -r ./frontend/build/* huseyinadm@31.186.24.78:/root/eticaret/frontend/build/
```

---

## 🚨 ACIL KOMUTLAR

### Container'ı Sıfırla

```bash
# Veri kaybeder!
docker-compose -f docker-compose.prod.yml down -v
docker system prune -af
docker-compose -f docker-compose.prod.yml up -d
```

### Disk Kullanımı

```bash
df -h              # Toplam disk
du -sh /root/*     # Klasör boyutları
du -sh /root/eticaret/*  # Proje klasörleri
```

### Memory/CPU Kullanımı

```bash
free -h            # Memory
top -b -n 1        # CPU
docker stats       # Container resource usage
```

### Network Kontrol

```bash
ping google.com     # İnternet bağlantısı
netstat -tulpn     # Açık portlar
sudo ufw status    # Firewall status
```

---

## 📝 DOSYA DÜZENLEME

### Nginx Config Düzenle

```bash
sudo nano /etc/nginx/sites-available/golkoygurme
# Ctrl+X → Y → Enter (Kaydet)
```

### appsettings.json Düzenle

```bash
nano /root/eticaret/src/ECommerce.API/appsettings.json
# Ctrl+X → Y → Enter (Kaydet)
```

### Crontab Düzenle

```bash
crontab -e
# Satır ekle ve Ctrl+X → Y → Enter
```

---

## 🔄 GÜNCELLEME ADAMLARI

### Frontend Güncelle

```bash
# Lokal'de build et
cd c:\Users\GAMZE\Desktop\eticaret\frontend
npm run build

# Sunucuya yükle
scp -r build/* huseyinadm@31.186.24.78:/root/eticaret/frontend/build/

# Sunucuda restart
ssh huseyinadm@31.186.24.78
docker-compose -f docker-compose.prod.yml restart frontend
```

### Backend Güncelle

```bash
# Lokal'de değişiklikleri yap
# git push yap

# Sunucuda pull
ssh huseyinadm@31.186.24.78
cd /root/eticaret
git pull origin main

# Backend rebuild
docker build -t ecommerce-api:latest ./src -f ./src/ECommerce.API/Dockerfile
docker-compose -f docker-compose.prod.yml restart api
```

---

## 🎯 SKKAYAN KONTROL LISTESI

```bash
# Tüm servisleri kontrol et (bir komutla)
echo "=== CONTAINERS ===" && docker-compose -f /root/eticaret/docker-compose.prod.yml ps && \
echo -e "\n=== BACKEND ===" && curl -s http://localhost:5000/api/health && \
echo -e "\n\n=== FRONTEND ===" && curl -s -I http://localhost:3000 && \
echo -e "\n=== DATABASE ===" && docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT @@VERSION;" && \
echo -e "\n=== DISK ===" && df -h / && \
echo -e "\n=== MEMORY ===" && free -h
```

---

## 🎓 YAYGINCA SORUNLAR

### Problem: `connection refused`

**Çözüm:**

```bash
# Container'ı restart et
docker-compose -f docker-compose.prod.yml restart api

# Logs'u kontrol et
docker logs ecommerce-api-prod --tail 100
```

### Problem: `502 Bad Gateway`

**Çözüm:**

```bash
# Nginx config kontrol et
sudo nginx -t

# Reload et
sudo systemctl reload nginx

# Backend çalıyor mu
curl http://localhost:5000/api/health
```

### Problem: `Admin panel açılmıyor`

**Çözüm:**

```bash
# Frontend logs
docker logs ecommerce-frontend-prod -f

# Nginx config'de /admin route var mı
sudo cat /etc/nginx/sites-available/golkoygurme | grep -A5 "admin"
```

### Problem: `Disk dolu`

**Çözüm:**

```bash
# Eski logs temizle
find /root/eticaret/logs -name "*.log" -mtime +30 -delete

# Docker cleanup
docker system prune -af

# Eski backup'ları sil
find /root/eticaret/backups -name "*.bak" -mtime +30 -delete
```

---

**Son Güncelleme:** 2026-01-12  
**Versiyon:** 2.0.0  
**Durum:** ✅ Production Ready
