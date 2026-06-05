# 🚀 E-Commerce Deployment Guide

Bu dokümanda projenizi **31.186.24.78** IP adresli sunucuya nasıl deploy edeceğinizi adım adım bulacaksınız.

## 📋 Sunucu Bilgileri

- **IP Adresi**: 31.186.24.78
- **Kullanıcı**: huseyinadm
- **Şifre**: Passwd1122FFGG
- **Proje Dizini**: /home/huseyinadm/ecommerce

## 🎯 Deployment Yöntemleri

### Yöntem 1: Git ile Deploy (ÖNERİLEN) ⭐

En güvenli ve pratik yöntem budur.

#### Adım 1: Kodu GitHub'a Push Edin

```powershell
# Local bilgisayarınızda
cd C:\Users\GAMZE\Desktop\eticaret
git add .
git commit -m "Production deployment"
git push origin main
```

#### Adım 2: Sunucuya SSH ile Bağlanın

```powershell
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
```

#### Adım 3: Sunucuda Projeyi Clone Edin

```bash
# Sunucuda çalıştırın
cd /home/huseyinadm
git clone https://github.com/gamzeaydinnn/e-ticaret.git ecommerce
cd ecommerce
```

---

### Yöntem 2: SCP ile Dosya Transferi

#### Windows'ta SCP Kullanımı

```powershell
# PowerShell'de çalıştırın
scp -r C:\Users\GAMZE\Desktop\eticaret huseyinadm@31.186.24.78:/home/huseyinadm/ecommerce
```

#### WinSCP ile Transfer (GUI)

1. WinSCP'yi açın
2. Yeni bağlantı oluşturun:
   - **Host**: 31.186.24.78
   - **Port**: 22
   - **Username**: huseyinadm
   - **Password**: Passwd1122FFGG
3. Sol panel: `C:\Users\GAMZE\Desktop\eticaret`
4. Sağ panel: `/home/huseyinadm/ecommerce`
5. Dosyaları sürükle-bırak ile transfer edin

---

### Yöntem 3: FileZilla ile SFTP Transfer

1. FileZilla'yı açın
2. Bağlantı bilgilerini girin:
   - **Host**: sftp://31.186.24.78
   - **Username**: huseyinadm
   - **Password**: Passwd1122FFGG
   - **Port**: 22
3. Bağlan'a tıklayın
4. Dosyaları transfer edin

---

## 🔧 Sunucu Kurulumu

### Adım 1: Sunucuya Bağlanın

```powershell
ssh huseyinadm@31.186.24.78
```

### Adım 2: Setup Script'ini Çalıştırın

```bash
cd /home/huseyinadm/ecommerce

# Script'i çalıştırılabilir yapın
chmod +x deploy/server-setup.sh

# Setup'ı başlatın
./deploy/server-setup.sh
```

Bu script şunları yapacak:

- ✅ Sistem güncellemesi
- ✅ Docker kurulumu
- ✅ Docker Compose kurulumu
- ✅ Git kurulumu
- ✅ Firewall yapılandırması
- ✅ Proje dizini oluşturma

### Adım 3: Environment Değişkenlerini Ayarlayın

```bash
# .env dosyasını oluşturun
cp .env.production .env

# Düzenleyin
nano .env
```

**ÖNEMLİ**: JWT_SECRET'i mutlaka güçlü bir şifre ile değiştirin!

```env
JWT_SECRET=YourVeryStrongRandomSecretKey123!@#$%^&*()
```

### Adım 4: Production Ayarlarını Güncelleyin

```bash
# API ayarlarını düzenleyin
nano src/ECommerce.API/appsettings.Production.json
```

Şunları güncelleyin:

- ✅ JWT Secret
- ✅ Email SMTP ayarları
- ✅ Ödeme gateway API key'leri (Iyzico, Stripe vb.)

### Adım 5: Docker Container'ları Başlatın

```bash
cd /home/huseyinadm/ecommerce

# Upload klasörünü release dizini dışında oluşturun
sudo mkdir -p /srv/ecommerce/uploads
sudo chown -R $USER:$USER /srv/ecommerce/uploads

# İsterseniz path'i .env içinde override edin
echo "UPLOADS_HOST_PATH=/srv/ecommerce/uploads" >> .env

# Container'ları build edin ve başlatın
docker-compose -f docker-compose.prod.yml up -d --build
```

Notlar:

- API container içinden görseller `/data/uploads` altında tutulur.
- `docker-compose.prod.yml` bunu host üzerindeki `${UPLOADS_HOST_PATH}` klasörüne bağlar.
- Bu klasör release dizini dışında olduğu için yeni deploy sırasında görseller silinmez.
- `docker-compose down -v` komutu SQL volume'lerini silebilir; hosttaki upload klasörünü silmez.

Bu işlem 10-15 dakika sürebilir. İşlemler:

- 📦 SQL Server container'ı oluşturma
- 🔨 .NET API build etme
- ⚛️ React uygulamasını build etme
- 🚀 Tüm servisleri başlatma

### Adım 6: Container Durumunu Kontrol Edin

```bash
# Container'ların durumunu görüntüleyin
docker-compose -f docker-compose.prod.yml ps

# Log'ları takip edin
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API log'larını görmek için
docker-compose -f docker-compose.prod.yml logs -f api

# Çıkmak için: Ctrl+C
```

### Adım 7: Database Migration'ları Çalıştırın

```bash
# API container'ına girin
docker-compose -f docker-compose.prod.yml exec api bash

# Migration'ları çalıştırın
dotnet ef database update

# Container'dan çıkın
exit
```

---

## 🌐 Uygulamaya Erişim

Deployment başarılı olduktan sonra:

- **Frontend**: http://31.186.24.78:3000
- **API**: http://31.186.24.78:5000
- **API Health Check**: http://31.186.24.78:5000/health

---

## 🔍 Container Yönetimi

### Container'ları Durdurma

```bash
docker-compose -f docker-compose.prod.yml stop
```

### Container'ları Başlatma

```bash
docker-compose -f docker-compose.prod.yml start
```

### Container'ları Yeniden Başlatma

```bash
docker-compose -f docker-compose.prod.yml restart
```

### Container'ları Silme

```bash
docker-compose -f docker-compose.prod.yml down
```

### Container'ları ve Volume'leri Silme

```bash
docker-compose -f docker-compose.prod.yml down -v
```

### Kod Güncellemesi Sonrası Rebuild

```bash
# Git ile güncelleme
git pull origin main

# Rebuild ve restart
docker-compose -f docker-compose.prod.yml up -d --build
```

---

## 🐛 Sorun Giderme (Troubleshooting)

### Problem: Container'lar başlamıyor

**Çözüm 1**: Log'ları kontrol edin

```bash
docker-compose -f docker-compose.prod.yml logs
```

**Çözüm 2**: Container'ları temiz başlatın

```bash
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d --build
```

### Problem: Database bağlantı hatası

**Kontrol edilecekler**:

```bash
# SQL Server container'ının çalıştığından emin olun
docker-compose -f docker-compose.prod.yml ps sqlserver

# SQL Server log'larını kontrol edin
docker-compose -f docker-compose.prod.yml logs sqlserver

# Connection string'i kontrol edin
nano src/ECommerce.API/appsettings.Production.json
```

### Problem: Frontend API'ye bağlanamıyor

**Çözüm**:

```bash
# .env dosyasını kontrol edin
cat .env

# REACT_APP_API_URL doğru mu?
REACT_APP_API_URL=http://31.186.24.78:5000

# Frontend'i rebuild edin
docker-compose -f docker-compose.prod.yml up -d --build frontend
```

### Problem: Port'lar zaten kullanımda

**Çözüm**:

```bash
# Hangi process port'u kullanıyor?
sudo lsof -i :3000
sudo lsof -i :5000
sudo lsof -i :1435

# Process'i sonlandırın
sudo kill -9 <PID>

# Veya .env'de port'ları değiştirin
nano .env
```

### Problem: Disk doldu

**Çözüm**:

```bash
# Disk kullanımını kontrol edin
df -h

# Kullanılmayan Docker image'larını temizleyin
docker system prune -a --volumes

# Log dosyalarını temizleyin
docker-compose -f docker-compose.prod.yml logs --tail=0 -f
```

---

## 🔒 Güvenlik Önerileri

### 1. Firewall Yapılandırması

```bash
# UFW firewall'ı etkinleştirin
sudo ufw enable

# Gerekli port'ları açın
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS
sudo ufw allow 3000/tcp # Frontend
sudo ufw allow 5000/tcp # API

# Durumu kontrol edin
sudo ufw status
```

### 2. SSH Anahtarı ile Giriş (Önerilir)

```bash
# Local bilgisayarınızda SSH key oluşturun
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"

# Public key'i sunucuya kopyalayın
ssh-copy-id huseyinadm@31.186.24.78
```

### 3. Şifre Güvenliği

- ✅ JWT_SECRET'i mutlaka değiştirin
- ✅ DB_PASSWORD'u güçlü yapın
- ✅ appsettings.Production.json'daki tüm secret'ları güncelleyin

### 4. HTTPS Kurulumu (Önerilir)

Domain'iniz varsa Let's Encrypt ile ücretsiz SSL:

```bash
# Nginx kurulumu
sudo apt install -y nginx certbot python3-certbot-nginx

# SSL sertifikası alın
sudo certbot --nginx -d yourdomain.com

# Otomatik yenileme
sudo systemctl enable certbot.timer
```

---

## 📊 Monitoring ve Bakım

### Log'ları İzleme

```bash
# Tüm log'lar
docker-compose -f docker-compose.prod.yml logs -f

# Son 100 satır
docker-compose -f docker-compose.prod.yml logs --tail=100

# Belirli bir servis
docker-compose -f docker-compose.prod.yml logs -f api
```

### Database Backup

```bash
# Backup klasörünü oluşturun
mkdir -p /home/huseyinadm/ecommerce/backups

# Manuel backup
docker-compose -f docker-compose.prod.yml exec sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' \
  -Q "BACKUP DATABASE [ECommerceDb] TO DISK = N'/backups/ECommerceDb.bak' WITH NOFORMAT, NOINIT, NAME = 'ECommerceDb-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
```

### Otomatik Backup Script

```bash
# Backup script oluşturun
nano /home/huseyinadm/backup.sh
```

İçeriği:

```bash
#!/bin/bash
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
docker-compose -f /home/huseyinadm/ecommerce/docker-compose.prod.yml exec sqlserver \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'ECom1234' \
  -Q "BACKUP DATABASE [ECommerceDb] TO DISK = N'/backups/ECommerceDb_${TIMESTAMP}.bak'"
```

```bash
# Çalıştırılabilir yapın
chmod +x /home/huseyinadm/backup.sh

# Crontab'a ekleyin (her gün saat 02:00'de)
crontab -e
# Ekleyin: 0 2 * * * /home/huseyinadm/backup.sh
```

### Sistem Kaynakları İzleme

```bash
# Container kaynak kullanımı
docker stats

# Disk kullanımı
df -h

# Memory kullanımı
free -h

# CPU kullanımı
top
```

---

## 📝 Hızlı Komutlar Özeti

```bash
# Deployment
ssh huseyinadm@31.186.24.78
cd /home/huseyinadm/ecommerce
docker-compose -f docker-compose.prod.yml up -d --build

# Durum Kontrolü
docker-compose -f docker-compose.prod.yml ps
docker-compose -f docker-compose.prod.yml logs -f

# Yeniden Başlatma
docker-compose -f docker-compose.prod.yml restart

# Güncelleme
git pull origin main
docker-compose -f docker-compose.prod.yml up -d --build

# Temizlik
docker-compose -f docker-compose.prod.yml down
docker system prune -a --volumes
```

---

## 🎉 Deployment Tamamlandı!

Uygulamanız artık çalışıyor olmalı:

🌐 **Frontend**: http://31.186.24.78:3000  
🔌 **API**: http://31.186.24.78:5000

Sorularınız için:

- 📧 GitHub Issues
- 💬 Project documentation

**İyi çalışmalar! 🚀**
