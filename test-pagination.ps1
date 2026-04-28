# Mikro API Sayfalama Testi
# 100'den fazla ürün gelip gelmediğini kontrol eder

Write-Host "=== MIKRO API SAYFALAMA TESTİ ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://localhost:5001"
$adminEmail = "admin@ecommerce.com"
$adminPassword = "Admin123!!"

Write-Host "1. Admin giriş yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST `
        -Body $loginBody -ContentType "application/json" `
        -SkipCertificateCheck
    
    $token = $loginResponse.data.token
    Write-Host "✓ Giriş başarılı. Token alındı." -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Giriş hatası: $_" -ForegroundColor Red
    exit 1
}

# Header'ı hazırla
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "2. Mikro API'den ürün çekiliyor..." -ForegroundColor Yellow
Write-Host "   (Bu işlem biraz zaman alabilir, sayfalama yapılıyor...)" -ForegroundColor Gray
Write-Host ""

try {
    # Sync endpoint'ini tetikle
    $syncResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/micro/sync-products" -Method POST `
        -Headers $headers -SkipCertificateCheck -TimeoutSec 300
    
    $totalProducts = $syncResponse.totalProducts
    $syncedProducts = $syncResponse.syncedProducts
    
    Write-Host "✓ Senkronizasyon tamamlandı!" -ForegroundColor Green
    Write-Host ""
    Write-Host "SONUÇLAR:" -ForegroundColor Cyan
    Write-Host "  Mikro'dan gelen ürün sayısı: $totalProducts" -ForegroundColor White
    Write-Host "  Veritabanına kaydedilen: $syncedProducts" -ForegroundColor White
    Write-Host ""
    
    if ($totalProducts -gt 100) {
        Write-Host "✓ BAŞARILI: 100'den fazla ürün geldi!" -ForegroundColor Green
        Write-Host "  Sayfalama çalışıyor! 🎉" -ForegroundColor Green
    } elseif ($totalProducts -eq 100) {
        Write-Host "⚠ UYARI: Tam 100 ürün geldi." -ForegroundColor Yellow
        Write-Host "  Bu sayfalama limitine denk gelebilir." -ForegroundColor Yellow
        Write-Host "  Daha fazla veri olup olmadığını kontrol edin." -ForegroundColor Yellow
    } else {
        Write-Host "ℹ BİLGİ: $totalProducts ürün geldi." -ForegroundColor Cyan
        Write-Host "  Mikro'da bu kadar ürün var olabilir veya sayfalama sınırlı." -ForegroundColor Cyan
    }
    
} catch {
    Write-Host "✗ Sync hatası: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Detaylı hata:" -ForegroundColor Gray
    Write-Host $_.Exception.Message -ForegroundColor Gray
    exit 1
}

Write-Host ""
Write-Host "=== TEST TAMAMLANDI ===" -ForegroundColor Cyan
