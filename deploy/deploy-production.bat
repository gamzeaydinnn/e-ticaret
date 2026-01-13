@echo off
REM ============================================================
REM E-TİCARET SUNUCU DEPLOYMENT SCRIPT (Windows)
REM SSH üzerinden uzak deployment
REM ============================================================

setlocal enabledelayedexpansion

set SERVER=31.186.24.78
set USER=root
set DEPLOY_SCRIPT=/tmp/deploy.sh

echo.
echo ============================================================
echo    E-TICARET PRODUCTION DEPLOYMENT
echo    Sunucu: %SERVER%
echo ============================================================
echo.

REM Git push kontrolü
echo [1/5] Git'e kod push ediliyor...
git push origin main
if errorlevel 1 (
    echo HATA: Git push başarısız!
    exit /b 1
)
echo OK
echo.

REM Deploy script'i sunucuya gönder
echo [2/5] Deploy script sunucuya gönderiliyor...
scp.exe -P 22 "C:\Users\GAMZE\Desktop\eticaret\deploy\deploy-production.sh" %USER%@%SERVER%:/tmp/deploy.sh
if errorlevel 1 (
    echo HATA: SCP başarısız!
    exit /b 1
)
echo OK
echo.

REM Deploy script'i çalıştır
echo [3/5] Deploy başlatılıyor (sunucuda)...
echo.
plink.exe -ssh -l %USER% -pw "Gamze@2024" %SERVER% "bash /tmp/deploy.sh"
if errorlevel 1 (
    echo HATA: Deployment başarısız!
    exit /b 1
)
echo.
echo OK
echo.

REM Test
echo [4/5] API testi yapılıyor...
plink.exe -ssh -l %USER% -pw "Gamze@2024" %SERVER% "curl -s http://localhost:5000/api/health | head -c 100"
echo.
echo OK
echo.

echo ============================================================
echo    DEPLOYMENT TAMAMLANDI!
echo ============================================================
echo.
echo Erişim Bilgileri:
echo    Frontend: http://31.186.24.78:3000
echo    Backend:  http://31.186.24.78:5000
echo    Admin:    admin@admin.com / admin123
echo.
echo Logları görmek için:
echo    Backend:  docker logs ecommerce-api-prod --tail 50
echo    Frontend: docker logs ecommerce-frontend-prod --tail 30
echo.
pause
