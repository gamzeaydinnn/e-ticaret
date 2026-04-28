Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MIKRO API - SqlVeriOkuV2 Test Runner" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Proje dizinine git
Set-Location "c:\Users\GAMZE\Desktop\eticaret\MikroApiTest"

# Program.cs'i yedekle ve TestSqlVeriOku.cs'i kullan
Write-Host "[1] Test dosyasını hazırlıyorum..." -ForegroundColor Yellow
$originalProgram = Get-Content -Path "Program.cs" -Raw
Copy-Item -Path "Program.cs" -Destination "Program.cs.bak" -Force

# TestSqlVeriOku.cs içeriğini Program.cs olarak kopyala
Copy-Item -Path "TestSqlVeriOku.cs" -Destination "Program.cs" -Force

Write-Host "[2] Proje derleniyor..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "[3] Test çalıştırılıyor..." -ForegroundColor Green
    Write-Host ""
    dotnet run --configuration Release
} else {
    Write-Host "HATA: Derleme başarısız!" -ForegroundColor Red
}

# Program.cs'i geri yükle
Write-Host ""
Write-Host "[4] Program.cs geri yükleniyor..." -ForegroundColor Yellow
Copy-Item -Path "Program.cs.bak" -Destination "Program.cs" -Force
Remove-Item -Path "Program.cs.bak" -Force

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  TEST TAMAMLANDI" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
