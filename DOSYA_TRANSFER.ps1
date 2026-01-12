# ============================================================================
# DOSYA TRANSFER KOMUTU
# Windows PowerShell'de çalıştır (Sunucuda değil!)
# 
# Kullanım: 
#   1. Bu dosyayı aç PowerShell'de
#   2. Sunucu IP'sini kontrol et: 31.186.24.78
#   3. Aşağıdaki komutları sırasıyla çalıştır
# ============================================================================

# SUNUCU BİLGİLERİ
$ServerIP = "31.186.24.78"
$User = "huseyinadm"
$ProjectPath = "c:\Users\GAMZE\Desktop\eticaret"
$RemotePath = "/root/eticaret"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DOSYA TRANSFER - SUNUCUYA YÜKLEME" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Sunucu: $ServerIP"
Write-Host "Kullanıcı: $User"
Write-Host ""

# ============================================================================
# 1. DOCKER COMPOSE DOSYASINI YÜKLE
# ============================================================================
Write-Host "[1/4] docker-compose.prod.yml yükleniyor..." -ForegroundColor Yellow
$DockerComposePath = "$ProjectPath\docker-compose.prod.yml"

if (Test-Path $DockerComposePath) {
    scp $DockerComposePath "${User}@${ServerIP}:${RemotePath}/"
    Write-Host "✅ docker-compose.prod.yml yüklendi" -ForegroundColor Green
} else {
    Write-Host "❌ docker-compose.prod.yml bulunamadı: $DockerComposePath" -ForegroundColor Red
}

# ============================================================================
# 2. FRONTEND BUILD'İNİ YÜKLE
# ============================================================================
Write-Host "`n[2/4] Frontend build/ klasörü yükleniyor..." -ForegroundColor Yellow
$FrontendBuildPath = "$ProjectPath\frontend\build"

if (Test-Path $FrontendBuildPath) {
    # Build klasörü varsa yükle
    scp -r "$FrontendBuildPath\*" "${User}@${ServerIP}:${RemotePath}/frontend/build/"
    Write-Host "✅ Frontend build yüklendi" -ForegroundColor Green
} else {
    Write-Host "⚠️  Frontend build bulunamadı: $FrontendBuildPath" -ForegroundColor Yellow
    Write-Host "Önce build etmelisiniz: npm run build" -ForegroundColor Yellow
}

# ============================================================================
# 3. BACKEND SOURCE CODE'U YÜKLE
# ============================================================================
Write-Host "`n[3/4] Backend src/ klasörü yükleniyor..." -ForegroundColor Yellow
$BackendPath = "$ProjectPath\src"

if (Test-Path $BackendPath) {
    scp -r "$BackendPath" "${User}@${ServerIP}:${RemotePath}/"
    Write-Host "✅ Backend source code yüklendi" -ForegroundColor Green
} else {
    Write-Host "❌ Backend klasörü bulunamadı: $BackendPath" -ForegroundColor Red
}

# ============================================================================
# 4. KONFIGÜRASYON DOSYASINI YÜKLE
# ============================================================================
Write-Host "`n[4/4] appsettings.json yükleniyor..." -ForegroundColor Yellow
$AppSettingsPath = "$ProjectPath\src\ECommerce.API\appsettings.json"

if (Test-Path $AppSettingsPath) {
    scp $AppSettingsPath "${User}@${ServerIP}:${RemotePath}/src/ECommerce.API/"
    Write-Host "✅ appsettings.json yüklendi" -ForegroundColor Green
} else {
    Write-Host "⚠️  appsettings.json bulunamadı: $AppSettingsPath" -ForegroundColor Yellow
}

# ============================================================================
# TAMAMLAMA
# ============================================================================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "DOSYA TRANSFERI TAMAMLANDI!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Sonraki Adım: Sunucuda deployment adımlarını izle" -ForegroundColor Yellow
Write-Host "Rehber: MANUAL_DEPLOYMENT_31.186.24.78.md" -ForegroundColor Yellow
Write-Host ""
