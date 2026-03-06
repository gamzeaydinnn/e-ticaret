# Mikro API Test Script
# Bu script, Mikro API baglantisinini test etmek icin kullanilir

Write-Host "=========================================="
Write-Host "  MIKRO API BAGLANTI TEST SCRIPTI"
Write-Host "=========================================="
Write-Host ""

# 1) MD5 Hash Hesapla
$today = Get-Date -Format 'yyyy-MM-dd'
$password = 'ZeMe@48.golkoy2'
$dataToHash = "$today $password"

$md5 = [System.Security.Cryptography.MD5]::Create()
$bytes = [System.Text.Encoding]::UTF8.GetBytes($dataToHash)
$hashBytes = $md5.ComputeHash($bytes)
$hash = ($hashBytes | ForEach-Object { $_.ToString('x2') }) -join ''

Write-Host "[1] MD5 HASH BILGILERI"
Write-Host "    Tarih: $today"
Write-Host "    Hash Edilecek String: '$dataToHash'"
Write-Host "    MD5 Hash: $hash"
Write-Host ""

# 2) HealthCheck - Auth gerektirmez
Write-Host "[2] HEALTHCHECK TESTI (HTTP - Auth gerektirmez)"
try {
    $resp = Invoke-WebRequest -Uri 'http://localhost:8094/Api/APIMethods/HealthCheck' -Method GET -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
    Write-Host "    BASARILI! Status: $($resp.StatusCode)"
    Write-Host "    Body: $($resp.Content)"
} catch {
    Write-Host "    BASARISIZ! Hata: $($_.Exception.Message)"
}
Write-Host ""

# 3) HealthCheck HTTPS
Write-Host "[3] HEALTHCHECK TESTI (HTTPS)"
try {
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    $resp2 = Invoke-WebRequest -Uri 'https://localhost:8094/Api/APIMethods/HealthCheck' -Method GET -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop
    Write-Host "    BASARILI! Status: $($resp2.StatusCode)"
    Write-Host "    Body: $($resp2.Content)"
} catch {
    Write-Host "    BASARISIZ! Hata: $($_.Exception.Message)"
}
Write-Host ""

# 4) StokListesiV2 testi (HTTP)
Write-Host "[4] STOKLISTESIV2 TESTI (HTTP - Auth gerekli)"
$bodyObj = @{
    Mikro = @{
        ApiKey = 'PZDEzh44zNcY2WKpOaoPHV5+mlVG1420SPXn3QBuVqcO6MvOk1j6NlSSFONwtTJV0ovN+6CjB6IKfPYN4TLXmjs6ESUxcwa2Yp+abW9+lac='
        CalismaYili = '2026'
        FirmaKodu = 'Ze-Me 2023'
        KullaniciKodu = 'Golkoy2'
        Sifre = $hash
    }
    Index = 0
    Size = '1'
    Sort = 'sto_kod'
}
$jsonBody = $bodyObj | ConvertTo-Json -Depth 10 -Compress
Write-Host "    JSON Body: $jsonBody"
Write-Host ""

try {
    $resp3 = Invoke-WebRequest -Uri 'http://localhost:8094/Api/APIMethods/StokListesiV2' -Method POST -Body $jsonBody -ContentType 'application/json; charset=utf-8' -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
    Write-Host "    BASARILI! Status: $($resp3.StatusCode)"
    $content = $resp3.Content
    if ($content.Length -gt 500) { $content = $content.Substring(0,500) + "..." }
    Write-Host "    Body: $content"
} catch {
    Write-Host "    BASARISIZ! Hata: $($_.Exception.Message)"
    $errResp = $_.Exception.Response
    if ($errResp) {
        Write-Host "    Status Code: $($errResp.StatusCode)"
        try {
            $stream = $errResp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errBody = $reader.ReadToEnd()
            if ($errBody.Length -gt 500) { $errBody = $errBody.Substring(0,500) + "..." }
            Write-Host "    Error Body: $errBody"
        } catch {}
    }
}
Write-Host ""

# 5) StokListesiV2 testi (HTTPS)
Write-Host "[5] STOKLISTESIV2 TESTI (HTTPS)"
try {
    $resp4 = Invoke-WebRequest -Uri 'https://localhost:8094/Api/APIMethods/StokListesiV2' -Method POST -Body $jsonBody -ContentType 'application/json; charset=utf-8' -TimeoutSec 30 -UseBasicParsing -ErrorAction Stop
    Write-Host "    BASARILI! Status: $($resp4.StatusCode)"
    $content4 = $resp4.Content
    if ($content4.Length -gt 500) { $content4 = $content4.Substring(0,500) + "..." }
    Write-Host "    Body: $content4"
} catch {
    Write-Host "    BASARISIZ! Hata: $($_.Exception.Message)"
    $errResp4 = $_.Exception.Response
    if ($errResp4) {
        Write-Host "    Status Code: $($errResp4.StatusCode)"
        try {
            $stream4 = $errResp4.GetResponseStream()
            $reader4 = New-Object System.IO.StreamReader($stream4)
            $errBody4 = $reader4.ReadToEnd()
            if ($errBody4.Length -gt 500) { $errBody4 = $errBody4.Substring(0,500) + "..." }
            Write-Host "    Error Body: $errBody4"
        } catch {}
    }
}
Write-Host ""

Write-Host "=========================================="
Write-Host "  TEST TAMAMLANDI"
Write-Host "=========================================="
Write-Host ""
Write-Host "SONUC YORUMLAMA:"
Write-Host "  - HealthCheck BASARILI, StokListesi 401 = Kimlik bilgileri yanlis"
Write-Host "  - Her ikisi de baglanti hatasi = API servisi calismiyordur"
Write-Host "  - HTTP basarisiz, HTTPS basarili = Protokol yanlis (https kullanin)"
Write-Host "  - Her ikisi de BASARILI = Kodda bir sorun var"
Write-Host ""
Read-Host "Devam etmek icin Enter'a basin"
