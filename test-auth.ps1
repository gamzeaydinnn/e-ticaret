# Test authentication
$body = @{
    email='admin@admin.com'
    password='admin123'
} | ConvertTo-Json

Write-Host "1. Login test..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri 'http://localhost:5153/api/auth/login' -Method Post -Body $body -ContentType 'application/json'
Write-Host "✅ Login SUCCESS" -ForegroundColor Green
Write-Host "User: $($login.User.Email), Role: $($login.User.Role)" -ForegroundColor Yellow

$token = $login.Token
Write-Host "`n2. Token (first 100 chars):" -ForegroundColor Cyan
Write-Host $token.Substring(0, [Math]::Min(100, $token.Length)) -ForegroundColor Gray

Write-Host "`n3. Testing /api/auth/permissions..." -ForegroundColor Cyan
try {
    $headers = @{
        Authorization = "Bearer $token"
    }
    $perms = Invoke-RestMethod -Uri 'http://localhost:5153/api/auth/permissions' -Headers $headers
    Write-Host "✅ Permissions SUCCESS" -ForegroundColor Green
    Write-Host "Permission Count: $($perms.data.permissionCount)" -ForegroundColor Yellow
} catch {
    Write-Host "❌ Permissions FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}
