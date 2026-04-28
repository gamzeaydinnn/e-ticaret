@echo off
REM ═════════════════════════════════════════════════════════════════
REM VPN Mikro API Test - Hızlı Başlat Script (Windows Batch)
REM ═════════════════════════════════════════════════════════════════

setlocal enabledelayedexpansion

echo.
echo ╔═════════════════════════════════════════════════════════════════╗
echo ║  🚀 VPN Mikro API Test - Başlangıç                             ║
echo ║     API Endpoint: http://10.0.0.3:8084                          ║
echo ╚═════════════════════════════════════════════════════════════════╝
echo.

REM Ortam değişkenini ayarla
set ASPNETCORE_ENVIRONMENT=VpnTest

echo.
echo ✅ Ortam: %ASPNETCORE_ENVIRONMENT%
echo ✅ Port: 5153 (HTTP)
echo ✅ Mikro API: http://10.0.0.3:8084
echo.

REM 5153 portu doluysa portu kullanan sureci kapat
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5153 ^| findstr LISTENING') do (
    echo ⚠️  5153 portunu kullanan surec kapatiliyor: PID %%a
    taskkill /PID %%a /F > nul 2>&1
)

REM VPN Bağlantı kontrolü
echo 📡 VPN bağlantısı kontrol ediliyor...
ping -n 1 10.0.0.3 > nul 2>&1
if %errorlevel% equ 0 (
    echo ✅ VPN'ye bağlı - 10.0.0.3 erişilebilir
) else (
    echo ⚠️  VPN'ye bağlanılamıyor! Lütfen VPN'ye bağlan.
    echo.
    timeout /t 5 /nobreak
)

echo.
echo 🔄 Uygulama başlatılıyor...
echo.

REM Dotnet run komutu
cd /d "%~dp0"
dotnet run --project src\ECommerce.API\ECommerce.API.csproj --launch-profile VpnTest

REM Hata kontrolü
if %errorlevel% neq 0 (
    echo.
    echo ❌ Hata oluştu! Lütfen kontrol et.
    echo.
    pause
)

endlocal
