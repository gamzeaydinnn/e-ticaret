# ⚡ HIZLI DEPLOY KOMUTLARI

## KOPYALA-YAPISTIR HAZIR!

### 🔥 TEK KOMUT DEPLOY (Hepsi Bir Arada)

```bash
ssh root@31.186.24.78 << 'ENDSSH'
cd /home/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build
echo "✅ DEPLOY TAMAMLANDI!"
echo "⏳ Backend başlatılıyor (30 saniye bekleyin)..."
sleep 30
docker ps
curl http://localhost:5000/health
echo "🎯 Test: https://golkoygurme.com.tr/admin"
ENDSSH
```

---

### 📋 ADIM ADIM (Manuel Kontrol İçin)

**1. SSH + Deploy:**

```bash
ssh root@31.186.24.78
cd /home/eticaret
git pull origin main
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build
```

**2. Log İzle:**

```bash
docker logs -f ecommerce-api-prod
# CTRL+C ile çık
```

**3. Durum Kontrol:**

```bash
docker ps
curl http://localhost:5000/health
```

---

### 🧪 API TEST

**Backend Health:**

```bash
curl http://localhost:5000/health
```

**Admin Login + Token Al:**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"admin123"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo "Token: $TOKEN"
```

**Siparişleri Listele:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/admin/orders | head -30
```

**Sipariş Durumunu Güncelle (Test):**

```bash
curl -X PUT http://localhost:5000/api/admin/orders/1011/status \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"status":"preparing"}'
```

**Güncellenmiş Siparişi Kontrol:**

```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/admin/orders/1011 | grep -o '"status":"[^"]*'
```

---

### 🛠️ SORUN GİDERME

**Backend Log'ları Kontrol:**

```bash
docker logs ecommerce-api-prod | tail -50
```

**Frontend Log'ları Kontrol:**

```bash
docker logs ecommerce-frontend-prod | tail -50
```

**SQL Server Log'ları Kontrol:**

```bash
docker logs ecommerce-sql-prod | tail -50
```

**Container'ları Yeniden Başlat (Hızlı Fix):**

```bash
docker-compose -f docker-compose.prod.yml restart
```

**Tüm Container'ları Temizle ve Yeniden Başlat:**

```bash
docker-compose -f docker-compose.prod.yml down
docker system prune -f
docker-compose -f docker-compose.prod.yml up -d --build
```

---

### 📊 ÖNEMLİ KONTROLLER

**1. .env.production Kontrolü (Lokal Makine):**

```bash
cat frontend/.env.production
# REACT_APP_API_URL= (BOŞ OLMALI ✓)
```

**2. Nginx Config Kontrolü (Sunucu):**

```bash
cat /etc/nginx/sites-available/golkoygurme | grep -A 10 "location /api"
```

**3. Docker Volume Kontrolü (Veriler Korunuyor mu?):**

```bash
docker volume ls | grep eticaret
# eticaret_sqlserver-data OLMALI ✓
```

**4. Uploads Klasörü Kontrolü:**

```bash
ls -lah /home/eticaret/uploads | head -10
```

---

### 🎯 TARAYICIDA TEST URL'LERİ

- **Ana Sayfa:** https://golkoygurme.com.tr
- **Admin Panel:** https://golkoygurme.com.tr/admin
- **Siparişler:** https://golkoygurme.com.tr/admin/orders
- **Kurye Panel:** https://golkoygurme.com.tr/courier
- **Mağaza Görevlisi:** https://golkoygurme.com.tr/store

**Admin Login:**

- Email: `admin@admin.com`
- Password: `admin123`

---

### ⚙️ NGINX YENİDEN BAŞLATMA (Gerekirse)

```bash
sudo nginx -t                    # Config test
sudo systemctl restart nginx     # Nginx yeniden başlat
sudo systemctl status nginx      # Durum kontrol
```

---

### 🔐 SSL SERTİFİKA YENİLEME (3 ayda bir)

```bash
sudo certbot renew --nginx
sudo systemctl restart nginx
```

---

### 📱 MOBİL TEST

**QR Kod ile Test:**

```bash
# Sunucuda qrencode yüklü değilse:
sudo apt install qrencode -y

# QR kod oluştur
qrencode -t ANSI "https://golkoygurme.com.tr"
```

Telefonda kameraya tut, siteyi aç!

---

## ✅ BAŞARILI DEPLOY KONTROLÜ

Deploy başarılı mı? Bu kontrolleri yap:

```bash
# 1. Container'lar çalışıyor mu?
docker ps | grep -E "api|frontend|sql"

# 2. Backend sağlıklı mı?
curl http://localhost:5000/health

# 3. Frontend açılıyor mu?
curl -I https://golkoygurme.com.tr | grep "200 OK"

# 4. Admin API çalışıyor mu?
curl -I http://localhost:5000/api/admin/orders

# 5. Kurye API çalışıyor mu?
curl -I http://localhost:5000/api/courier/orders
```

**Hepsi ✅ ise deploy başarılı!**

---

## 🎉 DEPLOY TAMAMLANDI!

Artık şunları yapabilirsiniz:

1. ✅ Admin panelden sipariş durumlarını değiştirme
2. ✅ Pending → Preparing geçişi
3. ✅ Modal ortalanmış görünüm
4. ✅ Tüm status değerleri çalışıyor (preparing, ready, assigned, out_for_delivery, delivered)
5. ✅ Kurye API endpoint'leri hazır

**Test için:** https://golkoygurme.com.tr/admin/orders
