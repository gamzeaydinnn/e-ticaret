# =============================================================================
# test-panel-system.ps1 - Panel Sistemi Kapsamli Test Script
# =============================================================================

$ErrorActionPreference = "Continue"

# Yapilandirma
$API_BASE = "http://localhost:5000/api"

# Test kullanicilari
$TEST_USERS = @{
    Admin = @{ Email = "admin@example.com"; Password = "Admin123!" }
    StoreAttendant = @{ Email = "storeattendant@test.com"; Password = "Test123!" }
    Dispatcher = @{ Email = "dispatcher@test.com"; Password = "Test123!" }
    Courier = @{ Email = "courier@example.com"; Password = "Courier123!" }
}

function Write-Success { param($Message) Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Err { param($Message) Write-Host "[FAIL] $Message" -ForegroundColor Red }
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Warn { param($Message) Write-Host "[WARN] $Message" -ForegroundColor Yellow }
function Write-Header { param($Message) Write-Host "`n======================================" -ForegroundColor Magenta; Write-Host "  $Message" -ForegroundColor Magenta; Write-Host "======================================" -ForegroundColor Magenta }

# =============================================================================
# TEST 1: API Saglik Kontrolu
# =============================================================================
function Test-APIHealth {
    Write-Header "TEST 1: API Saglik Kontrolu"
    
    $endpoints = @("/categories", "/products", "/banners")
    
    foreach ($endpoint in $endpoints) {
        try {
            $response = Invoke-WebRequest -Uri "$API_BASE$endpoint" -Method GET -UseBasicParsing -TimeoutSec 10
            if ($response.StatusCode -eq 200) {
                Write-Success "Endpoint OK: $endpoint"
            }
        } catch {
            Write-Err "Endpoint FAILED: $endpoint"
        }
    }
}

# =============================================================================
# TEST 2: Kullanici Login Testleri
# =============================================================================
function Test-UserLogins {
    Write-Header "TEST 2: Kullanici Login Testleri"
    
    $tokens = @{}
    
    foreach ($role in $TEST_USERS.Keys) {
        $user = $TEST_USERS[$role]
        Write-Info "Login deneniyor: $role ($($user.Email))"
        
        try {
            $body = @{
                email = $user.Email
                password = $user.Password
            } | ConvertTo-Json
            
            $response = Invoke-RestMethod -Uri "$API_BASE/Auth/login" -Method POST -Body $body -ContentType "application/json" -TimeoutSec 10
            
            if ($response.token) {
                Write-Success "$role login basarili"
                $tokens[$role] = $response.token
            } else {
                Write-Warn "$role login: Token alinamadi"
            }
        } catch {
            Write-Err "$role login basarisiz: $($_.Exception.Message)"
        }
    }
    
    return $tokens
}

# =============================================================================
# TEST 3: Store Attendant Endpoint Testleri
# =============================================================================
function Test-StoreAttendantEndpoints {
    param($Token)
    
    Write-Header "TEST 3: Store Attendant Endpoint Testleri"
    
    if (-not $Token) {
        Write-Warn "Store Attendant token yok, test atlaniyor"
        return
    }
    
    $headers = @{
        "Authorization" = "Bearer $Token"
        "Content-Type" = "application/json"
    }
    
    $endpoints = @(
        @{ Method = "GET"; Path = "/StoreAttendantOrder"; Name = "Siparis Listesi" },
        @{ Method = "GET"; Path = "/StoreAttendantOrder/summary"; Name = "Ozet Istatistikler" }
    )
    
    foreach ($ep in $endpoints) {
        try {
            $response = Invoke-WebRequest -Uri "$API_BASE$($ep.Path)" -Method $ep.Method -Headers $headers -UseBasicParsing -TimeoutSec 10
            Write-Success "$($ep.Name): OK ($($response.StatusCode))"
        } catch {
            $statusCode = 0
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            if ($statusCode -eq 401) {
                Write-Warn "$($ep.Name): Unauthorized (401)"
            } elseif ($statusCode -eq 403) {
                Write-Warn "$($ep.Name): Forbidden (403)"
            } elseif ($statusCode -eq 404) {
                Write-Warn "$($ep.Name): Not Found (404)"
            } else {
                Write-Err "$($ep.Name): HATA - $statusCode"
            }
        }
    }
}

# =============================================================================
# TEST 4: Dispatcher Endpoint Testleri
# =============================================================================
function Test-DispatcherEndpoints {
    param($Token)
    
    Write-Header "TEST 4: Dispatcher Endpoint Testleri"
    
    if (-not $Token) {
        Write-Warn "Dispatcher token yok, test atlaniyor"
        return
    }
    
    $headers = @{
        "Authorization" = "Bearer $Token"
        "Content-Type" = "application/json"
    }
    
    $endpoints = @(
        @{ Method = "GET"; Path = "/DispatcherOrder"; Name = "Siparis Listesi" },
        @{ Method = "GET"; Path = "/DispatcherOrder/summary"; Name = "Ozet Istatistikler" },
        @{ Method = "GET"; Path = "/DispatcherOrder/couriers"; Name = "Kurye Listesi" }
    )
    
    foreach ($ep in $endpoints) {
        try {
            $response = Invoke-WebRequest -Uri "$API_BASE$($ep.Path)" -Method $ep.Method -Headers $headers -UseBasicParsing -TimeoutSec 10
            Write-Success "$($ep.Name): OK ($($response.StatusCode))"
        } catch {
            $statusCode = 0
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            Write-Warn "$($ep.Name): $statusCode"
        }
    }
}

# =============================================================================
# TEST 5: SignalR Hub Kontrolu
# =============================================================================
function Test-SignalRHubs {
    Write-Header "TEST 5: SignalR Hub Endpoint Kontrolu"
    
    $hubs = @("/hubs/store", "/hubs/dispatch", "/hubs/courier", "/hubs/delivery", "/hubs/admin")
    
    foreach ($hub in $hubs) {
        try {
            $negotiateUrl = "http://localhost:5000$hub/negotiate?negotiateVersion=1"
            $response = Invoke-WebRequest -Uri $negotiateUrl -Method POST -UseBasicParsing -TimeoutSec 5
            Write-Success "SignalR Hub mevcut: $hub"
        } catch {
            $statusCode = 0
            if ($_.Exception.Response) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            if ($statusCode -eq 401) {
                Write-Info "SignalR Hub: $hub (Auth gerekli - 401)"
            } else {
                Write-Warn "SignalR Hub: $hub - Kontrol edilemedi ($statusCode)"
            }
        }
    }
}

# =============================================================================
# TEST 6: Frontend Build Kontrolu
# =============================================================================
function Test-FrontendFiles {
    Write-Header "TEST 6: Frontend Build Kontrolu"
    
    $buildPath = "c:\Users\GAMZE\Desktop\eticaret\frontend\build"
    
    if (Test-Path "$buildPath\index.html") {
        Write-Success "index.html mevcut"
    } else {
        Write-Err "index.html eksik"
    }
    
    if (Test-Path "$buildPath\static\js") {
        $jsFiles = Get-ChildItem "$buildPath\static\js" -Filter "*.js"
        Write-Success "JS dosyalari: $($jsFiles.Count) adet"
    }
    
    if (Test-Path "$buildPath\static\css") {
        $cssFiles = Get-ChildItem "$buildPath\static\css" -Filter "*.css"
        Write-Success "CSS dosyalari: $($cssFiles.Count) adet"
    }
}

# =============================================================================
# TEST 7: Veritabani Rol Kontrolu
# =============================================================================
function Test-DatabaseRoles {
    Write-Header "TEST 7: Veritabani Rol Kontrolu"
    
    Write-Info "Docker uzerinden SQL sorgusu calistiriliyor..."
    
    $query = "SELECT Name FROM AspNetRoles ORDER BY Name"
    
    try {
        $result = docker exec ecommerce-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -d ECommerceDb -Q "$query" -C -h -1 2>&1
        
        $resultStr = $result -join " "
        
        if ($resultStr -match "StoreAttendant") {
            Write-Success "StoreAttendant rolu mevcut"
        } else {
            Write-Warn "StoreAttendant rolu bulunamadi"
        }
        
        if ($resultStr -match "Dispatcher") {
            Write-Success "Dispatcher rolu mevcut"
        } else {
            Write-Warn "Dispatcher rolu bulunamadi"
        }
        
        Write-Info "Mevcut roller:"
        Write-Host $result -ForegroundColor Gray
    } catch {
        Write-Warn "Veritabani sorgusu calistirilamadi"
    }
}

# =============================================================================
# ANA TEST AKISI
# =============================================================================
Write-Host ""
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "     PANEL SISTEMI KAPSAMLI TEST SCRIPTI               " -ForegroundColor Cyan
Write-Host "     Store Attendant | Dispatcher | Courier            " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# Test 1
Test-APIHealth

# Test 2
$tokens = Test-UserLogins

# Test 3
Test-StoreAttendantEndpoints -Token $tokens["StoreAttendant"]

# Test 4
Test-DispatcherEndpoints -Token $tokens["Dispatcher"]

# Test 5
Test-SignalRHubs

# Test 6
Test-FrontendFiles

# Test 7
Test-DatabaseRoles

Write-Host ""
Write-Host "========================================================" -ForegroundColor Green
Write-Host "  Test tamamlandi: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
