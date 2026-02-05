# ============================================================================
# MİKRO API DEPLOY - Windows PowerShell Script
# Yerel bilgisayardan sunucuya dosya transfer ve deploy
# 
# Kullanım: 
#   .\deploy-mikro-to-server.ps1
# ============================================================================

$ErrorActionPreference = "Stop"

# Sunucu bilgileri
$ServerIP = "31.186.24.78"
$ServerUser = "huseyinadm"
$RemotePath = "/root/eticaret"
$LocalPath = $PSScriptRoot -replace "\\deploy$", ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "🔄 MİKRO API SUNUCU DEPLOY" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Yerel Klasör: $LocalPath"
Write-Host "Sunucu: $ServerUser@$ServerIP"
Write-Host ""

# 1. Dosyaları transfer et
Write-Host "📍 Adım 1/4: Dosyalar sunucuya kopyalanıyor..." -ForegroundColor Yellow

# docker-compose.prod.yml
Write-Host "  → docker-compose.prod.yml"
scp "$LocalPath\docker-compose.prod.yml" "${ServerUser}@${ServerIP}:${RemotePath}/"

# appsettings.Production.json
Write-Host "  → appsettings.Production.json"
scp "$LocalPath\src\ECommerce.API\appsettings.Production.json" "${ServerUser}@${ServerIP}:${RemotePath}/src/ECommerce.API/"

# nginx config
Write-Host "  → nginx-golkoygurme.conf"
scp "$LocalPath\deploy\nginx-golkoygurme.conf" "${ServerUser}@${ServerIP}:/tmp/"

# deploy script
Write-Host "  → deploy-mikro-api.sh"
scp "$LocalPath\deploy\deploy-mikro-api.sh" "${ServerUser}@${ServerIP}:${RemotePath}/"

Write-Host "✅ Dosya transferi tamamlandı" -ForegroundColor Green
Write-Host ""

# 2. Nginx konfigürasyonunu güncelle
Write-Host "📍 Adım 2/4: Nginx konfigürasyonu güncelleniyor..." -ForegroundColor Yellow
ssh "${ServerUser}@${ServerIP}" @"
sudo cp /tmp/nginx-golkoygurme.conf /etc/nginx/sites-available/golkoygurme
sudo nginx -t && sudo systemctl reload nginx
echo 'Nginx güncellendi'
"@
Write-Host "✅ Nginx güncellendi" -ForegroundColor Green
Write-Host ""

# 3. Deploy scriptini çalıştır
Write-Host "📍 Adım 3/4: Deploy script çalıştırılıyor..." -ForegroundColor Yellow
Write-Host "(Bu işlem birkaç dakika sürebilir)" -ForegroundColor Gray
Write-Host ""

ssh "${ServerUser}@${ServerIP}" @"
cd $RemotePath
chmod +x deploy-mikro-api.sh
./deploy-mikro-api.sh
"@

Write-Host ""
Write-Host "✅ Deploy tamamlandı" -ForegroundColor Green
Write-Host ""

# 4. Sonuç
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "📊 DEPLOY SONUCU" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Kontrol adresleri:"
Write-Host "  - Web Sitesi: https://golkoygurme.com.tr"
Write-Host "  - API Health: https://golkoygurme.com.tr/api/health"
Write-Host "  - Hangfire: https://golkoygurme.com.tr/hangfire"
Write-Host ""
Write-Host "Log kontrol komutu:"
Write-Host "  ssh $ServerUser@$ServerIP 'docker logs ecommerce-api-prod 2>&1 | grep -i mikro'"
Write-Host ""
