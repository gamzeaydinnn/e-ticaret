# 🚀 PRODUCTION DEPLOYMENT REHBERI - ADIM ADIM

**Sunucu:** 31.186.24.78  
**Kullanıcı:** huseyinadm  
**Proje:** E-Ticaret - Gölköy Gurme Market  
**Tarih:** 2026-01-12

---

## 📋 ÖN HAZIRLIK

### Sunucu Bilgileri:

- **IP Adresi:** 31.186.24.78
- **SSH Kullanıcısı:** huseyinadm
- **SSH Port:** 22 (veya özel port)
- **Domain:** golkoygurme.com.tr

### Lokal Hazırlık:

Deployment öncesi bu dosyaları sunucuya yükleyeceksiniz:

1. `docker-compose.prod.yml` - Docker compose configuration
2. Frontend `build/` klasörü - React production build
3. Backend source code - .NET uygulaması
4. `appsettings.json` - Konfigürasyon

---

## 🔌 ADIM 1: SUNUCUYA BAĞLAN

### Windows'tan SSH Bağlantısı:

```powershell
ssh huseyinadm@31.186.24.78
# Şifre sorulursa, sunucu şifresini gir
```

### Bağlandığında kontrol et:

```bash
whoami                    # Kullanıcı adını göster
pwd                       # Mevcut dizini göster
uname -a                  # Sistem bilgisi
```

**Beklenen çıktı:**

```
huseyinadm
/home/huseyinadm
Linux server 5.10.x #1 SMP ...
```

---

## 📁 ADIM 2: PROJE DİZİNİ HAZIRLA

### Proje klasörü oluştur:

```bash
mkdir -p /root/eticaret
cd /root/eticaret
pwd
```

### Gerekli alt klasörleri oluştur:

```bash
mkdir -p src
mkdir -p frontend/build
mkdir -p logs
mkdir -p uploads
mkdir -p backups
```

### Kontrol et:

```bash
ls -la
```

---

## 📤 ADIM 3: DOSYALARI SUNUCUYA YÜKLE

**NOT:** Bunları lokal makinenden PowerShell'de çalıştır (sunucuda değil!):

### 3.1 Docker Compose Dosyasını Yükle

```powershell
# Windows PowerShell'de çalıştır
scp -r docker-compose.prod.yml huseyinadm@31.186.24.78:/root/eticaret/
```

### 3.2 Frontend Build'ini Yükle

```powershell
# Windows PowerShell'de çalıştır
# Önce frontend build'lenmiş olmalı:
cd c:\Users\GAMZE\Desktop\eticaret\frontend
npm run build  # Eğer build yoksa çalıştır

# Sonra yükle:
scp -r build/* huseyinadm@31.186.24.78:/root/eticaret/frontend/build/
```

### 3.3 Backend Source Code'u Yükle

```powershell
# Windows PowerShell'de çalıştır
scp -r c:\Users\GAMZE\Desktop\eticaret\src huseyinadm@31.186.24.78:/root/eticaret/
```

### 3.4 Konfigürasyon Dosyasını Yükle

```powershell
# Windows PowerShell'de çalıştır
scp c:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API\appsettings.json huseyinadm@31.186.24.78:/root/eticaret/src/ECommerce.API/
```

---

## 🐳 ADIM 4: DOCKER KURULUMU (Sunucuda)

### 4.1 Docker Kurulu mu Kontrol Et

```bash
docker --version
docker-compose --version
```

**Eğer kurulu değilse:** (sunucuda)

```bash
# Ubuntu/Debian için
sudo apt update
sudo apt install -y docker.io docker-compose

# Docker daemon'u başlat
sudo systemctl start docker
sudo systemctl enable docker

# Şu kullanıcı docker groups'a ekle
sudo usermod -aG docker huseyinadm
```

### 4.2 Docker İçin Yapılandırma

```bash
# Sunucuda
cd /root/eticaret

# Dockerfile kontrol et
ls -la src/ECommerce.API/Dockerfile
ls -la frontend/Dockerfile
```

---

## 🗄️ ADIM 5: SQL SERVER CONTAINER BAŞLAT

### Sunucuda:

```bash
cd /root/eticaret

# docker-compose'daki SQL Server hizmeti başlat
docker-compose -f docker-compose.prod.yml up -d sqlserver

# Container başladığını kontrol et (30 saniye bekle)
sleep 30
docker ps | grep sqlserver
```

**Beklenen:** `ecommerce-sql-prod` container'ı `Up` durumda olmalı

### SQL Server bağlantısını test et:

```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT @@VERSION;"
```

**Beklenen:** SQL Server versiyonu ekrana yazılmalı

---

## 🔨 ADIM 6: BACKEND BUILD ET VE BAŞLAT

### Sunucuda:

```bash
cd /root/eticaret

# Backend image'ı build et
docker build -t ecommerce-api:latest ./src -f ./src/ECommerce.API/Dockerfile

# Build tamamlandığını kontrol et
docker images | grep ecommerce-api
```

### Backend container'ı başlat:

```bash
# docker-compose ile başlat
docker-compose -f docker-compose.prod.yml up -d api

# 20 saniye bekle
sleep 20

# Container'ı kontrol et
docker ps | grep api
```

### Backend loglarını incele:

```bash
# İlk 100 satırı göster
docker logs ecommerce-api-prod --tail 100

# Canlı logları izle (Ctrl+C ile durdur)
docker logs ecommerce-api-prod -f
```

**Beklenen:**

- `✅ Database migrations uygulandı`
- `✅ IdentitySeeder tamamlandı`
- `✅ ProductSeeder tamamlandı`
- `✅ BannerSeeder tamamlandı`
- `Application started`

### Backend health check:

```bash
curl http://localhost:5000/api/health
```

**Beklenen:** JSON response dönmeli

---

## 🎨 ADIM 7: FRONTEND IMAGE BUILD ET

### Sunucuda:

```bash
cd /root/eticaret

# Frontend image'ı build et
docker build -t ecommerce-frontend:latest ./frontend

# Build tamamlandığını kontrol et
docker images | grep ecommerce-frontend
```

### Frontend container'ı başlat:

```bash
docker-compose -f docker-compose.prod.yml up -d frontend

# 10 saniye bekle
sleep 10

# Container'ı kontrol et
docker ps | grep frontend
```

### Frontend loglarını incele:

```bash
docker logs ecommerce-frontend-prod --tail 50
```

**Beklenen:** Nginx başarıyla başlamalı, hata olmamalı

### Frontend erişimi test et:

```bash
curl -I http://localhost:3000
```

**Beklenen:** `200 OK` veya `301 Redirect`

---

## 🌐 ADIM 8: NGINX HOST KONFIGÜRASYONU

### Host Nginx'i konfigüre et:

```bash
# Nginx kurulu mu kontrol et
sudo nginx -v

# Eğer kurulu değilse:
# sudo apt install -y nginx certbot python3-certbot-nginx
```

### Nginx config dosyası oluştur:

```bash
sudo tee /etc/nginx/sites-available/golkoygurme > /dev/null << 'EOF'
server {
    listen 80;
    listen [::]:80;
    server_name golkoygurme.com.tr www.golkoygurme.com.tr;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name golkoygurme.com.tr www.golkoygurme.com.tr;

    ssl_certificate /etc/letsencrypt/live/golkoygurme.com.tr/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/golkoygurme.com.tr/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        client_max_body_size 10M;
    }

    location /uploads/ {
        proxy_pass http://localhost:5000/uploads/;
        proxy_set_header Host $host;
        add_header Cache-Control "public, max-age=86400";
    }
}
EOF
```

### Nginx config'i etkinleştir:

```bash
# Symlink oluştur
sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/

# Default config'i kapat (opsiyonel)
sudo rm -f /etc/nginx/sites-enabled/default

# Config'i test et
sudo nginx -t
```

**Beklenen:** `syntax is ok` ve `test is successful`

### Nginx'i reload et:

```bash
sudo systemctl reload nginx
sudo systemctl status nginx
```

**Beklenen:** `active (running)` yazmalı

---

## 🔐 ADIM 9: SSL SERTIFIKA AYARLA (Let's Encrypt)

### Sertifika talep et:

```bash
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr
```

**Adımlar:**

1. Email adresi gir: `huseyinadm@golkoygurme.com.tr`
2. Terms of Service kabul et: `y`
3. Email duyuruları için: `y` veya `n`

### Sertifika kontrol et:

```bash
sudo ls -la /etc/letsencrypt/live/golkoygurme.com.tr/
```

**Beklenen:** `fullchain.pem` ve `privkey.pem` dosyaları görülmeli

### Nginx'i yeniden başlat:

```bash
sudo systemctl reload nginx
```

---

## ✅ ADIM 10: TARAMA VE TEST

### 10.1 Docker Container Durumları

```bash
docker-compose -f docker-compose.prod.yml ps
```

**Beklenen:** Tüm container'lar `Up` durumda

```
NAME                     STATUS
ecommerce-sql-prod       Up
ecommerce-api-prod       Up
ecommerce-frontend-prod  Up
```

### 10.2 Backend API Test

```bash
curl -X GET http://localhost:5000/api/health
curl -X GET http://localhost:5000/api/banners/slider
curl -X GET http://localhost:5000/api/banners/promo
```

**Beklenen:** JSON response dönmeli

### 10.3 Admin Panel Test

```bash
curl -I https://golkoygurme.com.tr/admin
```

**Beklenen:** `200 OK` veya `301 Redirect`

### 10.4 Tarayıcıda Test

1. **Ana Sayfa:** https://golkoygurme.com.tr

   - Slider poster'ları görülmeli
   - Promo kartları görülmeli

2. **Admin Panel:** https://golkoygurme.com.tr/admin

   - Login sayfası açılmalı
   - Giriş: admin@admin.com / admin123

3. **Poster Yönetimi:** Admin Panel → Poster Yönetimi
   - Banner CRUD işlemleri çalışmalı

---

## 🔄 ADIM 11: BACKUP VE MONITORING AYARLA

### 11.1 Günlük Backup Otomasyonu

```bash
# Cron job'u ekle
crontab -e

# Şu satırı ekle (her gece 02:00'de backup al):
0 2 * * * docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "BACKUP DATABASE [ECommerceDb] TO DISK = '/backups/db_backup_$(date +\%Y\%m\%d_\%H\%M\%S).bak' WITH FORMAT;"
```

### 11.2 Log Rotation

```bash
# Log dosyalarını otomatik rotasyona al
cat >> /etc/logrotate.d/ecommerce << 'EOF'
/root/eticaret/logs/*.log {
    daily
    rotate 30
    compress
    delaycompress
    notifempty
    create 0644 root root
    postrotate
        docker kill -s SIGHUP ecommerce-api-prod 2>/dev/null || true
    endscript
}
EOF
```

---

## ⚠️ SORUN GİDERME

### Docker Container Restart

```bash
# Tüm servisleri restart et
docker-compose -f docker-compose.prod.yml restart

# Sadece API restart
docker-compose -f docker-compose.prod.yml restart api

# Sadece Frontend restart
docker-compose -f docker-compose.prod.yml restart frontend
```

### Backend Loglarını Canlı İzle

```bash
docker logs ecommerce-api-prod -f
```

### Database Bağlantısını Test Et

```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT COUNT(*) as 'Toplam Banner' FROM Banners;"
```

### Proxy Hatası (ECONNREFUSED)

```bash
# Backend'in çalıştığını kontrol et
curl http://localhost:5000/api/health

# Logs'ta ne olduğunu kontrol et
docker logs ecommerce-api-prod --tail 100
```

### Admin Panel Açılmıyor

```bash
# Frontend logs'u kontrol et
docker logs ecommerce-frontend-prod --tail 100

# Nginx config'i kontrol et
sudo nginx -t
```

---

## 📊 ADIM 12: GÜNLÜK BAKIMI

### Her Gün:

```bash
# Container durumlarını kontrol et
docker-compose -f docker-compose.prod.yml ps

# Backend loglarında hata var mı kontrol et
docker logs ecommerce-api-prod --tail 50 | grep -i error
```

### Her Hafta:

```bash
# Disk kullanımını kontrol et
df -h

# Database boyutunu kontrol et
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT db_name() AS DBName, CAST(SUM(size) * 8./1024 as DECIMAL(15,2)) as Size_MB FROM sys.master_files WHERE database_id = DB_ID() GROUP BY database_id;"
```

### Her Ay:

```bash
# SSL sertifikanın geçerliliğini kontrol et
sudo certbot certificates

# Eski backup'ları sil (30 günden eski)
find /root/eticaret/backups -name "*.bak" -mtime +30 -delete
```

---

## 🎯 KONTROL LİSTESİ

### Deployment Öncesi:

- [ ] Frontend build edildimi? (`npm run build`)
- [ ] Backend source kodu sunucuya yüklendimi?
- [ ] appsettings.json sunucuya yüklendimi?
- [ ] docker-compose.prod.yml sunucuya yüklendimi?

### Deployment Sırası:

- [ ] Sunucuya SSH bağlantısı kuruldu mu?
- [ ] Proje dizini oluşturuldu mu?
- [ ] Docker Compose başlatıldı mı?
- [ ] SQL Server container başladı mı?
- [ ] Backend image build edildi mi?
- [ ] API container başladı mı?
- [ ] Frontend image build edildi mi?
- [ ] Frontend container başladı mı?
- [ ] Nginx konfigüre edildi mi?
- [ ] SSL sertifika ayarlandı mı?

### Deployment Sonrası:

- [ ] Tüm container'lar Up durumda mı?
- [ ] Backend health check başarılı mı?
- [ ] Frontend erişilebilir mi?
- [ ] Admin panel açılıyor mu?
- [ ] Banner API çalışıyor mu?
- [ ] Login çalışıyor mu?
- [ ] HTTPS sertifikası geçerli mi?

---

## 📞 ACIL DURUM

### Container'ları Sıfırla (Veri kaybedecek!)

```bash
docker-compose -f docker-compose.prod.yml down -v
docker-compose -f docker-compose.prod.yml up -d
```

### Database'i Manuel Backup Al

```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "BACKUP DATABASE [ECommerceDb] TO DISK = '/backups/emergency_backup_$(date +%Y%m%d_%H%M%S).bak' WITH FORMAT;"

# Backup dosyasını lokal'e indir
scp huseyinadm@31.186.24.78:/root/eticaret/backups/emergency_backup_*.bak ./backups/
```

---

## 🎉 BAŞARILI DEPLOYMENT SONRASI

**Erişim Adresleri:**

- 🌐 **Ana Sayfa:** https://golkoygurme.com.tr
- 🔐 **Admin Panel:** https://golkoygurme.com.tr/admin
- 🔑 **Kullanıcı:** admin@admin.com / admin123
- 📊 **API:** https://golkoygurme.com.tr/api

**Başarı göstergeleri:**

- ✅ Tüm container'lar `Up` durumda
- ✅ Backend API sağlıklı
- ✅ Frontend yüklenmiş
- ✅ SSL sertifikası aktif
- ✅ Admin panel erişilebilir
- ✅ Banner'lar görünüyor

---

**Hazırlayan:** Senior Developer  
**Versiyon:** 2.0.0 - Manuel Deployment  
**Son Güncelleme:** 2026-01-12
