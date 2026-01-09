# 🚀 TEMIZ SUNUCU DEPLOY - ADIM ADIM KOMUTLAR

## 📋 SUNUCU BİLGİLERİ
```
IP: 31.186.24.78
Port: 22
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
Proje Dizini: /home/huseyinadm/eticaret
```

---

## 🔴 FAZE 1: SUNUCUYA BAĞLANMA

### Adım 1.1 - SSH ile Bağlanma
```bash
ssh huseyinadm@31.186.24.78
# Şifre girin: Passwd1122FFGG
```

### Adım 1.2 - Proje Dizinine Gitme
```bash
cd /home/huseyinadm
```

---

## 🟠 FAZE 2: ESKİ DEPLOYMENT'I TEMİZLEME

### Adım 2.1 - Mevcut Container'ları Durdur ve Sil
```bash
cd eticaret
docker-compose -f docker-compose.prod.yml down -v
```
**Açıklama:** `-v` flag'ı volumes'ları da siliyor (veritabanı dahil)

### Adım 2.2 - Docker Image'larını Sil (İsteğe Bağlı)
```bash
docker rmi ecommerce-frontend:latest
docker rmi ecommerce-api:latest
```

### Adım 2.3 - Dangling Image'ları Temizle
```bash
docker image prune -f
```

### Adım 2.4 - Logs Klasörünü Temizle
```bash
rm -rf logs/*
```

---

## 🟡 FAZE 3: KOD GÜNCELLEME

### Adım 3.1 - Kodu GitHub'dan Çek
```bash
cd /home/huseyinadm/eticaret
git pull origin main
```

**NOT:** Eğer repo klonlanmamışsa:
```bash
cd /home/huseyinadm
rm -rf eticaret  # Eski sürümü sil
git clone https://github.com/gamzeaydinnn/e-ticaret.git eticaret
cd eticaret
```

### Adım 3.2 - Dosyaları Kontrol Et
```bash
ls -la
# frontend/, src/, docker-compose.prod.yml dosyalarının var olduğunu kontrol et
```

---

## 🟢 FAZE 4: ENVIRONMENT VE KONFİGÜRASYON

### Adım 4.1 - Production .env Dosyası Oluştur
```bash
cat > .env << 'EOF'
# ============ DATABASE ============
DB_PASSWORD=ECom1234
DB_PORT=1435

# ============ API ============
API_PORT=5000
ASPNETCORE_ENVIRONMENT=Production

# ============ FRONTEND ============
FRONTEND_PORT=3000
REACT_APP_API_URL=https://golkoygurme.com.tr/api

# ============ JWT ============
JWT_SECRET=YourVeryStrongSecretKeyMinimum32CharactersLong!!! 

# ============ NETGSM SMS ============
NETGSM_USERCODE=8503078774
NETGSM_PASSWORD=123456Z-M
NETGSM_MSGHEADER=GOLKYGURMEM
NETGSM_APPNAME=GolkoyGurme
NETGSM_ENABLED=true
NETGSM_USEMOCKSERVICE=false

# ============ SMS VERİFİCATİON ============
SMS_EXPIRATION_SECONDS=180
SMS_RESEND_COOLDOWN=60
SMS_DAILY_MAX=5
SMS_HOURLY_MAX=3
SMS_MAX_WRONG_ATTEMPTS=3

# ============ CORS ============
CORS__ALLOWEDORIGINS__0=https://golkoygurme.com.tr
CORS__ALLOWEDORIGINS__1=https://www.golkoygurme.com.tr
CORS__ALLOWEDORIGINS__2=http://golkoygurme.com.tr
CORS__ALLOWEDORIGINS__3=http://www.golkoygurme.com.tr
EOF
```

### Adım 4.2 - .env Dosyasını Kontrol Et
```bash
cat .env
```

---

## 🔵 FAZA 5: DOCKERFILE VE DOCKER-COMPOSE GÜNCELLEMELERI

### Adım 5.1 - Nginx Dockerfile'daki API URL'yi Kontrol Et
```bash
grep -n "REACT_APP_API_URL" frontend/Dockerfile
```

**Beklenen:** `REACT_APP_API_URL=https://golkoygurme.com.tr/api`

### Adım 5.2 - Docker Compose'daki Frontend URL'yi Kontrol Et
```bash
grep -n "REACT_APP_API_URL" docker-compose.prod.yml
```

**Beklenen:** `REACT_APP_API_URL=https://golkoygurme.com.tr/api`

---

## 🟣 FAZA 6: DOCKER BUILD VE DEPLOYMENT

### Adım 6.1 - Tüm Image'ları Yeniden Oluştur
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

**Süre:** ~3-5 dakika

### Adım 6.2 - Container'ları Başlat
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### Adım 6.3 - Container Durumunu Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```

**Beklenen:** Tüm servislerin `Up` durumda olması

---

## 🟠 FAZA 7: VERITABANINI BAŞLATMA VE SEED DATA

### Adım 7.1 - API Container Loglarını İzle (Veritabanı Migration Kontrolü)
```bash
docker-compose -f docker-compose.prod.yml logs api -f
```

**Beklenen:** "All seed operations completed successfully" mesajı

**CTRL+C** ile çıkın

### Adım 7.2 - Veritabanı Migration Tamamlanmasını Bekle
```bash
sleep 30
```

### Adım 7.3 - SQL Server'a Bağlan (Opsiyonel - Veritabanını Kontrol Et)
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

Bağlandıktan sonra:
```sql
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
```

---

## 🟢 FAZA 8: SERVIS DURUMU KONTROLÜ

### Adım 8.1 - Frontend Kontrol (Port 3000)
```bash
curl -I http://localhost:3000
```

**Beklenen:** `HTTP/1.1 200 OK` veya `HTTP/1.1 301`

### Adım 8.2 - API Kontrol (Port 5000)
```bash
curl -I http://localhost:5000/api/health
```

**Beklenen:** `HTTP/1.1 200 OK`

### Adım 8.3 - Tüm Logları Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml logs --tail=50
```

### Adım 8.4 - Container İçinde API Çalışıp Çalışmadığını Kontrol Et
```bash
docker exec ecommerce-api-prod curl -s http://localhost:5000/api/health | head -c 200
```

---

## 🟡 FAZA 9: DOMAIN YAPISI (NGINX REVERSE PROXY)

### Adım 9.1 - Nginx Kurulumu (Opsiyonel - Domain Yönlendirmesi İçin)
```bash
sudo apt install -y nginx
```

### Adım 9.2 - Nginx Config Oluştur
```bash
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
```

### Adım 9.3 - Nginx Config'i Etkinleştir
```bash
sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## 🔴 FAZA 10: SSL SERTIFIKASI (HTTPS - ÖNERİLEN)

### Adım 10.1 - Let's Encrypt Sertifikası Oluştur
```bash
sudo apt install -y certbot python3-certbot-nginx
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr
```

### Adım 10.2 - Nginx'i HTTPS için Güncelle
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
```

### Adım 10.3 - Nginx'i Yeniden Başlat
```bash
sudo nginx -t
sudo systemctl restart nginx
```

---

## 🟢 FAZA 11: SON KONTROLLER

### Adım 11.1 - Tüm Container'ları Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```

### Adım 11.2 - API Sağlığını Kontrol Et
```bash
curl http://localhost:5000/api/health
```

### Adım 11.3 - Veritabanını Kontrol Et
```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products"
```

### Adım 11.4 - Firewall Kurallarını Kontrol Et
```bash
sudo ufw status
```

---

## 🆘 TROUBLESHOOTING

### Container Başlamıyor
```bash
docker-compose -f docker-compose.prod.yml logs api
docker-compose -f docker-compose.prod.yml logs sqlserver
```

### Veritabanı Bağlantısı Sorunu
```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"
```

### Port Çakışması
```bash
sudo netstat -tulpn | grep LISTEN
sudo lsof -i :5000
sudo lsof -i :3000
```

### Tüm Container'ları Yeniden Başlat
```bash
docker-compose -f docker-compose.prod.yml restart
```

### Yeni Baştan Başla (Tüm Veri Silinir!)
```bash
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

---

## 📊 MONITORING

### Logları Takip Et (Real-time)
```bash
docker-compose -f docker-compose.prod.yml logs -f
```

### Yalnız API Logları
```bash
docker-compose -f docker-compose.prod.yml logs -f api
```

### Yalnız Frontend Logları
```bash
docker-compose -f docker-compose.prod.yml logs -f frontend
```

### Yalnız Database Logları
```bash
docker-compose -f docker-compose.prod.yml logs -f sqlserver
```

### Container Kaynak Kullanımını Kontrol Et
```bash
docker stats
```

---

## ✅ DEPLOYMENT BAŞARILI ÖZETİ

Eğer aşağıdaki adımlar tamamlandıysa, deployment başarılı demektir:

1. ✅ Tüm container'lar `Up` durumda
2. ✅ API port 5000'de çalışıyor
3. ✅ Frontend port 3000'de çalışıyor
4. ✅ Veritabanı başarıyla oluşturuldu ve seed data yüklendi
5. ✅ CORS ayarları production domain'leri kapsıyor
6. ✅ SSL sertifikası yapılandırıldı (HTTPS)
7. ✅ Nginx reverse proxy çalışıyor

**Erişim:** https://golkoygurme.com.tr/

---

## 📞 HIZLI REFERANS

```bash
# Servisleri başlat
docker-compose -f docker-compose.prod.yml up -d

# Servisleri durdur
docker-compose -f docker-compose.prod.yml down

# Logları görüntüle
docker-compose -f docker-compose.prod.yml logs -f

# API'yi yeniden oluştur ve başlat
docker-compose -f docker-compose.prod.yml build api && docker-compose -f docker-compose.prod.yml up -d api

# Frontend'i yeniden oluştur ve başlat
docker-compose -f docker-compose.prod.yml build frontend && docker-compose -f docker-compose.prod.yml up -d frontend

# Veritabanını SQL Server'a bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Tüm veriyi sil (Dikkat!)
docker-compose -f docker-compose.prod.yml down -v
```

---

**Hazırlanan:** 9 Ocak 2026
**Server:** 31.186.24.78
**Kullanıcı:** huseyinadm
