# 🚀 SUNUCU DEPLOY KOMUTLARI - ÖZETİ (Bu Dosyaya Bakın!)

## 📌 SUNUCU BİLGİLERİ
```
IP: 31.186.24.78
Port: 22
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
```

---

## 🎯 BAŞLAMADAN ÖNCE

1. **SSH Bağlantısı Kur:**
   ```bash
   ssh huseyinadm@31.186.24.78
   # Şifre: Passwd1122FFGG
   cd /home/huseyinadm/eticaret
   ```

2. **UYARI:** Aşağıdaki komutlar TÜM VERİYİ SİLECEKTİR!

---

## 🔴 KOMUTLAR - SIRAYLA ÇalıŞTIR

### ADIM 1: ESKİ DEPLOYMENTİ TEMİZLE (Veritabanı dahil!)
```bash
docker-compose -f docker-compose.prod.yml down -v
docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true
docker image prune -f
rm -rf logs/*
```

### ADIM 2: KOD GÜNCELLE
```bash
git pull origin main
```

### ADIM 3: .ENV DOSYASINI OLUŞTUR (BİR BÜTÜN YAPISTIR)
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

### ADIM 4: DOCKER BUILD (⏱️ ~5 dakika, bekleyin)
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

### ADIM 5: CONTAINER'LARI BAŞLAT
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### ADIM 6: CONTAINER DURUMUNU KONTROL ET
```bash
docker-compose -f docker-compose.prod.yml ps
```
**Beklenen:** Tüm servislerin "Up" durumda olması

### ADIM 7: API LOGLARINI TAKIP ET (Migration bitene kadar bekle)
```bash
docker-compose -f docker-compose.prod.yml logs api -f
```
**Beklenen:** "All seed operations completed successfully" mesajı

**CTRL+C ile çık**

### ADIM 8: VERITABANINI KONTROL ET
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

Bağlandıktan sonra:
```sql
SELECT COUNT(*) as [Ürün] FROM ECommerceDb.dbo.Products;
GO
EXIT
```

### ADIM 9: SERVIS SAĞLIĞINI KONTROL ET
```bash
curl http://localhost:5000/api/health
curl -I http://localhost:3000
```

### ADIM 10: NGINX KURULUMU (HTTPS için - ÖNERİLEN)
```bash
# Nginx ve SSL Kur
sudo apt install -y nginx certbot python3-certbot-nginx

# SSL Sertifikası Oluştur
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr

# Nginx Config Oluştur (HTTPS ile)
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

# Nginx'i Etkinleştir ve Başlat
sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## ✅ BAŞARI KRİTERLERİ (Tüm Bunlar Olmalı)

- [ ] SSH bağlantısı kuruldu
- [ ] `docker ps` tüm container'ları gösteriyor
- [ ] `curl localhost:5000/api/health` - 200 OK
- [ ] Veritabanında 50+ ürün var
- [ ] `https://golkoygurme.com.tr/` açılıyor (HTTPS!)
- [ ] Admin paneline girilebiliyor (admin / admin123)

---

## 🆘 SORUN GIDERMEK

```bash
# API başlamıyor
docker-compose -f docker-compose.prod.yml logs api

# Veritabanı sorunu
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"

# Port çakışması
sudo lsof -i :5000

# Tüm veriyi sil ve yeni başla
docker-compose -f docker-compose.prod.yml down -v && docker system prune -a -f && docker-compose -f docker-compose.prod.yml build --no-cache && docker-compose -f docker-compose.prod.yml up -d
```

---

## 📚 DAHA DETAYLI DOKÜMANTASYON

- **SUNUCU_DEPLOY_CHECKLIST.md** - Adım adım kontrol listesi
- **TEMIZ_DEPLOY_KOMUTLARI.md** - Tüm detaylar
- **SUNUCU_DEPLOY_OZET.md** - Hızlı referans

---

**Hazır mısınız? Başlayın!** 🚀
