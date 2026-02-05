# 🚀 MİKRO API DEPLOY - HIZLI KOMUTLAR

**Tarih:** 2026-02-06

---

## 📁 SUNUCUYA ATILACAK DOSYALAR

| #   | Dosya                                                  | Açıklama                   |
| --- | ------------------------------------------------------ | -------------------------- |
| 1   | `docker-compose.prod.yml`                              | Mikro API ayarları eklendi |
| 2   | `src/ECommerce.API/appsettings.Production.json`        | MikroSync enabled          |
| 3   | `src/ECommerce.API/Program.cs`                         | SSL bypass eklendi         |
| 4   | `src/ECommerce.Infrastructure/Config/MikroSettings.cs` | IgnoreSslErrors eklendi    |
| 5   | `deploy/nginx-golkoygurme.conf`                        | Hangfire route eklendi     |
| 6   | `deploy/deploy-mikro-api.sh`                           | Deploy script              |

---

## 🖥️ HIZLI DEPLOY (PowerShell)

```powershell
# 1. Proje dizinine git
cd C:\Users\GAMZE\Desktop\eticaret

# 2. Tüm dosyaları sunucuya kopyala (tek komut)
scp docker-compose.prod.yml huseyinadm@31.186.24.78:/root/eticaret/
scp src/ECommerce.API/appsettings.Production.json huseyinadm@31.186.24.78:/root/eticaret/src/ECommerce.API/
scp deploy/nginx-golkoygurme.conf huseyinadm@31.186.24.78:/tmp/
scp deploy/deploy-mikro-api.sh huseyinadm@31.186.24.78:/root/eticaret/

# 3. Backend source kodunu kopyala
scp -r src huseyinadm@31.186.24.78:/root/eticaret/
```

---

## 🐧 SUNUCU KOMUTLARI (SSH'den sonra)

```bash
# 1. Sunucuya bağlan
ssh huseyinadm@31.186.24.78

# 2. Nginx konfigürasyonunu güncelle
sudo cp /tmp/nginx-golkoygurme.conf /etc/nginx/sites-available/golkoygurme
sudo nginx -t && sudo systemctl reload nginx

# 3. Proje dizinine git ve deploy et
cd /root/eticaret
chmod +x deploy/deploy-mikro-api.sh
./deploy/deploy-mikro-api.sh
```

---

## ✅ DOĞRULAMA KOMUTLARI

```bash
# Mikro API çalışıyor mu?
curl -k https://localhost:8094/Api/APIMethods/HealthCheck

# Container'dan erişim var mı?
docker exec ecommerce-api-prod curl -k https://host.docker.internal:8094/Api/APIMethods/HealthCheck

# Backend loglarında Mikro mesajları
docker logs ecommerce-api-prod 2>&1 | grep -i mikro

# Hangfire job'ları kayıtlı mı?
docker logs ecommerce-api-prod 2>&1 | grep -i "job kaydedildi"
```

---

## 🔧 YAPILAN DEĞİŞİKLİKLER ÖZET

### 1. `docker-compose.prod.yml`

- ✅ `extra_hosts: host.docker.internal:host-gateway` eklendi
- ✅ Mikro API environment variables eklendi
- ✅ MikroSync job ayarları eklendi
- ✅ `restart: always` eklendi

### 2. `appsettings.Production.json`

- ✅ MikroSettings ApiUrl: `https://host.docker.internal:8094`
- ✅ MikroSync bölümü eklendi (`JobsEnabled: true`)

### 3. `Program.cs`

- ✅ HTTP Client'a SSL sertifika bypass eklendi
- ✅ Self-signed sertifikalar için `DangerousAcceptAnyServerCertificateValidator`

### 4. `MikroSettings.cs`

- ✅ `IgnoreSslErrors` property eklendi

### 5. `nginx-golkoygurme.conf`

- ✅ `/hangfire` route eklendi
- ✅ `/mikro-api/` debug proxy eklendi (sadece yerel ağ)

---

## 📊 SENKRONİZASYON ZAMANLARI

| Job        | Cron           | Açıklama            |
| ---------- | -------------- | ------------------- |
| Stock Sync | `*/15 * * * *` | Her 15 dakikada bir |
| Price Sync | `0 * * * *`    | Her saat başı       |
| Full Sync  | `0 6 * * *`    | Her gün saat 06:00  |
| Order Push | `*/5 * * * *`  | Her 5 dakikada bir  |

---

## 🚨 SORUN ÇIKTIĞINDA

```bash
# Job'ları devre dışı bırak
docker exec ecommerce-api-prod sh -c 'export MikroSync__JobsEnabled=false'

# veya docker-compose.prod.yml'de:
# - MikroSync__JobsEnabled=false
# yapıp container'ı restart et

docker-compose -f docker-compose.prod.yml restart api
```

---

**Hazırlayan:** GitHub Copilot  
**Son Güncelleme:** 2026-02-06
