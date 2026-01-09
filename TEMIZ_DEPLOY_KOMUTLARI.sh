# 🚀 TEMIZ SUNUCU DEPLOY - WINDOWS POWERSCRIPT

## 📌 SUNUCU BİLGİLERİ
# IP: 31.186.24.78
# Port: 22
# Kullanıcı: huseyinadm
# Şifre: Passwd1122FFGG
# Proje Dizini: /home/huseyinadm/eticaret

# ============================================================================
# FAZA 1: SUNUCUYA BAĞLANMA
# ============================================================================

## Adım 1.1 - SSH ile Bağlanma
ssh huseyinadm@31.186.24.78

## Adım 1.2 - Şifre Girin
# Passwd1122FFGG

## Adım 1.3 - Proje Dizinine Gitme
cd /home/huseyinadm

# ============================================================================
# FAZA 2: ESKİ DEPLOYMENT'I TEMİZLEME
# ============================================================================

cd eticaret

## Adım 2.1 - Tüm Container'ları ve Volume'ları Kaldır
docker-compose -f docker-compose.prod.yml down -v

## Adım 2.2 - Docker Image'larını Sil
docker rmi ecommerce-frontend:latest 2>/dev/null || true
docker rmi ecommerce-api:latest 2>/dev/null || true

## Adım 2.3 - Dangling Image'ları Temizle
docker image prune -f

## Adım 2.4 - Logs Klasörünü Temizle
rm -rf logs/*

# ============================================================================
# FAZA 3: KOD GÜNCELLEME
# ============================================================================

## Adım 3.1 - Kodu GitHub'dan Çek
git pull origin main

## Adım 3.2 - Dosyaları Kontrol Et
ls -la

# Beklenen: frontend/, src/, docker-compose.prod.yml

# ============================================================================
# FAZA 4: ENVIRONMENT DOSYASINI OLUŞTUR
# ============================================================================

## Adım 4.1 - .env Dosyası Oluştur
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

## Adım 4.2 - .env Dosyasını Kontrol Et
cat .env

# ============================================================================
# FAZA 5: DOCKER BUILD
# ============================================================================

## Adım 5.1 - Tüm Image'ları Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build --no-cache

# Süre: ~3-5 dakika. Bekleyin...

## Adım 5.2 - Container'ları Başlat
docker-compose -f docker-compose.prod.yml up -d

# ============================================================================
# FAZA 6: VERITABANINI BAŞLATMA VE KONTROL
# ============================================================================

## Adım 6.1 - Container Durumunu Kontrol Et
docker-compose -f docker-compose.prod.yml ps

# Beklenen: Tüm servislerin "Up" durumda olması

## Adım 6.2 - API Loglarını İzle (Migration Kontrol)
docker-compose -f docker-compose.prod.yml logs api -f

# Beklenen: "All seed operations completed successfully"
# CTRL+C ile çıkın

## Adım 6.3 - 30 Saniye Bekle
sleep 30

## Adım 6.4 - SQL Server'a Bağlan ve Veritabanını Kontrol Et
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Bağlandıktan sonra SQL Komutları:
SELECT name FROM sys.databases;
GO
USE ECommerceDb
GO
SELECT COUNT(*) as [Ürün Sayısı] FROM Products;
GO
SELECT COUNT(*) as [Kategori Sayısı] FROM Categories;
GO
SELECT COUNT(*) as [Kullanıcı Sayısı] FROM Users;
GO
EXIT

# ============================================================================
# FAZA 7: SERVIS DURUMU KONTROLÜ
# ============================================================================

## Adım 7.1 - Frontend Kontrol
curl -I http://localhost:3000

# Beklenen: HTTP/1.1 200 OK veya HTTP/1.1 301

## Adım 7.2 - API Kontrol
curl -I http://localhost:5000/api/health

# Beklenen: HTTP/1.1 200 OK

## Adım 7.3 - Tüm Logları Kontrol Et
docker-compose -f docker-compose.prod.yml logs --tail=50

## Adım 7.4 - API Sağlığını Test Et
docker exec ecommerce-api-prod curl -s http://localhost:5000/api/health | head -c 200

# ============================================================================
# FAZA 8: NGINX VE REVERSE PROXY AYARI (İsteğe Bağlı)
# ============================================================================

## Adım 8.1 - Nginx Kur
sudo apt install -y nginx

## Adım 8.2 - Nginx Config Dosyası Oluştur
sudo tee /etc/nginx/sites-available/golkoygurme > /dev/null << 'EOF'
server {
    listen 80;
    listen [::]:80;
    server_name golkoygurme.com.tr www.golkoygurme.com.tr;

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

## Adım 8.3 - Config'i Etkinleştir ve Test Et
sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx

# ============================================================================
# FAZA 9: SSL SERTIFIKASI (HTTPS)
# ============================================================================

## Adım 9.1 - Certbot ve Let's Encrypt Kur
sudo apt install -y certbot python3-certbot-nginx

## Adım 9.2 - SSL Sertifikası Oluştur
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr

## Adım 9.3 - Nginx'i HTTPS için Güncelle
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

## Adım 9.4 - Nginx'i Yeniden Başlat
sudo nginx -t
sudo systemctl restart nginx

# ============================================================================
# FAZA 10: SON KONTROLLER
# ============================================================================

## Adım 10.1 - Tüm Container'ları Kontrol Et
docker-compose -f docker-compose.prod.yml ps

## Adım 10.2 - API Sağlığı
curl http://localhost:5000/api/health

## Adım 10.3 - Veritabanı Bağlantısı Kontrol Et
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products"

## Adım 10.4 - Firewall Durumu
sudo ufw status

# ============================================================================
# HIZLI KOMUTLAR
# ============================================================================

# Servisleri Başlat
docker-compose -f docker-compose.prod.yml up -d

# Servisleri Durdur
docker-compose -f docker-compose.prod.yml down

# Logları Takip Et (Real-time)
docker-compose -f docker-compose.prod.yml logs -f

# Yalnız API Logları
docker-compose -f docker-compose.prod.yml logs -f api

# Yalnız Frontend Logları
docker-compose -f docker-compose.prod.yml logs -f frontend

# Yalnız Database Logları
docker-compose -f docker-compose.prod.yml logs -f sqlserver

# API'yi Yeniden Oluştur ve Başlat
docker-compose -f docker-compose.prod.yml build api && docker-compose -f docker-compose.prod.yml up -d api

# Frontend'i Yeniden Oluştur ve Başlat
docker-compose -f docker-compose.prod.yml build frontend && docker-compose -f docker-compose.prod.yml up -d frontend

# Veritabanına Bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Container Kaynak Kullanımı
docker stats

# Tüm Veriyi Sil ve Yeni Baştan Başla (DİKKAT!)
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# ============================================================================
# TROUBLESHOOTING
# ============================================================================

# Container Başlamıyor
docker-compose -f docker-compose.prod.yml logs api
docker-compose -f docker-compose.prod.yml logs sqlserver

# Veritabanı Bağlantısı Sorunu
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"

# Port Çakışması
sudo netstat -tulpn | grep LISTEN
sudo lsof -i :5000
sudo lsof -i :3000

# Yeni Baştan Başla
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# ============================================================================
# ✅ DEPLOYMENT BAŞARILI ÖZETİ
# ============================================================================

# Eğer aşağıdaki adımlar tamamlandıysa, deployment başarılı:
# 1. ✅ Tüm container'lar "Up" durumda
# 2. ✅ API port 5000'de çalışıyor
# 3. ✅ Frontend port 3000'de çalışıyor
# 4. ✅ Veritabanı başarıyla oluşturuldu ve seed data yüklendi
# 5. ✅ CORS ayarları production domain'leri kapsıyor
# 6. ✅ SSL sertifikası yapılandırıldı (HTTPS)
# 7. ✅ Nginx reverse proxy çalışıyor
# 
# Erişim: https://golkoygurme.com.tr/
