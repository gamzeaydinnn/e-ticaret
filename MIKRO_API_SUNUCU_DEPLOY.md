# 🔄 MİKRO API SUNUCU DEPLOY REHBERİ

**Tarih:** 2026-02-06  
**Sunucu:** 31.186.24.78  
**Amaç:** Mikro ERP API entegrasyonunu sunucuda aktif etmek

---

## 📋 ÖN GEREKSİNİMLER

1. ✅ Sunucuda Mikro API servisi çalışıyor (port 8094)
2. ✅ Docker container'lar aktif
3. ✅ SSH erişimi mevcut

---

## 🔍 ADIM 1: SUNUCUYA BAĞLAN VE MİKRO API KONTROLÜ

```bash
# 1. SSH ile bağlan
ssh huseyinadm@31.186.24.78

# 2. Mikro API'nin çalışıp çalışmadığını kontrol et
curl -k https://localhost:8094/Api/APIMethods/HealthCheck

# 3. Port 8094'ün dinlenip dinlenmediğini kontrol et
sudo netstat -tulpn | grep 8094

# 4. Mikro API servisinin durumu (eğer systemd ile çalışıyorsa)
sudo systemctl status mikro-api  # veya ilgili servis adı
```

---

## 📁 ADIM 2: DOSYALARI SUNUCUYA KOPYALA

### Windows PowerShell'den (Yerel Bilgisayardan):

```powershell
# 1. Proje klasörüne git
cd C:\Users\GAMZE\Desktop\eticaret

# 2. Docker compose dosyasını kopyala
scp docker-compose.prod.yml huseyinadm@31.186.24.78:/root/eticaret/

# 3. Production appsettings'i kopyala
scp src/ECommerce.API/appsettings.Production.json huseyinadm@31.186.24.78:/root/eticaret/src/ECommerce.API/

# 4. Nginx konfigürasyonunu kopyala
scp deploy/nginx-golkoygurme.conf huseyinadm@31.186.24.78:/tmp/

# 5. Backend source kodunu kopyala (değişiklikler varsa)
scp -r src/* huseyinadm@31.186.24.78:/root/eticaret/src/
```

---

## ⚙️ ADIM 3: SUNUCUDA NGİNX KONFIGÜRASYONU

```bash
# Sunucuya bağlı iken:

# 1. Nginx config dosyasını yerine kopyala
sudo cp /tmp/nginx-golkoygurme.conf /etc/nginx/sites-available/golkoygurme

# 2. Symlink oluştur (eğer yoksa)
sudo ln -sf /etc/nginx/sites-available/golkoygurme /etc/nginx/sites-enabled/golkoygurme

# 3. Nginx konfigürasyonunu test et
sudo nginx -t

# 4. Başarılı ise nginx'i yeniden yükle
sudo systemctl reload nginx
```

---

## 🐳 ADIM 4: DOCKER CONTAINER'LARI GÜNCELLE

```bash
# Sunucuya bağlı iken:

# 1. Proje dizinine git
cd /root/eticaret

# 2. Mevcut container'ları durdur
docker-compose -f docker-compose.prod.yml down

# 3. Backend image'ı yeniden oluştur (Mikro ayarları ile)
docker-compose -f docker-compose.prod.yml build api

# 4. Container'ları başlat
docker-compose -f docker-compose.prod.yml up -d

# 5. Container durumlarını kontrol et
docker-compose -f docker-compose.prod.yml ps

# 6. API loglarını izle (Mikro bağlantı mesajlarını görmek için)
docker logs ecommerce-api-prod -f --tail 100
```

---

## ✅ ADIM 5: MİKRO API BAĞLANTI TESTİ

```bash
# 1. Backend container'ından Mikro API'ye erişim testi
docker exec ecommerce-api-prod curl -k https://host.docker.internal:8094/Api/APIMethods/HealthCheck

# 2. Backend loglarında Mikro bağlantı mesajlarını ara
docker logs ecommerce-api-prod 2>&1 | grep -i "mikro"

# 3. API üzerinden Mikro durumunu kontrol et
curl http://localhost:5000/api/mikro/status

# 4. Hangfire dashboard'u kontrol et (senkronizasyon job'ları)
curl http://localhost:5000/hangfire
```

---

## 🔧 ADIM 6: MİKRO API SORUN GİDERME

### Senaryo 1: Container'dan host.docker.internal'a erişilemiyor

```bash
# Extra hosts ayarını kontrol et
docker inspect ecommerce-api-prod | grep -A5 "ExtraHosts"

# Manuel olarak extra_hosts ekle (gerekirse)
docker run --add-host=host.docker.internal:host-gateway ...
```

### Senaryo 2: Mikro API SSL Sertifika Hatası

```bash
# Self-signed sertifika kullanılıyorsa, backend'de SSL doğrulamasını atla
# appsettings.Production.json'da:
# "MikroSettings": {
#     "IgnoreSslErrors": true
# }
```

### Senaryo 3: Mikro API Port Açık Değil

```bash
# Firewall kurallarını kontrol et
sudo ufw status
sudo ufw allow 8094/tcp  # Sadece yerel erişim için gerekli değil

# iptables kontrol
sudo iptables -L -n | grep 8094
```

### Senaryo 4: Mikro API Şifre/Auth Hatası

```bash
# Backend loglarında auth hatalarını ara
docker logs ecommerce-api-prod 2>&1 | grep -i "auth\|password\|sifre\|unauthorized"

# MD5 hash kontrolü - şifre günlük olarak hash'leniyor
# Format: YYYY-MM-DD + Şifre → MD5
```

---

## 📊 ADIM 7: SENKRONİZASYON JOB'LARINI KONTROL ET

```bash
# 1. Hangfire dashboard'a git (tarayıcıdan)
# https://golkoygurme.com.tr/hangfire

# 2. Recurring Jobs sekmesini kontrol et:
# - mikro-stock-sync (her 15 dakika)
# - mikro-price-sync (saatlik)
# - mikro-full-sync (günlük, saat 06:00)
# - mikro-order-push (her 5 dakika)

# 3. Job'ları manuel tetikle (test için)
curl -X POST http://localhost:5000/api/mikro/trigger-sync?jobName=stock-sync
```

---

## 📝 HIZLI KONTROL LİSTESİ

| Kontrol                          | Komut                                                                                                 | Beklenen Sonuç             |
| -------------------------------- | ----------------------------------------------------------------------------------------------------- | -------------------------- |
| Mikro API çalışıyor mu?          | `curl -k https://localhost:8094/Api/APIMethods/HealthCheck`                                           | 200 OK                     |
| Docker Mikro'ya erişebiliyor mu? | `docker exec ecommerce-api-prod curl -k https://host.docker.internal:8094/Api/APIMethods/HealthCheck` | 200 OK                     |
| Backend başladı mı?              | `docker logs ecommerce-api-prod \| grep "started"`                                                    | Application started        |
| Mikro Settings yüklendi mi?      | `docker logs ecommerce-api-prod \| grep "MicroService"`                                               | "Başlatıldı. API URL: ..." |
| Hangfire job'ları kayıtlı mı?    | `docker logs ecommerce-api-prod \| grep "MikroJobScheduler"`                                          | "X job kaydedildi"         |
| Nginx çalışıyor mu?              | `sudo nginx -t && curl -I https://golkoygurme.com.tr`                                                 | 200 OK                     |

---

## 🚨 ACİL DURUM: GERİ ALMA

Eğer Mikro entegrasyonu sorun çıkarırsa, job'ları devre dışı bırakabilirsiniz:

```bash
# 1. docker-compose.prod.yml'de şu satırı değiştir:
# - MikroSync__JobsEnabled=true → false

# 2. Container'ı yeniden başlat
docker-compose -f docker-compose.prod.yml restart api

# veya alternatif olarak:
docker exec ecommerce-api-prod bash -c 'export MikroSync__JobsEnabled=false'
```

---

## 📞 DESTEK

Sorun yaşarsanız:

1. Backend loglarını kontrol edin: `docker logs ecommerce-api-prod -f`
2. Mikro API loglarını kontrol edin (sunucuda)
3. Nginx error loglarını kontrol edin: `sudo tail -f /var/log/nginx/error.log`

---

**Son Güncelleme:** 2026-02-06
