# Hızlı Test - Ürün Sync
$token = (Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" -Method POST -Body (@{email="admin@ecommerce.com";password="Admin123!!"} | ConvertTo-Json) -ContentType "application/json" -SkipCertificateCheck).data.token

Write-Host "Sync başlatılıyor..." -ForegroundColor Yellow
$result = Invoke-RestMethod -Uri "https://localhost:5001/api/admin/micro/sync-products" -Method POST -Headers @{Authorization="Bearer $token"} -SkipCertificateCheck -TimeoutSec 300

Write-Host ""
Write-Host "TOPLAM ÜRÜN: $($result.totalProducts)" -ForegroundColor Green
Write-Host "KAYDEDİLEN: $($result.syncedProducts)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tüm sonuç:" -ForegroundColor Gray
$result | ConvertTo-Json
