# SSL sertifika kontrolünü devre dışı bırak (eski PowerShell için)
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host "Login yapılıyor..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@ecommerce.com"
    password = "Admin123!!"
} | ConvertTo-Json

$token = (Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json").data.token

Write-Host "Token alındı, sync başlatılıyor..." -ForegroundColor Yellow
Write-Host ""

$headers = @{
    Authorization = "Bearer $token"
}

$result = Invoke-RestMethod -Uri "https://localhost:5001/api/admin/micro/sync-products" -Method POST -Headers $headers -TimeoutSec 300

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "TOPLAM ÜRÜN: $($result.totalProducts)" -ForegroundColor Green
Write-Host "KAYDEDİLEN: $($result.syncedProducts)" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

if ($result.totalProducts -gt 100) {
    Write-Host "✓ SAYFALAMA ÇALIŞIYOR! 100'den fazla ürün geldi!" -ForegroundColor Green
} else {
    Write-Host "Toplam $($result.totalProducts) ürün var." -ForegroundColor Yellow
}
