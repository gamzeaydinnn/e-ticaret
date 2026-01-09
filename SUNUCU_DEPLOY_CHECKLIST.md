# 🚀 SUNUCUYA TEMIZ DEPLOY - MADDE MADDE TÜM KOMUTLAR

## 📋 SUNUCU BİLGİLERİ
```
IP: 31.186.24.78
Port: 22
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
Proje Dizini: /home/huseyinadm/eticaret
```

---

## 🎯 DEPLOYMENT AKIŞI (10 FAZA)

### ✅ FAZA 1: SSH BAĞLANTISI

**1.1** PowerShell'i açın

**1.2** SSH komutu çalıştırın:
```bash
ssh huseyinadm@31.186.24.78
```

**1.3** Şifre girin:
```
Passwd1122FFGG
```

**1.4** Bağlandıktan sonra proje dizinine gidin:
```bash
cd /home/huseyinadm/eticaret
```

**Kontrol:** Eğer prompt `huseyinadm@...:/home/huseyinadm/eticaret$` görüyorsanız ✅

---

### ✅ FAZA 2: ESKİ DEPLOYMENT'I TEMİZLE (⚠️ TÜM VERİ SİLİNİR!)

**2.1** Tüm container'ları ve volume'ları durdur ve sil:
```bash
docker-compose -f docker-compose.prod.yml down -v
```

**2.2** Docker image'larını sil:
```bash
docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true
```

**2.3** Dangling image'ları temizle:
```bash
docker image prune -f
```

**2.4** Logs klasörünü temizle:
```bash
rm -rf logs/*
```

**Kontrol:** `docker-compose -f docker-compose.prod.yml ps` boş çıkmalı

---

### ✅ FAZA 3: KOD GÜNCELLE

**3.1** GitHub'dan son kodu çek:
```bash
git pull origin main
```

**3.2** Dosyaları kontrol et:
```bash
ls -la
```

**Beklenen:** `frontend/`, `src/`, `docker-compose.prod.yml` dosyaları görünmeli

---

### ✅ FAZA 4: ENVIRONMENT DOSYASINI OLUŞTUR

**4.1** `.env` dosyası oluştur (aşağıdaki komutu BİR BÜTÜN olarak yapıştır):
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

**4.2** Dosyayı kontrol et:
```bash
cat .env
```

**Beklenen:** Tüm değerleri içeren dosya görünmeli

---

### ✅ FAZA 5: DOCKER IMAGE'LARINI OLUŞTUR (⏱️ ~5 dakika)

**5.1** Tüm image'ları yeniden oluştur (cache'i kullanma):
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

**Beklenen:** "Successfully built" veya "Successfully tagged" mesajları

---

### ✅ FAZA 6: CONTAINER'LARI BAŞLAT

**6.1** Tüm serviseri başlat:
```bash
docker-compose -f docker-compose.prod.yml up -d
```

**Beklenen:** "Creating" ve "Starting" mesajları

**6.2** Container durumunu kontrol et:
```bash
docker-compose -f docker-compose.prod.yml ps
```

**Beklenen:** 
```
NAME                      STATUS
ecommerce-sql-prod        Up
ecommerce-api-prod        Up
ecommerce-frontend-prod   Up
```

---

### ✅ FAZA 7: VERITABANINI BAŞLATMA VE MIGRATION

**7.1** API loglarını canlı takip et (veritabanı migration'ını izlemeye göz at):
```bash
docker-compose -f docker-compose.prod.yml logs api -f
```

**Beklenen:** Sonunda bu mesajı göreceksiniz:
```
✅✅✅ TÜM SEED İŞLEMLERİ BAŞARIYLA TAMAMLANDI! ✅✅✅
```

**7.2** Loglardan çık (CTRL+C tuşlarına basın)

**7.3** 30 saniye bekle:
```bash
sleep 30
```

---

### ✅ FAZA 8: VERITABANINI KONTROL ET

**8.1** SQL Server'a bağlan:
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

**8.2** Veritabanını listele:
```sql
SELECT name FROM sys.databases;
GO
```

**Beklenen:** `ECommerceDb` veritabanı görünmeli

**8.3** Ürün sayısını kontrol et:
```sql
USE ECommerceDb
GO
SELECT COUNT(*) as [Ürün Sayısı] FROM Products;
GO
```

**Beklenen:** 50'den fazla ürün olmalı

**8.4** Çık:
```sql
EXIT
```

---

### ✅ FAZA 9: SERVIS SAĞLIĞINI KONTROL ET

**9.1** API Health Check:
```bash
curl http://localhost:5000/api/health
```

**Beklenen:** JSON yanıt veya `{"status":"Healthy"}` gibi bir cevap

**9.2** Frontend Check:
```bash
curl -I http://localhost:3000
```

**Beklenen:** `HTTP/1.1 200 OK` veya `HTTP/1.1 301` (redirect)

**9.3** Tüm logları son 50 satırda kontrol et:
```bash
docker-compose -f docker-compose.prod.yml logs --tail=50
```

**Beklenen:** Hata mesajı olmamalı, sadece info logları

---

### ✅ FAZA 10: NGINX VE HTTPS SETUP (İsteğe Bağlı ama ÖNERİLEN)

**10.1** Nginx kurulumu:
```bash
sudo apt install -y nginx certbot python3-certbot-nginx
```

**10.2** Let's Encrypt SSL Sertifikası:
```bash
sudo certbot certonly --nginx -d golkoygurme.com.tr -d www.golkoygurme.com.tr
```

**Not:** Email adresi soracak, geliştiricinin emaili kullan

**10.3** Nginx konfigurasyonu (HTTPS ile):
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

**10.4** Nginx config'i etkinleştir:
```bash
sudo ln -s /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/
```

**10.5** Nginx test et:
```bash
sudo nginx -t
```

**Beklenen:** "successful" mesajı

**10.6** Nginx'i başlat:
```bash
sudo systemctl restart nginx
```

---

## 📊 SON KONTROLLER (HEPSİ BAŞARILI OLMALI)

| Komut | Beklenen Sonuç |
|-------|----------------|
| `docker-compose -f docker-compose.prod.yml ps` | Tüm container'lar "Up" |
| `curl http://localhost:5000/api/health` | JSON yanıt |
| `curl -I http://localhost:3000` | 200 OK |
| `curl -I https://golkoygurme.com.tr` | 200 OK (SSL varsa) |
| `docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products"` | 50+ sonuç |

---

## 🐳 HIZLI REFERANS KOMUTLARI

```bash
# Container Durumu
docker-compose -f docker-compose.prod.yml ps

# Logları Takip Et
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API
docker-compose -f docker-compose.prod.yml logs -f api

# Sadece Frontend
docker-compose -f docker-compose.prod.yml logs -f frontend

# Sadece Database
docker-compose -f docker-compose.prod.yml logs -f sqlserver

# Servisleri Başlat
docker-compose -f docker-compose.prod.yml up -d

# Servisleri Durdur
docker-compose -f docker-compose.prod.yml down

# Kaynakları Göster
docker stats

# API Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build api
docker-compose -f docker-compose.prod.yml up -d api

# Frontend Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build frontend
docker-compose -f docker-compose.prod.yml up -d frontend

# SQL Server Bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Disk Kullanımı
du -sh /home/huseyinadm/eticaret
df -h

# Tüm Veriyi Sil (DİKKAT!)
docker-compose -f docker-compose.prod.yml down -v
```

---

## ⚠️ SORUN ÇÖZMEK

### API Çalışmıyor
```bash
# Logları kontrol et
docker-compose -f docker-compose.prod.yml logs api

# Container içinde test et
docker exec ecommerce-api-prod curl -s http://localhost:5000/api/health
```

### Veritabanı Bağlantı Sorunu
```bash
# SQL Server'a bağlan ve test et
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"
```

### Port Çakışması
```bash
# Hangi process'in port'u kullandığını göster
sudo lsof -i :5000
sudo lsof -i :3000

# Proccess'i kill et
sudo kill -9 [PID]
```

### Tüm Veriyi Sil ve Yeni Başla
```bash
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d
```

---

## ✅ BAŞARILI DEPLOYMENT ÖZETİ

Aşağıdakiler tamamlandığında deployment **100% başarılı**:

1. ✅ SSH bağlantısı kuruldu
2. ✅ Eski deployment temizlendi
3. ✅ Kod GitHub'dan çekildi
4. ✅ .env dosyası oluşturuldu
5. ✅ Docker image'ları build edildi
6. ✅ Container'lar başlatıldı
7. ✅ Veritabanı migration'ı tamamlandı
8. ✅ Ürünler/Kategoriler/Kullanıcılar veritabanına yüklendi
9. ✅ API port 5000'de çalışıyor
10. ✅ Frontend port 3000'de çalışıyor
11. ✅ HTTPS çalışıyor: https://golkoygurme.com.tr/
12. ✅ API erişim: https://golkoygurme.com.tr/api/health

**Eğer tüm bunlar başarılı ise, site https://golkoygurme.com.tr adresinde canlı demektir!**

---

## 📞 İLETİŞİM

**Sunucu Yöneticisi:** huseyinadm@31.186.24.78  
**Proje:** GolkoyGurme E-Ticaret  
**Oluşturuldu:** 9 Ocak 2026
