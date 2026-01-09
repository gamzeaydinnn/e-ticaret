# 📋 SUNUCU DEPLOY - HAZIR DOSYALAR ÖZETİ

## 🎯 Oluşturulan Dosyalar

Sunucuya temiz deploy için 4 farklı dokümantasyon dosyası hazırlandı:

### 1️⃣ **SUNUCU_DEPLOY_CHECKLIST.md** ⭐ (EN TEMEL)
- **Kullanım:** Tüm deployment adımlarını sırayla takip et
- **Format:** Madde madde, detaylı açıklamalarla
- **İçerik:** 10 faza, kontrol noktaları, troubleshooting
- **Hedef:** Yeni başlayan veya en güvenli yaklaşım isteyen

### 2️⃣ **TEMIZ_DEPLOY_KOMUTLARI.md** (DETAYLI)
- **Kullanım:** Tüm komutlar açıklamalı şekilde
- **Format:** Bash script formatında (kopyala-yapıştır yapabilir)
- **İçelik:** 11 bölüm, tüm detaylar, monitoring komutları
- **Hedef:** Deneyimli DevOps veya teknik detay isteyenler

### 3️⃣ **SUNUCU_DEPLOY_OZET.md** (HIZLI REFERANS)
- **Kullanım:** Hızlı komut araması
- **Format:** Kisa komutlar, tablolaş
- **İçerik:** Copy-paste komutları, Nginx, SSL, Troubleshooting
- **Hedef:** Deployment sırasında hızlı referans

### 4️⃣ **TEMIZ_DEPLOY_KOMUTLARI.sh** (BASH SCRIPT)
- **Kullanım:** Sunucuda doğrudan çalıştırılabilir
- **Format:** Bash script (#!/bin/bash)
- **İçerik:** Tüm komutlar, yorum satırları
- **Hedef:** Automation, batch işlemler

### 5️⃣ **SUNUCU_DEPLOY.ps1** (WINDOWS POWERSHELL - INTERAKTIF)
- **Kullanım:** Windows'ta çalıştırmak için
- **Format:** PowerShell menü sistemi
- **İçerik:** 6 farklı seçenek, renkli output
- **Hedef:** Windows kullanıcıları

---

## 🚀 HIZLI BAŞLANGAÇ

### SEÇENEK 1: Adım Adım (En Güvenli)
```
1. SUNUCU_DEPLOY_CHECKLIST.md aç
2. Faza 1'den başla
3. Her faza tamamla
4. Sonuç kontrolü yap
```

### SEÇENEK 2: Copy-Paste (Hızlı)
```
1. SUNUCU_DEPLOY_OZET.md aç
2. "Tüm komutlar bir arada" bölümünü kopyala
3. Sunucuya yapıştır
4. Bekle
5. Son kontroller yap
```

### SEÇENEK 3: Script ile (Otomatik)
```bash
# Sunucuda çalıştır
bash TEMIZ_DEPLOY_KOMUTLARI.sh
```

### SEÇENEK 4: Windows'tan (İnteraktif)
```powershell
# Windows PowerShell'de
.\SUNUCU_DEPLOY.ps1
```

---

## 📊 SUNUCU BİLGİLERİ

```
IP Adresi: 31.186.24.78
Port: 22
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
Proje Dizini: /home/huseyinadm/eticaret
```

---

## 🔧 ÖNCESİ VE SONRASI

### KODU DEĞIŞTIRME (Yerel Makinede)
- ✅ `categoryService.js` oluşturuldu
- ✅ `.env.production` API URL ayarlandı
- ✅ `appsettings.json` CORS domain'leri eklendi
- ✅ Docker compose production API URL güncellendi

### SUNUCUYA DEPLOY SIRASIYLA
1. SSH Bağlantısı
2. Eski Deployment Temizle (Veritabanı dahil)
3. Kodu GitHub'dan Çek
4. Environment Dosyası Oluştur
5. Docker Build
6. Container Başlat
7. Migration Bekle
8. Sağlık Kontrol
9. Nginx Setup (İsteğe Bağlı)
10. SSL Sertifikası (İsteğe Bağlı)

---

## ✅ BAŞARILI DEPLOYMENT İŞARETLERİ

Aşağıdakilerden biri çalışmışsa deployment başarılı:

- ✅ `curl http://localhost:5000/api/health` - 200 OK
- ✅ `curl -I http://localhost:3000` - 200 OK
- ✅ `docker-compose ps` - Tüm container'lar Up
- ✅ Veritabanı 50+ ürünle dolu
- ✅ https://golkoygurme.com.tr/ açılıyor
- ✅ Admin paneline girilebiliyor (admin/admin123)

---

## 🎯 DEPLOYMENTİN AMACI

- ✅ Tüm veri temizlenip sıfırdan başlanması
- ✅ Production ortamında çalışan bir sistem
- ✅ HTTPS SSL sertifikası ile güvenli
- ✅ CORS ayarlarının production domain'leri kapsayması
- ✅ NetGSM SMS entegrasyonunun çalışması
- ✅ Veritabanının seed data'yla dolu olması
- ✅ Admin panelinin erişilebilir olması

---

## 📱 CIHAZ GEREKSINMELERÍ

### Sunucu Gereksinimleri
- ✅ Docker
- ✅ Docker Compose
- ✅ Git
- ✅ Nginx (optional)
- ✅ Certbot (optional)

### Yerel Makine Gereksinimleri
- ✅ SSH Client (Windows 10+, Mac, Linux built-in)
- ✅ Git (versiyon kontrol)
- ✅ Text Editor (dosya düzenleme)

---

## 🆘 HIZLI ÇÖZÜMLER

| Problem | Çözüm |
|---------|-------|
| SSH bağlantısı yok | IP/Port/User kontrol et |
| Build hatası | `docker-compose build --no-cache` |
| Port kullanımında | `sudo lsof -i :5000` ile kontrol et |
| Veri yüklenmedi | `docker logs ecommerce-api-prod` kontrol et |
| CORS hatası | `.env` dosyasındaki domain'leri kontrol et |
| API yanıt vermiyor | `curl localhost:5000/api/health` test et |

---

## 📚 İLGİLİ DOSYALAR

```
c:/Users/GAMZE/Desktop/eticaret/
├── SUNUCU_DEPLOY_CHECKLIST.md      ← BURADAN BAŞLA
├── TEMIZ_DEPLOY_KOMUTLARI.md       (Detaylı versiyon)
├── SUNUCU_DEPLOY_OZET.md           (Özet versiyon)
├── TEMIZ_DEPLOY_KOMUTLARI.sh       (Bash script)
├── SUNUCU_DEPLOY.ps1               (PowerShell menü)
├── docker-compose.prod.yml         (Production config)
├── frontend/.env.production        (Frontend config)
├── .env                            (Backend config)
└── src/ECommerce.API/appsettings.json (API config)
```

---

## 🎓 ÖĞRENİLECEK NOTLAR

### Neden 0'dan deploy ediyoruz?
- Eski veritabanı kalıntılarını temizlemek
- Production ayarlarını sıfır saymaktan kontrol etmek
- Verileri yeni baştan yüklemek
- Test ortamını temizlemek

### Her Faza Neden Gerekli?
1. **Temizlik** → Eski sorunlar kalmaması
2. **Kod Güncelleme** → En son sürüm çekmek
3. **Config** → Production ayarları
4. **Build** → Yeni image'lar oluşturmak
5. **Container** → Servisleri başlatmak
6. **Migration** → Veritabanını kurmak
7. **Test** → Sistemin çalıştığını doğrulamak

### Güvenlik Notları
- ✅ JWT secret'i güçlü tutun
- ✅ Database password'ü değiştirin (production'da)
- ✅ SSH key-based auth kullanın (password yerine)
- ✅ Firewall kurallarını katılaştırın
- ✅ SSL sertifikası zorunlu olmalı

---

## 🔐 ÖNEMLİ HATIRLATMALAR

⚠️ **UYARILAR:**
1. Deployment sırasında TÜM veri silinecektir
2. Veritabanı yedek almadan yapıştırılır
3. Bu işlem geri alınamaz
4. Admin şifresi hardcoded'dir (güvenlik açığı!)

✅ **ÖNERİLER:**
1. İlk kez production'a gitmeden önce testlerde dene
2. Her deployment'tan sonra kontrol listesini takip et
3. Logları takip et (Faza 7)
4. Hata olursa troubleshooting'e bak

---

## 📞 DESTEK

Eğer sorun yaşıyorsanız:

1. SUNUCU_DEPLOY_OZET.md'deki Troubleshooting bölümüne bak
2. Docker loglarını kontrol et (`docker-compose logs -f`)
3. Port kullanımını kontrol et (`sudo lsof -i`)
4. Network bağlantısını test et (`curl localhost:5000/api/health`)
5. Veritabanı bağlantısını test et (`docker exec ...`)

---

**Son Güncelleme:** 9 Ocak 2026  
**Sunucu:** 31.186.24.78  
**Proje:** GolkoyGurme E-Ticaret  
**Durumu:** 🟢 Deployment Hazır
