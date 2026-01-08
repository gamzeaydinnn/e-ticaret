# Database'i sıfırla ve yeniden oluştur
Write-Host "Database sıfırlanıyor..." -ForegroundColor Yellow

$server = "localhost,1433"
$database = "ECommerceDb"
$userId = "sa"
$password = "ECom1234"

# SQL Server'a bağlan ve database'i drop et
Write-Host "Database drop ediliyor..." -ForegroundColor Cyan
$query = "USE master; IF EXISTS(SELECT * FROM sys.databases WHERE name='$database') BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END"
sqlcmd -S $server -U $userId -P $password -Q $query

if ($LASTEXITCODE -eq 0) {
    Write-Host "Database silindi" -ForegroundColor Green
} else {
    Write-Host "Database silinirken hata oluştu (kod: $LASTEXITCODE)" -ForegroundColor Red
    Write-Host "Devam ediliyor..." -ForegroundColor Yellow
}

# Backend'i çalıştır (database otomatik oluşacak)
Write-Host ""
Write-Host "Backend başlatılıyor (database otomatik oluşacak)..." -ForegroundColor Cyan
Set-Location "c:\Users\GAMZE\Desktop\eticaret\src\ECommerce.API"
dotnet run
