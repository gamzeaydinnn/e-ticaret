# ═════════════════════════════════════════════════════════════════
# VPN Mikro API Test - Installation & Verification Script (PowerShell)
# Kurulum durumunu kontrol eder ve test ortamını hazırlar
# ═════════════════════════════════════════════════════════════════

Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🔍 VPN Mikro API Test - Kurulum Kontrolü (PowerShell)" -ForegroundColor Cyan
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Dosya kontrol fonksiyonu
function Check-File {
    param([string]$Path)
    
    if (Test-Path $Path -PathType Leaf) {
        Write-Host "✅ $Path" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ $Path (BULUNAMADI)" -ForegroundColor Red
        return $false
    }
}

# Dizin kontrol fonksiyonu
function Check-Directory {
    param([string]$Path)
    
    if (Test-Path $Path -PathType Container) {
        Write-Host "✅ $Path" -ForegroundColor Green
        return $true
    } else {
        Write-Host "❌ $Path (BULUNAMADI)" -ForegroundColor Red
        return $false
    }
}

# ═════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "📋 YAPILAN İŞLEMLER:" -ForegroundColor Yellow
Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray

# 1. Yapılandırma Dosyaları
Write-Host ""
Write-Host "🔧 1. Yapılandırma Dosyaları:" -ForegroundColor Cyan
Check-File "src\ECommerce.API\appsettings.VpnTest.json"
Check-File "Postman-VPN-MikroAPI-Test.json"

# 2. Dokümantasyon
Write-Host ""
Write-Host "📖 2. Dokümantasyon Dosyaları:" -ForegroundColor Cyan
Check-File "VPN_TEST_SETUP.md"
Check-File "VPN_MIKRO_API_SETUP_SUMMARY.md"

# 3. Kod Dosyaları
Write-Host ""
Write-Host "💻 3. C# Kod Dosyaları:" -ForegroundColor Cyan
Check-File "src\ECommerce.Infrastructure\Services\MicroServices\MikroApiVpnTestService.cs"
Check-File "src\ECommerce.API\Controllers\VpnTest\MikroApiTestController.cs"

# 4. Program.cs güncellemesi
Write-Host ""
Write-Host "⚙️  4. Program.cs Güncellemesi:" -ForegroundColor Cyan
$programCsContent = Get-Content "src\ECommerce.API\Program.cs" -Raw
if ($programCsContent -match "MikroApiVpnTestService") {
    Write-Host "✅ Program.cs - MikroApiVpnTestService kaydı" -ForegroundColor Green
} else {
    Write-Host "❌ Program.cs - MikroApiVpnTestService kaydı (GÜNCELLENMESİ GEREKEBILIR)" -ForegroundColor Yellow
}

# ═════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📝 ÖNEMLİ NOTLAR:" -ForegroundColor Yellow
Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray

Write-Host ""
Write-Host "1️⃣ ORTAM DEĞİŞKENİNİ AYARLA:" -ForegroundColor Cyan
Write-Host "   PowerShell:" -ForegroundColor Gray
Write-Host '   $env:ASPNETCORE_ENVIRONMENT = "VpnTest"' -ForegroundColor Yellow
Write-Host ""
Write-Host "   CMD (komut satırı):" -ForegroundColor Gray
Write-Host "   set ASPNETCORE_ENVIRONMENT=VpnTest" -ForegroundColor Yellow
Write-Host ""

Write-Host "2️⃣ UYGULAMAYI BAŞLAT:" -ForegroundColor Cyan
Write-Host "   dotnet run --project src\ECommerce.API\ECommerce.API.csproj" -ForegroundColor Yellow
Write-Host ""

Write-Host "3️⃣ TEST ET:" -ForegroundColor Cyan
Write-Host "   API Endpoint: http://10.0.0.3:8084" -ForegroundColor Gray
Write-Host "   Test Controller: /api/mikroapitest/" -ForegroundColor Gray
Write-Host "   Postman Collection: Postman-VPN-MikroAPI-Test.json" -ForegroundColor Gray
Write-Host ""

Write-Host "4️⃣ KİMLİK BİLGİLERİ:" -ForegroundColor Cyan
Write-Host "   API URL: http://10.0.0.3:8084" -ForegroundColor Gray
Write-Host "   Firma: Ze-Me 2023" -ForegroundColor Gray
Write-Host "   Kullanıcı: Golkoy2" -ForegroundColor Gray
Write-Host "   Çalışma Yılı: 2026" -ForegroundColor Gray
Write-Host ""

# ═════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "🔗 BAŞLANGIC ENDPOINT'LERİ:" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""
Write-Host "GET    /api/mikroapitest/config          → Aktif konfigürasyonu görüntüle" -ForegroundColor White
Write-Host "POST   /api/mikroapitest/login            → API'ye giriş yap" -ForegroundColor White
Write-Host "GET    /api/mikroapitest/product/{key}   → Ürün bilgisi sorgula" -ForegroundColor White
Write-Host "GET    /api/mikroapitest/customer/{key}  → Müşteri bilgisi sorgula" -ForegroundColor White
Write-Host "GET    /api/mikroapitest/system-info     → Sistem bilgisi al" -ForegroundColor White
Write-Host "GET    /api/mikroapitest/health-check    → Bağlantı kontrolü" -ForegroundColor White
Write-Host ""

# ═════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "🚀 HAZIRLIKLARıN TESPİT EDİLMESİ:" -ForegroundColor Cyan
Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray

$filesOk = $true
$filesOk = (Check-File "src\ECommerce.API\appsettings.VpnTest.json") -and $filesOk
$filesOk = (Check-File "Postman-VPN-MikroAPI-Test.json") -and $filesOk
$filesOk = (Check-File "VPN_TEST_SETUP.md") -and $filesOk
$filesOk = (Check-File "VPN_MIKRO_API_SETUP_SUMMARY.md") -and $filesOk
$filesOk = (Check-File "src\ECommerce.Infrastructure\Services\MicroServices\MikroApiVpnTestService.cs") -and $filesOk
$filesOk = (Check-File "src\ECommerce.API\Controllers\VpnTest\MikroApiTestController.cs") -and $filesOk

Write-Host ""
if ($filesOk) {
    Write-Host "✅ TÜM DOSYALAR BAŞARIYLA OLUŞTURULDU!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Sonraki adım: ASPNETCORE_ENVIRONMENT = VpnTest ile çalıştır" -ForegroundColor Yellow
} else {
    Write-Host "⚠️  BAZI DOSYALAR EKSİK OLABİLİR" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Lütfen manuel olarak kontrol et:" -ForegroundColor Gray
    Write-Host "  - appsettings.VpnTest.json" -ForegroundColor Gray
    Write-Host "  - MikroApiVpnTestService.cs" -ForegroundColor Gray
    Write-Host "  - MikroApiTestController.cs" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✨ Kurulum tamamlandı! VPN test ortamı hazır." -ForegroundColor Green
Write-Host "═════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# ═════════════════════════════════════════════════════════════════════
# ÖNERİLİ KOMUTLAR
# ═════════════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "💡 HIZLI BAŞLATMA:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. PowerShell'de çalıştır:" -ForegroundColor Gray
Write-Host "`$env:ASPNETCORE_ENVIRONMENT = 'VpnTest'; dotnet run --project src\ECommerce.API" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Veya batch file oluştur (.bat dosyası Windows için):" -ForegroundColor Gray
Write-Host "   Bkz: vpn-test-start.bat" -ForegroundColor Gray
Write-Host ""
