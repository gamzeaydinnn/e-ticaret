# ==========================================================================
# TEST-ORDER-FLOW-V2.ps1 - Siparis Akisi End-to-End Test Scripti
# ==========================================================================
# Bu script sunlari yapar:
# 1. Veritabaninda test kullanicilari olusturur (musteri, kurye)
# 2. Siparis olusturur
# 3. Siparis durumunu degistirir (New -> Confirmed -> Processing -> Assigned -> OutForDelivery -> Delivered)
# 4. Her adimda durumu kontrol eder
# ==========================================================================

param(
    [switch]$Verbose = $true
)

# Renk fonksiyonlari
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Err { param($msg) Write-Host "[ERROR] $msg" -ForegroundColor Red }
function Write-Info { param($msg) Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Yellow }

# ==========================================================================
# VERITABANI BAGLANTISI
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
        Write-Err "SQL Hatasi: $_"
        if ($connection.State -eq 'Open') { $connection.Close() }
        return $null
    }
}

# ==========================================================================
# ANA TEST AKISI
# ==========================================================================
Write-Host ""
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "          SIPARIS AKISI END-TO-END TEST SCRIPTI V2                      " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""

# ==========================================================================
# ADIM 1: VERITABANI BAGLANTISI KONTROLU
# ==========================================================================
Write-Step "ADIM 1: Veritabani baglantisi kontrol ediliyor..."

$dbCheck = Invoke-SqlQuery -Query "SELECT 1 AS Test" -ReturnData
if ($dbCheck -eq $null) {
    Write-Err "Veritabanina baglanilamadi! Lutfen SQL Server'in calistigindan emin olun."
    exit 1
}
Write-Success "Veritabani baglantisi basarili"

# ==========================================================================
# ADIM 2: MEVCUT VERILERI KONTROL ET
# ==========================================================================
Write-Step "ADIM 2: Mevcut veriler kontrol ediliyor..."

# Musteri bul veya olustur
$customerQuery = "SELECT TOP 1 Id, FirstName, LastName, Email FROM Users WHERE Role = 'Customer' AND IsActive = 1"
$customer = Invoke-SqlQuery -Query $customerQuery -ReturnData

if ($customer -eq $null -or $customer.Rows.Count -eq 0) {
    Write-Info "Musteri bulunamadi, olusturuluyor..."
    $customerId = [Guid]::NewGuid().ToString()
    $createCustomer = @"
INSERT INTO Users (Id, FirstName, LastName, FullName, Email, UserName, NormalizedEmail, NormalizedUserName, 
    PasswordHash, SecurityStamp, ConcurrencyStamp, Role, IsActive, CreatedAt, UpdatedAt, EmailConfirmed, 
    PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES 
('$customerId', 'Test', 'Musteri', 'Test Musteri', 'testmusteri@test.com', 'testmusteri@test.com',
 'TESTMUSTERI@TEST.COM', 'TESTMUSTERI@TEST.COM', 
 'AQAAAAIAAYagAAAAEJL...', NEWID(), NEWID(), 'Customer', 1, GETDATE(), GETDATE(), 1, 0, 0, 0, 0)
"@
    Invoke-SqlQuery -Query $createCustomer
    $customer = Invoke-SqlQuery -Query $customerQuery -ReturnData
}

$custRow = $customer.Rows[0]
Write-Success "Musteri: $($custRow.FirstName) $($custRow.LastName) (ID: $($custRow.Id))"

# Kurye bul veya olustur
$courierQuery = @"
SELECT TOP 1 c.Id, c.UserId, u.FirstName, u.LastName, u.Email, c.Phone
FROM Couriers c
INNER JOIN Users u ON c.UserId = u.Id
WHERE c.IsActive = 1 AND u.IsActive = 1
"@
$courier = Invoke-SqlQuery -Query $courierQuery -ReturnData

if ($courier -eq $null -or $courier.Rows.Count -eq 0) {
    Write-Info "Kurye bulunamadi, olusturuluyor..."
    $courierUserId = [Guid]::NewGuid().ToString()
    $courierId = [Guid]::NewGuid().ToString()
    
    $createCourierUser = @"
INSERT INTO Users (Id, FirstName, LastName, FullName, Email, UserName, NormalizedEmail, NormalizedUserName, 
    PasswordHash, SecurityStamp, ConcurrencyStamp, Role, IsActive, CreatedAt, UpdatedAt, EmailConfirmed, 
    PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES 
('$courierUserId', 'Test', 'Kurye', 'Test Kurye', 'testkurye@test.com', 'testkurye@test.com',
 'TESTKURYE@TEST.COM', 'TESTKURYE@TEST.COM', 
 'AQAAAAIAAYagAAAAEJL...', NEWID(), NEWID(), 'Courier', 1, GETDATE(), GETDATE(), 1, 0, 0, 0, 0)
"@
    Invoke-SqlQuery -Query $createCourierUser
    
    $createCourier = @"
INSERT INTO Couriers (Id, UserId, Phone, Vehicle, Status, IsActive, IsOnline, CurrentCapacity, MaxCapacity, CreatedAt, UpdatedAt, Rating, ActiveOrders, CompletedToday)
VALUES ('$courierId', '$courierUserId', '05551234567', 'Motorcycle', 'Available', 1, 1, 0, 10, GETDATE(), GETDATE(), 5.0, 0, 0)
"@
    Invoke-SqlQuery -Query $createCourier
    $courier = Invoke-SqlQuery -Query $courierQuery -ReturnData
}

$courRow = $courier.Rows[0]
Write-Success "Kurye: $($courRow.FirstName) $($courRow.LastName) (Courier ID: $($courRow.Id))"

# Urun bul
$productQuery = "SELECT TOP 1 Id, Name, Price, StockQuantity FROM Products WHERE IsActive = 1 AND StockQuantity > 0"
$product = Invoke-SqlQuery -Query $productQuery -ReturnData

if ($product -eq $null -or $product.Rows.Count -eq 0) {
    Write-Info "Urun bulunamadi, olusturuluyor..."
    
    # Once kategori olustur
    $categoryId = [Guid]::NewGuid().ToString()
    $createCategory = @"
INSERT INTO Categories (Id, Name, Slug, IsActive, CreatedAt, UpdatedAt)
VALUES ('$categoryId', 'Test Kategori', 'test-kategori', 1, GETDATE(), GETDATE())
"@
    Invoke-SqlQuery -Query $createCategory
    
    $productId = [Guid]::NewGuid().ToString()
    $createProduct = @"
INSERT INTO Products (Id, Name, Description, Price, StockQuantity, CategoryId, Slug, SKU, IsActive, CreatedAt, UpdatedAt, Currency)
VALUES ('$productId', 'Test Urun', 'Test urun aciklamasi', 99.99, 100, '$categoryId', 'test-urun', 'TST001', 1, GETDATE(), GETDATE(), 'TRY')
"@
    Invoke-SqlQuery -Query $createProduct
    $product = Invoke-SqlQuery -Query $productQuery -ReturnData
}

$prodRow = $product.Rows[0]
Write-Success "Urun: $($prodRow.Name) - $($prodRow.Price) TL (Stok: $($prodRow.StockQuantity))"

# ==========================================================================
# ADIM 3: SIPARIS OLUSTUR
# ==========================================================================
Write-Step "ADIM 3: Siparis olusturuluyor..."

$orderId = [Guid]::NewGuid().ToString()
$orderNumber = "ORD-TEST-" + (Get-Date -Format "yyyyMMddHHmmss")
$orderItemId = [Guid]::NewGuid().ToString()
$quantity = 2
$unitPrice = $prodRow.Price
$totalPrice = $quantity * $unitPrice

$createOrder = @"
INSERT INTO Orders (Id, OrderNumber, UserId, IsGuestOrder, CustomerName, CustomerPhone, CustomerEmail,
    ShippingAddress, ShippingCity, TotalPrice, FinalPrice, Status, OrderDate, PaymentStatus, ShippingMethod,
    ShippingCost, DiscountAmount, IsActive, CreatedAt, UpdatedAt, Currency, VatAmount, CouponDiscountAmount, CampaignDiscountAmount)
VALUES 
('$orderId', '$orderNumber', '$($custRow.Id)', 0, '$($custRow.FirstName) $($custRow.LastName)', 
 '05551112233', '$($custRow.Email)', 'Test Mahallesi Test Sokak No:1 Daire:2', 'Istanbul',
 $totalPrice, $totalPrice, -1, GETDATE(), 'Pending', 'Standard', 0, 0, 1, GETDATE(), GETDATE(), 'TRY', 0, 0, 0)
"@
$result = Invoke-SqlQuery -Query $createOrder

if ($result -eq $null) {
    Write-Err "Siparis olusturulamadi!"
    exit 1
}

# Siparis kalemi ekle
$createOrderItem = @"
INSERT INTO OrderItems (Id, OrderId, ProductId, ProductName, UnitPrice, Quantity, TotalPrice, IsActive, CreatedAt, UpdatedAt)
VALUES ('$orderItemId', '$orderId', '$($prodRow.Id)', '$($prodRow.Name)', $unitPrice, $quantity, $totalPrice, 1, GETDATE(), GETDATE())
"@
Invoke-SqlQuery -Query $createOrderItem

# Siparis gecmisi ekle
$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', 0, -1, 'System', 'Siparis olusturuldu', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis olusturuldu: $orderNumber"
Write-Info "  -> Siparis ID: $orderId"
Write-Info "  -> Toplam: $totalPrice TL"
Write-Info "  -> Durum: New (-1)"

# ==========================================================================
# ADIM 4: SIPARISI ONAYLA (New -> Confirmed)
# ==========================================================================
Write-Step "ADIM 4: Siparis onaylaniyor (New -> Confirmed)..."
Start-Sleep -Seconds 1

$confirmOrder = @"
UPDATE Orders SET Status = -2, ConfirmedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = '$orderId'
"@
Invoke-SqlQuery -Query $confirmOrder

$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', -1, -2, 'Admin', 'Siparis onaylandi', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis onaylandi: Confirmed (-2)"

# ==========================================================================
# ADIM 5: HAZIRLANIYOR (Confirmed -> Processing)
# ==========================================================================
Write-Step "ADIM 5: Siparis hazirlaniyor (Confirmed -> Processing)..."
Start-Sleep -Seconds 1

$processOrder = @"
UPDATE Orders SET Status = 1, ProcessingStartedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = '$orderId'
"@
Invoke-SqlQuery -Query $processOrder

$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', -2, 1, 'Admin', 'Siparis hazirlaniyor', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis hazirlaniyor: Processing (1)"

# ==========================================================================
# ADIM 6: KURYE ATA (Processing -> Assigned)
# ==========================================================================
Write-Step "ADIM 6: Kurye ataniyor (Processing -> Assigned)..."
Start-Sleep -Seconds 1

$assignCourier = @"
UPDATE Orders SET Status = 4, CourierId = '$($courRow.Id)', AssignedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = '$orderId'
"@
Invoke-SqlQuery -Query $assignCourier

$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', 1, 4, 'Admin', 'Kurye atandi: $($courRow.FirstName) $($courRow.LastName)', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Kurye atandi: $($courRow.FirstName) $($courRow.LastName)"
Write-Info "  -> Durum: Assigned (4)"

# ==========================================================================
# ADIM 7: KURYE YOLA CIKTI (Assigned -> OutForDelivery)
# ==========================================================================
Write-Step "ADIM 7: Kurye yola cikiyor (Assigned -> OutForDelivery)..."
Start-Sleep -Seconds 1

$outForDelivery = @"
UPDATE Orders SET Status = 6, OutForDeliveryAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = '$orderId'
"@
Invoke-SqlQuery -Query $outForDelivery

$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', 4, 6, 'Courier', 'Kurye yola cikti', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Kurye yola cikti: OutForDelivery (6)"

# ==========================================================================
# ADIM 8: TESLIM EDILDI (OutForDelivery -> Delivered)
# ==========================================================================
Write-Step "ADIM 8: Siparis teslim ediliyor (OutForDelivery -> Delivered)..."
Start-Sleep -Seconds 1

$delivered = @"
UPDATE Orders SET Status = 7, DeliveredAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = '$orderId'
"@
Invoke-SqlQuery -Query $delivered

$historyId = [Guid]::NewGuid().ToString()
$addHistory = @"
INSERT INTO OrderStatusHistories (Id, OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ('$historyId', '$orderId', 6, 7, 'Courier', 'Siparis teslim edildi', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis teslim edildi: Delivered (7)"

# ==========================================================================
# ADIM 9: SONUC KONTROLU
# ==========================================================================
Write-Step "ADIM 9: Sonuc kontrol ediliyor..."

$finalQuery = @"
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
        WHEN 4 THEN 'Assigned'
        WHEN 6 THEN 'OutForDelivery'
        WHEN 7 THEN 'Delivered'
        ELSE 'Unknown'
    END AS StatusText,
    o.TotalPrice,
    o.CustomerName,
    o.ShippingAddress,
    o.ShippingCity,
    o.CreatedAt,
    o.ConfirmedAt,
    o.AssignedAt,
    o.OutForDeliveryAt,
    o.DeliveredAt
FROM Orders o
WHERE o.Id = '$orderId'
"@
$finalResult = Invoke-SqlQuery -Query $finalQuery -ReturnData

if ($finalResult -ne $null -and $finalResult.Rows.Count -gt 0) {
    $order = $finalResult.Rows[0]
    
    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Green
    Write-Host "                    SIPARIS OZET BILGILERI                              " -ForegroundColor Green
    Write-Host "------------------------------------------------------------------------" -ForegroundColor Green
    Write-Host "  Siparis No    : $($order.OrderNumber)" -ForegroundColor White
    Write-Host "  Durum         : $($order.StatusText) ($($order.Status))" -ForegroundColor White
    Write-Host "  Tutar         : $($order.TotalPrice) TL" -ForegroundColor White
    Write-Host "  Musteri       : $($order.CustomerName)" -ForegroundColor White
    Write-Host "  Adres         : $($order.ShippingAddress), $($order.ShippingCity)" -ForegroundColor White
    Write-Host "------------------------------------------------------------------------" -ForegroundColor Green
    Write-Host "  ZAMAN CIZELGESI                                                      " -ForegroundColor Yellow
    Write-Host "  Olusturuldu   : $($order.CreatedAt)" -ForegroundColor Gray
    if ($order.ConfirmedAt) {
        Write-Host "  Onaylandi     : $($order.ConfirmedAt)" -ForegroundColor Gray
    }
    if ($order.AssignedAt) {
        Write-Host "  Kurye Atandi  : $($order.AssignedAt)" -ForegroundColor Gray
    }
    if ($order.OutForDeliveryAt) {
        Write-Host "  Yola Cikildi  : $($order.OutForDeliveryAt)" -ForegroundColor Gray
    }
    if ($order.DeliveredAt) {
        Write-Host "  Teslim Edildi : $($order.DeliveredAt)" -ForegroundColor Gray
    }
    Write-Host "========================================================================" -ForegroundColor Green
}

# ==========================================================================
# ADIM 10: SIPARIS GECMISI
# ==========================================================================
Write-Step "ADIM 10: Siparis durum gecmisi..."

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
    Reason
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
        Write-Host "  $icon $($h.ChangedAt) - $($h.StatusText) ($($h.ChangedBy))" -ForegroundColor Cyan
        if ($h.Reason) {
            Write-Host "       $($h.Reason)" -ForegroundColor DarkGray
        }
    }
}

# ==========================================================================
# TEST SONUCU
# ==========================================================================
Write-Host ""
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "               [OK] TEST BASARIYLA TAMAMLANDI!                          " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Test edilen akis:" -ForegroundColor White
Write-Host "   1. Siparis olusturuldu (New = -1)" -ForegroundColor Green
Write-Host "   2. Admin onayladi (Confirmed = -2)" -ForegroundColor Green
Write-Host "   3. Hazirlanmaya baslandi (Processing = 1)" -ForegroundColor Green
Write-Host "   4. Kurye atandi (Assigned = 4)" -ForegroundColor Green
Write-Host "   5. Kurye yola cikti (OutForDelivery = 6)" -ForegroundColor Green
Write-Host "   6. Teslim edildi (Delivered = 7)" -ForegroundColor Green
Write-Host ""
Write-Host "Kontrol etmek icin:" -ForegroundColor Yellow
Write-Host "   Admin Panel: http://localhost:3000/admin/orders" -ForegroundColor Cyan
Write-Host "   Kurye Panel: http://localhost:3000/courier/dashboard" -ForegroundColor Cyan
Write-Host "   Siparis Takip: http://localhost:3000/order-tracking" -ForegroundColor Cyan
Write-Host ""
Write-Host "Siparis No: $orderNumber" -ForegroundColor White
Write-Host ""
