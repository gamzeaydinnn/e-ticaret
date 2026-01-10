# 🚀 SUNUCU FRONTEND GÜNCELLEME - HIZLI KOMUTLAR

## 📌 DURUM

- ✅ Yerel PC (localhost:3000) = **ÇALIŞIYOR**
- ❌ Sunucu (31.186.24.78:3000) = **GÜNCELLENMELI**

## 🖥️ SUNUCU KOMUTLARI

### SEÇENEK 1: Otomatik Script (Önerilen)

**Sunucuya bağlan:**

```bash
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
```

**Script'i indir ve çalıştır:**

```bash
cd ~
# GitHub'dan doğrudan indirip çalıştır
curl -s https://raw.githubusercontent.com/gamzeaydinnn/e-ticaret/main/SUNUCU_FRONTEND_GUNCELLE.sh | bash
```

Veya sunucuya transfer edip çalıştır:

```bash
# PC'den upload et (PowerShell)
scp C:\Users\GAMZE\Desktop\eticaret\SUNUCU_FRONTEND_GUNCELLE.sh huseyinadm@31.186.24.78:~/

# Sunucuda
ssh huseyinadm@31.186.24.78
chmod +x ~/SUNUCU_FRONTEND_GUNCELLE.sh
./SUNUCU_FRONTEND_GUNCELLE.sh
```

---

### SEÇENEK 2: Manuel Komutlar

```bash
# 1. Sunucu sunucuya bağlan
ssh huseyinadm@31.186.24.78

# 2. Proje klasörüne git
cd ~/eticaret

# 3. Frontend konteynerini durdur
docker-compose -f docker-compose.prod.yml stop frontend

# 4. 5 saniye bekle
sleep 5

# 5. GitHub'dan son kodu çek
git fetch origin
git pull origin main

# 6. Frontend image'ını rebuild et (yeni kodu alsın)
docker-compose -f docker-compose.prod.yml build --no-cache frontend

# 7. Frontend'i başlat
docker-compose -f docker-compose.prod.yml up -d frontend

# 8. 10 saniye bekle başlaması için
sleep 10

# 9. Kontrol et
docker-compose -f docker-compose.prod.yml ps frontend

# 10. Tarayıcıda test et
echo "Frontend: http://31.186.24.78:3000"
```

---

## 🔍 SORUN GİDERME

### Frontend yanıt vermiyor (HTTP 000)

```bash
# Log'ları kontrol et
docker-compose -f docker-compose.prod.yml logs frontend

# Container'ı manuel başlat (ayrıntılı çıktı görmek için)
docker-compose -f docker-compose.prod.yml up frontend
```

### Port 3000'de başka bir servis çalışıyor

```bash
# Port 3000'de ne çalışıyor kontrol et
sudo lsof -i :3000
# veya
docker ps | grep 3000
```

### Build sırasında hata veriyorsa

```bash
# Docker diskini temizle
docker system prune -af
docker volume prune -f

# Tekrar build et
docker-compose -f docker-compose.prod.yml build --no-cache frontend
```

---

## ✅ BAŞARILI DEPLOY KONTROL

```bash
# 1. Container çalışıyor mu?
docker-compose -f docker-compose.prod.yml ps frontend

# 2. HTTP 200 dönüyor mu?
curl -I http://localhost:3000

# 3. Kategoriler görünüyor mu?
curl -s http://localhost:5000/api/categories | head -50

# 4. Log'lar temiz mi?
docker-compose -f docker-compose.prod.yml logs frontend | tail -20
```

---

## 📊 DEPLOYMENT TIMELINE

| Adım               | Bekleme      | Açıklama                |
| ------------------ | ------------ | ----------------------- |
| 1. Frontend stop   | Anında       | Container durdurulur    |
| 2. Git pull        | 5-10s        | Kod indirilir           |
| 3. Docker build    | 3-5 min      | Image rebuild edilir    |
| 4. Container start | 5-10s        | Yeni image başlatılır   |
| 5. Health check    | 5-10s        | Port açılır, hazır olur |
| **TOPLAM**         | **~4-5 min** |                         |

---

## 🎯 BEKLENEN SONUÇ

✅ **BAŞARILI:**

- http://31.186.24.78:3000 açılıyor
- Kategoriler görünüyor
- Log'larda hata yok
- API'yle iletişim kurabiliyor

❌ **BAŞARISIZ YAKLAŞMALAR:**

- `docker-compose.prod.yml restart frontend` ← Eski image'ı başlatır!
- `docker restart ecommerce-frontend-prod` ← Rebuild yapmaz!
- Código değiştirip `docker-compose up -d` ← Image rebuild etmez!

**Doğru yaklaşım = build --no-cache + up -d**

---

**Son Güncelleme**: 2026-01-10
