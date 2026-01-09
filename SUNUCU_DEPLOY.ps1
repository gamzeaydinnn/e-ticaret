#!/usr/bin/env pwsh
# Sunucuya Temiz Deploy - Windows PowerShell Script

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$SERVER_IP = "31.186.24.78"
$SERVER_USER = "huseyinadm"
$SERVER_PASS = "Passwd1122FFGG"
$REMOTE_PATH = "/home/huseyinadm/eticaret"

Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   🚀 SUNUCUYA TEMIZ DEPLOY - WINDOWS POWERSHELL SCRIPT   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 SUNUCU BİLGİLERİ:" -ForegroundColor Yellow
Write-Host "   IP: $SERVER_IP" -ForegroundColor Gray
Write-Host "   Kullanıcı: $SERVER_USER" -ForegroundColor Gray
Write-Host "   Proje: $REMOTE_PATH" -ForegroundColor Gray
Write-Host ""

Write-Host "🎯 AŞAMALAR:" -ForegroundColor Yellow
Write-Host "   1. Sunucuya SSH ile bağlanma" -ForegroundColor Gray
Write-Host "   2. Eski container'ları ve volume'ları silme" -ForegroundColor Gray
Write-Host "   3. Kodu GitHub'dan çekme" -ForegroundColor Gray
Write-Host "   4. .env dosyasını oluşturma" -ForegroundColor Gray
Write-Host "   5. Docker image'ları oluşturma" -ForegroundColor Gray
Write-Host "   6. Container'ları başlatma" -ForegroundColor Gray
Write-Host "   7. Veritabanı migration'ını kontrol etme" -ForegroundColor Gray
Write-Host "   8. Servis sağlığını kontrol etme" -ForegroundColor Gray
Write-Host ""

# Menu
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                     SEÇENEKLER                           ║" -ForegroundColor Cyan
Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
Write-Host "║ 1. SSH Bağlantısı Kur (başlamadan önce çalıştırın)      ║" -ForegroundColor Green
Write-Host "║ 2. Tüm Deployment Komutlarını Göster (kopyala-yapıştır)║" -ForegroundColor Green
Write-Host "║ 3. Hızlı Deploy Komutları (tek satır)                   ║" -ForegroundColor Green
Write-Host "║ 4. Docker Komutları Referansı                           ║" -ForegroundColor Green
Write-Host "║ 5. Troubleshooting Komutları                            ║" -ForegroundColor Yellow
Write-Host "║ 6. Monitörleme Komutları                                ║" -ForegroundColor Yellow
Write-Host "║ 7. Çıkış                                                ║" -ForegroundColor Red
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "Seçim yapın (1-7): " -ForegroundColor White -NoNewline
$choice = Read-Host

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║              SSH BAĞLANTISI KORU                         ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        Write-Host "Aşağıdaki komutu PowerShell'de çalıştırın:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "ssh $SERVER_USER@$SERVER_IP" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Şifre: $SERVER_PASS" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Bağlandıktan sonra:" -ForegroundColor Yellow
        Write-Host "cd /home/$SERVER_USER/eticaret" -ForegroundColor Cyan
        Write-Host ""
    }
    
    "2" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║         TÜM DEPLOYMENT KOMUTLARI (KOPYALA-YAPISTIR)      ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        
        Write-Host "⚠️  UYARI: Bu komutlar ESKİ TÜM VERİYİ SİLECEKTİR!" -ForegroundColor Red
        Write-Host "DEVAM ETSİ EMIN MİSİNİZ? (Evet/Hayır): " -ForegroundColor Yellow -NoNewline
        $confirm = Read-Host
        
        if ($confirm -eq "Evet") {
            Write-Host ""
            Write-Host "📋 Komutlar hazırlanıyor..." -ForegroundColor Yellow
            
$commands = @"
# FAZA 1: BAĞLAN
ssh huseyinadm@31.186.24.78
# Şifre girin: Passwd1122FFGG
cd /home/huseyinadm/eticaret

# FAZA 2: ESKİ DEPLOYMENT'I TEMİZLE
docker-compose -f docker-compose.prod.yml down -v
docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true
docker image prune -f
rm -rf logs/*

# FAZA 3: KOD GÜNCELLE
git pull origin main

# FAZA 4: .ENV DOSYASINI OLUŞTUR
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

# FAZA 5: BUILD VE DEPLOY
docker-compose -f docker-compose.prod.yml build --no-cache
docker-compose -f docker-compose.prod.yml up -d

# FAZA 6: MIGRATION KONTROL (Migration bitene kadar bekle)
docker-compose -f docker-compose.prod.yml logs api -f

# FAZA 7: SON KONTROLLER
docker-compose -f docker-compose.prod.yml ps
curl http://localhost:5000/api/health
curl -I http://localhost:3000
"@
            Write-Host ""
            Write-Host "📌 Aşağıdaki komutları sunucuda sırayla çalıştırın:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host $commands -ForegroundColor Cyan
            Write-Host ""
            Write-Host "✅ Komutları kopyaladığınız zaman bu penceresini kapatabilirsiniz" -ForegroundColor Green
        }
    }
    
    "3" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║          HIZLI DEPLOY (BİR SATIR)                        ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        
$oneLiner = @"
cd /home/huseyinadm/eticaret && docker-compose -f docker-compose.prod.yml down -v && docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true && docker image prune -f && rm -rf logs/* && git pull origin main && cat > .env << 'EOF'
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
 && docker-compose -f docker-compose.prod.yml build --no-cache && docker-compose -f docker-compose.prod.yml up -d
"@
        Write-Host "⏱️  Sunucuda bunu yapıştırın (uzun bir komuttur):" -ForegroundColor Yellow
        Write-Host ""
        Write-Host $oneLiner -ForegroundColor Cyan
        Write-Host ""
    }
    
    "4" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║          DOCKER KOMUTLARI REFERANSI                      ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-Host ""
        
$dockerCommands = @"
# Container Durumu Kontrol
docker-compose -f docker-compose.prod.yml ps

# Logları Canlı Takip
docker-compose -f docker-compose.prod.yml logs -f

# API Logları
docker-compose -f docker-compose.prod.yml logs -f api

# Frontend Logları
docker-compose -f docker-compose.prod.yml logs -f frontend

# Database Logları
docker-compose -f docker-compose.prod.yml logs -f sqlserver

# Servisleri Başlat
docker-compose -f docker-compose.prod.yml up -d

# Servisleri Durdur
docker-compose -f docker-compose.prod.yml down

# API'yi Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build api && docker-compose -f docker-compose.prod.yml up -d api

# Frontend'i Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build frontend && docker-compose -f docker-compose.prod.yml up -d frontend

# Tüm Servisleri Yeniden Oluştur
docker-compose -f docker-compose.prod.yml build && docker-compose -f docker-compose.prod.yml up -d

# Veritabanına Bağlan
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C

# Kaynak Kullanımı Görüntüle
docker stats

# Image Listesini Göster
docker images

# Volume Listesini Göster
docker volume ls
"@
        Write-Host $dockerCommands -ForegroundColor Cyan
        Write-Host ""
    }
    
    "5" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
        Write-Host "║       TROUBLESHOOTING KOMUTLARI                          ║" -ForegroundColor Yellow
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow
        Write-Host ""
        
$troubleshoot = @"
# Container Başlamıyor - API Loglarını Kontrol Et
docker-compose -f docker-compose.prod.yml logs api

# Container Başlamıyor - Database Loglarını Kontrol Et
docker-compose -f docker-compose.prod.yml logs sqlserver

# API Sağlık Kontrolü
curl http://localhost:5000/api/health

# API Container İçinde Health Check
docker exec ecommerce-api-prod curl -s http://localhost:5000/api/health

# Veritabanı Bağlantısı Kontrol Et
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1"

# Port Çakışması Kontrol Et
sudo netstat -tulpn | grep LISTEN
sudo lsof -i :5000
sudo lsof -i :3000

# Disk Alanı Kontrol Et
df -h
du -sh /home/huseyinadm/eticaret

# Docker Dangling Images/Volumes Temizle
docker system prune -a -f --volumes

# Tüm Veriyi Sil ve Yeni Başla (DİKKAT! VERI KAYBI!)
docker-compose -f docker-compose.prod.yml down -v && rm -rf logs/* && docker system prune -a -f && docker-compose -f docker-compose.prod.yml build --no-cache && docker-compose -f docker-compose.prod.yml up -d

# Process'i Kill Et (Kalıcı Problemler İçin)
sudo killall docker-compose
sudo systemctl restart docker
docker-compose -f docker-compose.prod.yml up -d

# Container Shell'e Gir
docker exec -it ecommerce-api-prod bash
docker exec -it ecommerce-sql-prod /bin/bash
"@
        Write-Host $troubleshoot -ForegroundColor Yellow
        Write-Host ""
    }
    
    "6" {
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║       MONITÖRLEME KOMUTLARI                              ║" -ForegroundColor Cyan
        Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        Write-Host ""
        
$monitoring = @"
# Real-time Container Durumu (Her 2 saniyede güncelle)
watch -n 2 'docker-compose -f docker-compose.prod.yml ps'

# Real-time Loglar
docker-compose -f docker-compose.prod.yml logs -f --tail=20

# CPU ve Bellek Kullanımı
docker stats

# Disk Kullanımı
du -sh /home/huseyinadm/eticaret
du -sh /var/lib/docker

# Konteyner Restartı Kontrol Et
docker-compose -f docker-compose.prod.yml ps | grep "Restarting"

# Son 100 Log Satırı
docker-compose -f docker-compose.prod.yml logs --tail=100

# Son 5 Dakikadan Beri Loglar
docker-compose -f docker-compose.prod.yml logs --since 5m

# Sistem Bilgisi
uname -a
docker --version
docker-compose --version

# Network Bilgisi
docker network ls
docker network inspect eticaret_ecommerce-network

# Volume Bilgisi
docker volume ls
docker volume inspect eticaret_sqlserver-data
"@
        Write-Host $monitoring -ForegroundColor Cyan
        Write-Host ""
    }
    
    "7" {
        Write-Host ""
        Write-Host "👋 Çıkılıyor..." -ForegroundColor Red
        exit 0
    }
    
    default {
        Write-Host ""
        Write-Host "❌ Geçersiz seçim! Lütfen 1-7 arası bir sayı girin." -ForegroundColor Red
        Write-Host ""
    }
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Gray
Write-Host "║  Daha fazla bilgi için dokümantasyona bakın:            ║" -ForegroundColor Gray
Write-Host "║  - TEMIZ_DEPLOY_KOMUTLARI.md (Detaylı)                 ║" -ForegroundColor Gray
Write-Host "║  - SUNUCU_DEPLOY_OZET.md (Özet)                        ║" -ForegroundColor Gray
Write-Host "║  - TEMIZ_DEPLOY_KOMUTLARI.sh (Bash Script)             ║" -ForegroundColor Gray
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Gray
Write-Host ""
