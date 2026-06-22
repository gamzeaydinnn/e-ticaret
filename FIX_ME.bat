@echo off
echo ========================================================
echo ECommerce Program.cs Dosyasini Onarma Araci
echo ========================================================
echo.
echo Bozulan Program.cs dosyasi orjinal haline donduruluyor...
git checkout src/ECommerce.API/Program.cs
if %ERRORLEVEL% GEQ 1 (
    echo.
    echo HATA: git komutu basarisiz oldu. Lutfen git'in kurulu oldugundan emin olun.
) else (
    echo.
    echo BASARILI: Program.cs basariyla onarildi!
)
echo.
pause
