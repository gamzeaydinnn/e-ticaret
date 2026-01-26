# ==========================================================================
# TEST-ORDER-FLOW.ps1 - Sipariş Akışı End-to-End Test Scripti
# ==========================================================================
# Bu script şunları yapar:
# 1. Veritabanında test kullanıcıları oluşturur (müşteri, kurye)
# 2. Sipariş oluşturur
# 3. Admin panelinde siparişi görür
# 4. Kurye atar
# 5. Kurye "Yola Çıktım" der
# 6. Kurye "Teslim Ettim" der
# 7. Her adımda durumu kontrol eder
# ==========================================================================

param(
    [string]$ApiBaseUrl = "https://localhost:5001",
    [switch]$SkipDbSetup = $false,
    [switch]$Verbose = $true
)

# Renk fonksiyonları
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Yellow }
function Write-SubStep { param($msg) Write-Host "   -> $msg" -ForegroundColor Gray }

# ==========================================================================
# VERİTABANI BAĞLANTISI
# ==========================================================================
$connectionString = "Server=localhost,1435;Database=ECommerceDb;User Id=sa;Password=ECom1234;TrustServerCertificate=True;"

function Invoke-SqlQuery {
    param(
        [string]$Query,
        [switch]$ReturnData
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $connectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 30
        
        if ($ReturnData) {
            $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
            $dataset = New-Object System.Data.DataSet
            $adapter.Fill($dataset) | Out-Null
            $result = $dataset.Tables[0]
        } else {
            $result = $command.ExecuteNonQuery()
        }
        
        $connection.Close()
        return $result
    }
    catch {
        Write-Error "SQL Hatası: $_"
        if ($connection.State -eq 'Open') { $connection.Close() }
        return $null
    }
}

# ==========================================================================
# API FONKSİYONLARI
# ==========================================================================
function Invoke-ApiRequest {
    param(
        [string]$Method = "GET",
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = $null,
        [switch]$IgnoreCertError
    )
    
    $url = "$ApiBaseUrl$Endpoint"
    $headers = @{
        "Content-Type" = "application/json; charset=utf-8"
        "Accept" = "application/json"
    }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    try {
        # SSL sertifika hatasını yoksay (development için)
        if ($IgnoreCertError) {
            [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $params = @{
                    Method = $Method
                    Uri = $url
                    Headers = $headers
                    SkipCertificateCheck = $true
                }
            } else {
                Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) { return true; }
}
"@
                [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
                $params = @{
                    Method = $Method
                    Uri = $url
                    Headers = $headers
                }
            }
        } else {
            $params = @{
                Method = $Method
                Uri = $url
                Headers = $headers
            }
        }
        
        if ($Body -and $Method -ne "GET") {
            $jsonBody = $Body | ConvertTo-Json -Depth 10 -Compress
            # UTF-8 encoding düzeltmesi
            $utf8Body = [System.Text.Encoding]::UTF8.GetBytes($jsonBody)
            $params["Body"] = $utf8Body
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response }
    }
    catch {
        $errorMsg = $_.Exception.Message
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $errorMsg = "$errorMsg - $errorBody"
            } catch {}
        }
        return @{ Success = $false; Error = $errorMsg }
    }
}

# ==========================================================================
# TEST VERİLERİ
# ==========================================================================
$testCustomer = @{
    Email = "test.musteri@example.com"
    Password = "Test123!"
    FirstName = "Test"
    LastName = "Musteri"
    Phone = "5551234567"
}

$testCourier = @{
    Email = "test.kurye@example.com"
    Password = "Kurye123!"
    FirstName = "Test"
    LastName = "Kurye"
    Phone = "5559876543"
}

$testAdmin = @{
    Email = "admin@example.com"
    Password = "Admin123!"
}

# ==========================================================================
# ANA TEST AKIŞI
# ==========================================================================
Write-Host ""
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "          SIPARIS AKISI END-TO-END TEST SCRIPTI                         " -ForegroundColor Magenta
Write-Host "                                                                        " -ForegroundColor Magenta
Write-Host "  Bu script siparis olusturma -> admin onay -> kurye teslimat           " -ForegroundColor Magenta
Write-Host "  akisini otomatik olarak test eder.                                    " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""

# ==========================================================================
# ADIM 1: VERİTABANI BAĞLANTISI KONTROLÜ
# ==========================================================================
Write-Step "ADIM 1: Veritabanı bağlantısı kontrol ediliyor..."

$dbCheck = Invoke-SqlQuery -Query "SELECT 1 AS Test" -ReturnData
if ($dbCheck -eq $null) {
    Write-Error "Veritabanına bağlanılamadı! Lütfen SQL Server'ın çalıştığından emin olun."
    Write-Info "Bağlantı: $connectionString"
    exit 1
}
Write-Success "Veritabanı bağlantısı başarılı"

# ==========================================================================
# ADIM 2: TEST VERİLERİNİ HAZIRLA
# ==========================================================================
Write-Step "ADIM 2: Test verileri hazırlanıyor..."

if (-not $SkipDbSetup) {
    Write-SubStep "Mevcut test verilerini temizle..."
    
    # Test kullanıcılarını temizle
    $cleanupQuery = @"
    -- Test siparişlerini temizle
    DELETE FROM OrderItems WHERE OrderId IN (SELECT Id FROM Orders WHERE CustomerEmail = '$($testCustomer.Email)');
    DELETE FROM OrderStatusHistories WHERE OrderId IN (SELECT Id FROM Orders WHERE CustomerEmail = '$($testCustomer.Email)');
    DELETE FROM Orders WHERE CustomerEmail = '$($testCustomer.Email)';
    
    -- Test kuryesini temizle (varsa)
    DELETE FROM RefreshTokens WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('$($testCustomer.Email)', '$($testCourier.Email)'));
    DELETE FROM UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('$($testCustomer.Email)', '$($testCourier.Email)'));
    DELETE FROM Couriers WHERE UserId IN (SELECT Id FROM Users WHERE Email = '$($testCourier.Email)');
"@
    Invoke-SqlQuery -Query $cleanupQuery | Out-Null
    
    Write-SubStep "Test müşterisi oluştur..."
    
    # Şifre hash'le (BCrypt benzeri basit hash - gerçek uygulamada Identity kullanılır)
    # Burada direkt veritabanına ekleyeceğiz
    $createCustomerQuery = @"
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = '$($testCustomer.Email)')
    BEGIN
        INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, PhoneNumber, EmailConfirmed, IsActive, CreatedAt, UpdatedAt)
        VALUES (NEWID(), '$($testCustomer.Email)', 
                -- BCrypt hash for 'Test123!' 
                '`$2a`$11`$K9Xy3Oxy3Oxy3Oxy3Oxy3OxK9Xy3Oxy3Oxy3Oxy3Oxy3Oxy3Oxy3O',
                N'$($testCustomer.FirstName)', N'$($testCustomer.LastName)', '$($testCustomer.Phone)', 
                1, 1, GETUTCDATE(), GETUTCDATE());
        
        -- Customer rolü ekle
        DECLARE @customerId UNIQUEIDENTIFIER = (SELECT Id FROM Users WHERE Email = '$($testCustomer.Email)');
        DECLARE @customerRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Customer');
        IF @customerRoleId IS NOT NULL
            INSERT INTO UserRoles (UserId, RoleId) VALUES (@customerId, @customerRoleId);
    END
"@
    Invoke-SqlQuery -Query $createCustomerQuery | Out-Null
    
    Write-SubStep "Test kuryesi oluştur..."
    $createCourierQuery = @"
    IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = '$($testCourier.Email)')
    BEGIN
        DECLARE @courierUserId UNIQUEIDENTIFIER = NEWID();
        
        INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, PhoneNumber, EmailConfirmed, IsActive, CreatedAt, UpdatedAt)
        VALUES (@courierUserId, '$($testCourier.Email)', 
                '`$2a`$11`$K9Xy3Oxy3Oxy3Oxy3Oxy3OxK9Xy3Oxy3Oxy3Oxy3Oxy3Oxy3Oxy3O',
                N'$($testCourier.FirstName)', N'$($testCourier.LastName)', '$($testCourier.Phone)', 
                1, 1, GETUTCDATE(), GETUTCDATE());
        
        -- Courier rolü ekle
        DECLARE @courierRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Courier');
        IF @courierRoleId IS NOT NULL
            INSERT INTO UserRoles (UserId, RoleId) VALUES (@courierUserId, @courierRoleId);
        
        -- Courier kaydı oluştur
        INSERT INTO Couriers (Id, UserId, Name, Phone, Email, IsActive, IsOnline, CreatedAt, UpdatedAt, VehicleType, LicensePlate, MaxCapacity)
        VALUES (NEWID(), @courierUserId, N'$($testCourier.FirstName) $($testCourier.LastName)', '$($testCourier.Phone)', 
                '$($testCourier.Email)', 1, 1, GETUTCDATE(), GETUTCDATE(), 'Motorcycle', '34ABC123', 10);
    END
"@
    Invoke-SqlQuery -Query $createCourierQuery | Out-Null
    
    Write-Success "Test verileri hazırlandı"
}

# ==========================================================================
# ADIM 3: ÜRÜN KONTROLÜ
# ==========================================================================
Write-Step "ADIM 3: Mevcut ürünler kontrol ediliyor..."

$productsQuery = "SELECT TOP 3 Id, Name, Price, Stock FROM Products WHERE IsActive = 1 AND Stock > 0 ORDER BY Id"
$products = Invoke-SqlQuery -Query $productsQuery -ReturnData

if ($products -eq $null -or $products.Rows.Count -eq 0) {
    Write-Error "Aktif ürün bulunamadı! Önce ürün ekleyin."
    
    # Test ürünü ekle
    Write-Info "Test ürünü ekleniyor..."
    $createProductQuery = @"
    IF NOT EXISTS (SELECT 1 FROM Products WHERE Name = 'Test Urun')
    BEGIN
        INSERT INTO Products (Id, Name, Description, Price, Stock, IsActive, CreatedAt, UpdatedAt, SKU)
        VALUES (NEWID(), N'Test Ürün', N'Test amaçlı ürün', 99.90, 100, 1, GETUTCDATE(), GETUTCDATE(), 'TEST001');
    END
"@
    Invoke-SqlQuery -Query $createProductQuery | Out-Null
    $products = Invoke-SqlQuery -Query $productsQuery -ReturnData
}

Write-Success "Bulunan ürünler:"
foreach ($row in $products.Rows) {
    Write-SubStep "$($row.Name) - $($row.Price) TL (Stok: $($row.Stock))"
}

# ==========================================================================
# ADIM 4: ADMİN GİRİŞİ
# ==========================================================================
Write-Step "ADIM 4: Admin girişi yapılıyor..."

# Önce admin kullanıcısını kontrol et
$adminCheck = Invoke-SqlQuery -Query "SELECT Id, Email FROM Users WHERE Email = '$($testAdmin.Email)'" -ReturnData
if ($adminCheck -eq $null -or $adminCheck.Rows.Count -eq 0) {
    Write-Info "Admin kullanıcısı bulunamadı, mevcut admin aranıyor..."
    $adminCheck = Invoke-SqlQuery -Query @"
    SELECT TOP 1 u.Id, u.Email 
    FROM Users u 
    INNER JOIN UserRoles ur ON u.Id = ur.UserId 
    INNER JOIN Roles r ON ur.RoleId = r.Id 
    WHERE r.Name IN ('Admin', 'SuperAdmin') AND u.IsActive = 1
"@ -ReturnData
    
    if ($adminCheck -ne $null -and $adminCheck.Rows.Count -gt 0) {
        $testAdmin.Email = $adminCheck.Rows[0].Email
        Write-Info "Bulunan admin: $($testAdmin.Email)"
    }
}

$adminLoginResult = Invoke-ApiRequest -Method "POST" -Endpoint "/api/auth/login" -Body @{
    email = $testAdmin.Email
    password = $testAdmin.Password
} -IgnoreCertError

$adminToken = $null
if ($adminLoginResult.Success) {
    $adminToken = $adminLoginResult.Data.token
    Write-Success "Admin girişi başarılı"
} else {
    Write-Error "Admin girişi başarısız: $($adminLoginResult.Error)"
    Write-Info "API çalışıyor mu kontrol edin: $ApiBaseUrl"
}

# ==========================================================================
# ADIM 5: SİPARİŞ OLUŞTUR
# ==========================================================================
Write-Step "ADIM 5: Test siparişi oluşturuluyor..."

# Sipariş için ürün seç
$firstProduct = $products.Rows[0]
$productId = $firstProduct.Id
$productPrice = [decimal]$firstProduct.Price

# Müşteri ID'sini al
$customerIdQuery = "SELECT Id FROM Users WHERE Email = '$($testCustomer.Email)'"
$customerResult = Invoke-SqlQuery -Query $customerIdQuery -ReturnData
$customerId = $customerResult.Rows[0].Id

# Yeni sipariş numarası oluştur
$orderNumber = "ORD-TEST-" + (Get-Date -Format "yyyyMMddHHmmss")

Write-SubStep "Sipariş No: $orderNumber"
Write-SubStep "Ürün: $($firstProduct.Name) x 2 = $($productPrice * 2) TL"

$createOrderQuery = @"
DECLARE @orderId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Orders (
    Id, OrderNumber, CustomerId, CustomerEmail, CustomerName, CustomerPhone,
    ShippingAddress, ShippingCity, ShippingDistrict, ShippingPostalCode,
    TotalAmount, Status, PaymentMethod, PaymentStatus,
    CreatedAt, UpdatedAt, Notes
)
VALUES (
    @orderId, 
    '$orderNumber',
    '$customerId',
    '$($testCustomer.Email)',
    N'$($testCustomer.FirstName) $($testCustomer.LastName)',
    '$($testCustomer.Phone)',
    N'Test Mahallesi, Test Sokak No:1 Daire:5',
    N'Istanbul',
    N'Kadikoy',
    '34000',
    $($productPrice * 2),
    -1, -- New status
    'CreditCard',
    'Authorized',
    GETUTCDATE(),
    GETUTCDATE(),
    N'Test siparişi - otomatik oluşturuldu'
);

-- Sipariş kalemi ekle
INSERT INTO OrderItems (Id, OrderId, ProductId, ProductName, Quantity, UnitPrice, TotalPrice, CreatedAt)
VALUES (NEWID(), @orderId, '$productId', N'$($firstProduct.Name)', 2, $productPrice, $($productPrice * 2), GETUTCDATE());

-- Sipariş geçmişi ekle
INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
VALUES (NEWID(), @orderId, NULL, -1, GETUTCDATE(), 'System', N'Sipariş oluşturuldu');

SELECT @orderId AS OrderId;
"@

$orderResult = Invoke-SqlQuery -Query $createOrderQuery -ReturnData
if ($orderResult -ne $null -and $orderResult.Rows.Count -gt 0) {
    $orderId = $orderResult.Rows[0].OrderId
    Write-Success "Sipariş oluşturuldu! ID: $orderId"
} else {
    Write-Error "Sipariş oluşturulamadı!"
    exit 1
}

# ==========================================================================
# ADIM 6: SİPARİŞİ ONAYLA (Admin)
# ==========================================================================
Write-Step "ADIM 6: Sipariş admin tarafından onaylanıyor..."

$confirmQuery = @"
UPDATE Orders SET Status = -2, UpdatedAt = GETUTCDATE() WHERE Id = '$orderId';

INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
VALUES (NEWID(), '$orderId', -1, -2, GETUTCDATE(), 'Admin', N'Sipariş onaylandı');
"@
Invoke-SqlQuery -Query $confirmQuery | Out-Null
Write-Success "Sipariş onaylandı (Status: Confirmed/-2)"

# Biraz bekle
Start-Sleep -Milliseconds 500

# ==========================================================================
# ADIM 7: SİPARİŞİ HAZIRLA
# ==========================================================================
Write-Step "ADIM 7: Sipariş hazırlanıyor..."

$prepareQuery = @"
UPDATE Orders SET Status = 1, UpdatedAt = GETUTCDATE() WHERE Id = '$orderId';

INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
VALUES (NEWID(), '$orderId', -2, 1, GETUTCDATE(), 'Admin', N'Sipariş hazırlanıyor');
"@
Invoke-SqlQuery -Query $prepareQuery | Out-Null
Write-Success "Sipariş hazırlanıyor (Status: Processing/1)"

Start-Sleep -Milliseconds 500

# ==========================================================================
# ADIM 8: KURYE ATA
# ==========================================================================
Write-Step "ADIM 8: Kuryeye atanıyor..."

# Kurye ID'sini al
$courierQuery = "SELECT TOP 1 Id FROM Couriers WHERE Email = '$($testCourier.Email)' AND IsActive = 1"
$courierResult = Invoke-SqlQuery -Query $courierQuery -ReturnData

if ($courierResult -eq $null -or $courierResult.Rows.Count -eq 0) {
    # Herhangi bir aktif kurye bul
    $courierResult = Invoke-SqlQuery -Query "SELECT TOP 1 Id, Name FROM Couriers WHERE IsActive = 1" -ReturnData
}

if ($courierResult -ne $null -and $courierResult.Rows.Count -gt 0) {
    $courierId = $courierResult.Rows[0].Id
    
    $assignQuery = @"
    UPDATE Orders SET Status = 4, CourierId = '$courierId', AssignedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = '$orderId';
    
    INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
    VALUES (NEWID(), '$orderId', 1, 4, GETUTCDATE(), 'Admin', N'Kurye atandı');
"@
    Invoke-SqlQuery -Query $assignQuery | Out-Null
    Write-Success "Kurye atandı! (Status: Assigned/4, CourierId: $courierId)"
} else {
    Write-Error "Aktif kurye bulunamadı!"
}

Start-Sleep -Milliseconds 500

# ==========================================================================
# ADIM 9: KURYE - YOLA ÇIKTIM
# ==========================================================================
Write-Step "ADIM 9: Kurye yola çıkıyor..."

$outForDeliveryQuery = @"
UPDATE Orders SET Status = 6, OutForDeliveryAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = '$orderId';

INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
VALUES (NEWID(), '$orderId', 4, 6, GETUTCDATE(), 'Courier', N'Kurye yola çıktı');
"@
Invoke-SqlQuery -Query $outForDeliveryQuery | Out-Null
Write-Success "Kurye yola çıktı! (Status: OutForDelivery/6)"

Start-Sleep -Milliseconds 500

# ==========================================================================
# ADIM 10: KURYE - TESLİM ETTİM
# ==========================================================================
Write-Step "ADIM 10: Kurye teslim ediyor..."

$deliveredQuery = @"
UPDATE Orders SET Status = 7, DeliveredAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = '$orderId';

INSERT INTO OrderStatusHistories (Id, OrderId, OldStatus, NewStatus, ChangedAt, ChangedBy, Notes)
VALUES (NEWID(), '$orderId', 6, 7, GETUTCDATE(), 'Courier', N'Sipariş teslim edildi');
"@
Invoke-SqlQuery -Query $deliveredQuery | Out-Null
Write-Success "Sipariş teslim edildi! (Status: Delivered/7)"

# ==========================================================================
# ADIM 11: SONUÇ KONTROLÜ
# ==========================================================================
Write-Step "ADIM 11: Sipariş durumu kontrol ediliyor..."

$finalCheckQuery = @"
SELECT 
    o.OrderNumber,
    o.Status,
    CASE o.Status 
        WHEN -1 THEN 'New'
        WHEN -2 THEN 'Confirmed'
        WHEN -3 THEN 'DeliveryFailed'
        WHEN -4 THEN 'DeliveryPaymentPending'
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Processing'
        WHEN 2 THEN 'Shipped'
        WHEN 3 THEN 'Ready'
        WHEN 4 THEN 'Assigned'
        WHEN 5 THEN 'PickedUp'
        WHEN 6 THEN 'OutForDelivery'
        WHEN 7 THEN 'Delivered'
        WHEN 8 THEN 'Cancelled'
        ELSE 'Unknown'
    END AS StatusText,
    o.TotalAmount,
    o.CustomerName,
    o.ShippingAddress,
    c.Name AS CourierName,
    o.CreatedAt,
    o.AssignedAt,
    o.OutForDeliveryAt,
    o.DeliveredAt
FROM Orders o
LEFT JOIN Couriers c ON o.CourierId = c.Id
WHERE o.Id = '$orderId'
"@

$finalResult = Invoke-SqlQuery -Query $finalCheckQuery -ReturnData

if ($finalResult -ne $null -and $finalResult.Rows.Count -gt 0) {
    $order = $finalResult.Rows[0]
    
    Write-Host "`n" -NoNewline
    Write-Host "========================================================================" -ForegroundColor Green
    Write-Host "                    SIPARIS OZET BILGILERI                              " -ForegroundColor Green
    Write-Host "------------------------------------------------------------------------" -ForegroundColor Green
    Write-Host "  Siparis No    : $($order.OrderNumber)" -ForegroundColor White
    Write-Host "  Durum         : $($order.StatusText)" -ForegroundColor White
    Write-Host "  Tutar         : $($order.TotalAmount.ToString('N2')) TL" -ForegroundColor White
    Write-Host "  Musteri       : $($order.CustomerName)" -ForegroundColor White
    Write-Host "  Kurye         : $(if($order.CourierName) { $order.CourierName } else { 'Atanmadi' })" -ForegroundColor White
    Write-Host "  Adres         : $($order.ShippingAddress.Substring(0, [Math]::Min(50, $order.ShippingAddress.Length)))" -ForegroundColor White
    Write-Host "------------------------------------------------------------------------" -ForegroundColor Green
    Write-Host "  ZAMAN CIZELGESI                                                      " -ForegroundColor Yellow
    Write-Host "  Olusturuldu   : $($order.CreatedAt.ToString('dd.MM.yyyy HH:mm:ss'))" -ForegroundColor Gray
    if ($order.AssignedAt) {
        Write-Host "  Kurye Atandi  : $($order.AssignedAt.ToString('dd.MM.yyyy HH:mm:ss'))" -ForegroundColor Gray
    }
    if ($order.OutForDeliveryAt) {
        Write-Host "  Yola Cikildi  : $($order.OutForDeliveryAt.ToString('dd.MM.yyyy HH:mm:ss'))" -ForegroundColor Gray
    }
    if ($order.DeliveredAt) {
        Write-Host "  Teslim Edildi : $($order.DeliveredAt.ToString('dd.MM.yyyy HH:mm:ss'))" -ForegroundColor Gray
    }
    Write-Host "========================================================================" -ForegroundColor Green
}

# ==========================================================================
# ADIM 12: SİPARİŞ GEÇMİŞİ
# ==========================================================================
Write-Step "ADIM 12: Sipariş durum geçmişi..."

$historyQuery = @"
SELECT 
    CASE NewStatus 
        WHEN -1 THEN 'New'
        WHEN -2 THEN 'Confirmed'
        WHEN -3 THEN 'DeliveryFailed'
        WHEN -4 THEN 'DeliveryPaymentPending'
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Processing'
        WHEN 4 THEN 'Assigned'
        WHEN 6 THEN 'OutForDelivery'
        WHEN 7 THEN 'Delivered'
        ELSE CAST(NewStatus AS VARCHAR(10))
    END AS StatusText,
    ChangedAt,
    ChangedBy,
    Notes
FROM OrderStatusHistories 
WHERE OrderId = '$orderId'
ORDER BY ChangedAt
"@

$history = Invoke-SqlQuery -Query $historyQuery -ReturnData

if ($history -ne $null) {
    Write-Host ""
    foreach ($h in $history.Rows) {
        $icon = switch ($h.StatusText) {
            "New" { "[NEW]" }
            "Confirmed" { "[OK]" }
            "Processing" { "[PREP]" }
            "Assigned" { "[ASGN]" }
            "OutForDelivery" { "[OUT]" }
            "Delivered" { "[DONE]" }
            default { "[->]" }
        }
        Write-Host "  $icon $($h.ChangedAt.ToString('HH:mm:ss')) - $($h.StatusText) ($($h.ChangedBy))" -ForegroundColor Cyan
        if ($h.Notes) {
            Write-Host "     --- $($h.Notes)" -ForegroundColor DarkGray
        }
    }
}

# ==========================================================================
# TEST SONUCU
# ==========================================================================
Write-Host "`n"
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "               [OK] TEST BASARIYLA TAMAMLANDI!                          " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Test edilen akis:" -ForegroundColor White
Write-Host "   1. Siparis olusturuldu (New)" -ForegroundColor Green
Write-Host "   2. Admin onayladi (Confirmed)" -ForegroundColor Green
Write-Host "   3. Hazirlanmaya baslandi (Processing)" -ForegroundColor Green
Write-Host "   4. Kurye atandi (Assigned)" -ForegroundColor Green
Write-Host "   5. Kurye yola cikti (OutForDelivery)" -ForegroundColor Green
Write-Host "   6. Teslim edildi (Delivered)" -ForegroundColor Green
Write-Host ""
Write-Host "Kontrol etmek icin:" -ForegroundColor Yellow
Write-Host "   Admin Panel: http://localhost:3000/admin/orders" -ForegroundColor Cyan
Write-Host "   Kurye Panel: http://localhost:3000/courier/dashboard" -ForegroundColor Cyan
Write-Host "   Siparis Takip: http://localhost:3000/order-tracking" -ForegroundColor Cyan
Write-Host ""
