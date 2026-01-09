# 🚀 SUNUCUYA TEMIZ DEPLOY - HIZLI BAŞLANGAÇ

## 🎯 TÜM KOMUTLAR BİR ARADA

### SUNUCU BİLGİLERİ
```
IP: 31.186.24.78
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
```

---

## 📋 KOPYALA-YAPISTIR KOMUTLARı (Sunucuda Çalıştır)

### 1️⃣ BAĞLAN
```bash
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
cd /home/huseyinadm/eticaret
```

### 2️⃣ ESKİ DEPLOYMENT'I TEMİZLE (Tüm veri silinir!)
```bash
docker-compose -f docker-compose.prod.yml down -v
docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true
docker image prune -f
rm -rf logs/*
```

### 3️⃣ KOD GÜNCELLE
```bash
git pull origin main
```

### 4️⃣ ENVIRONMENT DOSYASINI OLUŞTUR
```bash
cat > .env << 'EOF'
DB_PASSWORD=ECom1234
DB_PORT=1435
API_PORT=5000
ASPNETCORE_ENVIRONMENT=Production
FRONTEND_PORT=3000
REACT_APP_API_URL=https://golkoygurme.com.tr/api
JWT_SECRET=YourVeryStrongSecretKeyMinimum32CharactersLong!!!
NETGSM_USERCODE=8503078774
NETGSM_PASSWORD=123456Z-M
NETGSM_MSGHEADER=GOLKYGURMEM
NETGSM_APPNAME=GolkoyGurme
NETGSM_ENABLED=true
NETGSM_USEMOCKSERVICE=false
SMS_EXPIRATION_SECONDS=180
SMS_RESEND_COOLDOWN=60
SMS_DAILY_MAX=5
SMS_HOURLY_MAX=3
SMS_MAX_WRONG_ATTEMPTS=3
CORS__ALLOWEDORIGINS__0=https://golkoygurme.com.tr
CORS__ALLOWEDORIGINS__1=https://www.golkoygurme.com.tr
CORS__ALLOWEDORIGINS__2=http://golkoygurme.com.tr
CORS__ALLOWEDORIGINS__3=http://www.golkoygurme.com.tr
EOF
```

### 5️⃣ BUILD VE DEPLOY
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

### 6️⃣ KONTROL (Migration bitene kadar bekle)
```bash
docker-compose -f docker-compose.prod.yml logs api -f
# "All seed operations completed successfully" görünce CTRL+C
```

### 7️⃣ SON KONTROLLER
```bash
docker-compose -f docker-compose.prod.yml ps
curl http://localhost:5000/api/health
curl -I http://localhost:3000
```

---

## 🐳 DOCKER KOMUTLARI (Hızlı Referans)

```bash
# Container Durumu Kontrol
docker-compose -f docker-compose.prod.yml ps

# Logları Canlı Takip
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API Logları
docker-compose -f docker-compose.prod.yml logs -f api

# Servisleri Başlat
docker-compose -f docker-compose.prod.yml up -d

# Servisleri Durdur
docker-compose -f docker-compose.prod.yml down

# API'yi Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build api
docker-compose -f docker-compose.prod.yml up -d api

# Frontend'i Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build frontend
docker-compose -f docker-compose.prod.yml up -d frontend

# Veritabanını Kontrol
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Kaynak Kullanımını Görüntüle
docker stats
```

---

## 🗄️ SQL SERVER KOMUTLARI

### Veritabanına Bağlan
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

### Bağlandıktan Sonra Çalıştır
```sql
SELECT COUNT(*) as [Ürün Sayısı] FROM ECommerceDb.dbo.Products;
GO
SELECT COUNT(*) as [Kategori Sayısı] FROM ECommerceDb.dbo.Categories;
GO
SELECT COUNT(*) as [Kullanıcı Sayısı] FROM ECommerceDb.dbo.Users;
GO
EXIT
```

---

## 🌐 NGINX VE HTTPS SETUP

### Nginx Kur
```bash
sudo apt install -y nginx certbot python3-certbot-nginx
```

### SSL Sertifikası Oluştur
```bash
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr
```

### Nginx Config (HTTPS ile)
```bash
sudo tee /etc/nginx/sites-available/golkoygurme > /dev/null << 'EOF'
server {
    listen 80;
    server_name golkoygurme.com.tr www.golkoygurme.com.tr;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
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
        proxy_cache_bypass $http_upgrade;
    }

    location /api/ {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
EOF

sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## ⚠️ SORUN ÇÖZMEK

### Container Başlamıyor
```bash
docker-compose -f docker-compose.prod.yml logs api
docker-compose -f docker-compose.prod.yml logs sqlserver
```

### API Çalışmıyor
```bash
curl http://localhost:5000/api/health
docker exec ecommerce-api-prod curl -s http://localhost:5000/api/health
```

### Veritabanı Sorunu
```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"
```

### Port Çakışması
```bash
sudo netstat -tulpn | grep LISTEN
sudo lsof -i :5000
sudo lsof -i :3000
```

### Tüm Veriyi Sil ve Yeni Başla (DİKKAT!)
```bash
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

---

## ✅ BAŞARILI DEPLOYMENT ÖZETİ

- ✅ `docker ps` tüm container'ları gösteriyor
- ✅ `curl http://localhost:5000/api/health` yanıt veriyor
- ✅ `curl http://localhost:3000` 200 OK dönüyor
- ✅ Veritabanı ürünlerle dolu (`SELECT COUNT(*) FROM Products` > 0)
- ✅ HTTPS çalışıyor: https://golkoygurme.com.tr/
- ✅ API erişim var: https://golkoygurme.com.tr/api/health

---

## 📊 MONITORING VE BAKIMSEVER KOMUTLAR

```bash
# Real-time Monitoring
watch -n 2 'docker-compose -f docker-compose.prod.yml ps'

# Günlük Log Döngüsü Kontrol
docker system df

# Eski Log Dosyalarını Temizle
docker-compose -f docker-compose.prod.yml logs --tail=100 -f

# Konteyner Restartını Kontrol
docker-compose -f docker-compose.prod.yml ps | grep "Restarting"

# Disk Kullanımını Kontrol
du -sh /home/huseyinadm/eticaret
du -sh /var/lib/docker/volumes
```

---

**Hazırlanan:** 9 Ocak 2026  
**Server:** 31.186.24.78  
**Proje:** GolkoyGurme E-Ticaret
