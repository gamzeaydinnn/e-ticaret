@echo off
echo ===================================
echo PuTTY Indirme ve Kurulum
echo ===================================
echo.
echo PuTTY'yi manuel olarak indirmek icin:
echo https://www.putty.org/
echo.
echo Veya otomatik indirme:
echo.

set PUTTY_URL=https://the.earth.li/~sgtatham/putty/latest/w64/putty.exe
set DOWNLOAD_PATH=%USERPROFILE%\Desktop\putty.exe

echo Indiriliyor: %PUTTY_URL%
powershell -Command "Invoke-WebRequest -Uri '%PUTTY_URL%' -OutFile '%DOWNLOAD_PATH%'"

if exist "%DOWNLOAD_PATH%" (
    echo.
    echo Basarili! PuTTY indirildi: %DOWNLOAD_PATH%
    echo.
    echo Baglanti Bilgileri:
    echo - Host: 31.186.24.78
    echo - Port: 22
    echo - Username: huseyinadm
    echo - Password: Passwd1122FFGG
    echo.
    echo PuTTY'yi calistirmak icin Enter'a basin...
    pause
    start "" "%DOWNLOAD_PATH%"
) else (
    echo Indirme basarisiz. Manuel indirin: https://www.putty.org/
)

pause
