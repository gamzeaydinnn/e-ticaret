# ==========================================================================
# TEST-ORDER-FLOW-V3.ps1 - Siparis Akisi End-to-End Test Scripti
# ==========================================================================
# Bu script sunlari yapar:
# 1. Mevcut kullanicilari kullanir
# 2. Siparis olusturur
# 3. Siparis durumunu degistirir (New -> Confirmed -> Processing -> Assigned -> OutForDelivery -> Delivered)
# 4. Her adimda durumu kontrol eder
# ==========================================================================

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

function Invoke-SqlScalar {
    param([string]$Query)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection
        $connection.ConnectionString = $connectionString
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $result = $command.ExecuteScalar()
        
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
Write-Host "          SIPARIS AKISI END-TO-END TEST SCRIPTI V3                      " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""

# ==========================================================================
# ADIM 1: VERITABANI BAGLANTISI KONTROLU
# ==========================================================================
Write-Step "ADIM 1: Veritabani baglantisi kontrol ediliyor..."

$dbCheck = Invoke-SqlQuery -Query "SELECT 1 AS Test" -ReturnData
if ($dbCheck -eq $null) {
    Write-Err "Veritabanina baglanilamadi!"
    exit 1
}
Write-Success "Veritabani baglantisi basarili"

# ==========================================================================
# ADIM 2: MEVCUT VERILERI KONTROL ET
# ==========================================================================
Write-Step "ADIM 2: Mevcut veriler kontrol ediliyor..."

# Musteri bul
$customerQuery = "SELECT TOP 1 Id, FirstName, LastName, Email FROM Users WHERE IsActive = 1"
$customer = Invoke-SqlQuery -Query $customerQuery -ReturnData

if ($customer -eq $null -or $customer.Rows.Count -eq 0) {
    Write-Err "Hic kullanici bulunamadi! Lutfen once bir kullanici olusturun."
    exit 1
}

$custRow = $customer.Rows[0]
Write-Success "Musteri: $($custRow.FirstName) $($custRow.LastName) (ID: $($custRow.Id))"

# Kurye bul
$courierQuery = @"
SELECT TOP 1 c.Id, c.UserId, u.FirstName, u.LastName, c.Phone
FROM Couriers c
INNER JOIN Users u ON c.UserId = u.Id
WHERE c.IsActive = 1
"@
$courier = Invoke-SqlQuery -Query $courierQuery -ReturnData

$hasCourier = ($courier -ne $null -and $courier.Rows.Count -gt 0)
if ($hasCourier) {
    $courRow = $courier.Rows[0]
    Write-Success "Kurye: $($courRow.FirstName) $($courRow.LastName) (ID: $($courRow.Id))"
} else {
    Write-Info "Kurye bulunamadi - kurye atama adimi atlanacak"
}

# Urun bul
$productQuery = "SELECT TOP 1 Id, Name, Price, StockQuantity FROM Products WHERE IsActive = 1 AND StockQuantity > 0"
$product = Invoke-SqlQuery -Query $productQuery -ReturnData

if ($product -eq $null -or $product.Rows.Count -eq 0) {
    Write-Err "Aktif urun bulunamadi! Lutfen once urun ekleyin."
    exit 1
}

$prodRow = $product.Rows[0]
Write-Success "Urun: $($prodRow.Name) - $($prodRow.Price) TL (Stok: $($prodRow.StockQuantity))"

# ==========================================================================
# ADIM 3: SIPARIS OLUSTUR
# ==========================================================================
Write-Step "ADIM 3: Siparis olusturuluyor..."

$orderNumber = "ORD-TEST-" + (Get-Date -Format "yyyyMMddHHmmss")
$quantity = 2
$unitPrice = $prodRow.Price
$totalPrice = $quantity * $unitPrice

# Siparis olustur (ID identity oldugu icin belirtmiyoruz)
$createOrder = @"
INSERT INTO Orders (OrderNumber, UserId, IsGuestOrder, CustomerName, CustomerPhone, CustomerEmail,
    ShippingAddress, ShippingCity, TotalPrice, FinalPrice, Status, OrderDate, PaymentStatus, ShippingMethod,
    ShippingCost, DiscountAmount, IsActive, CreatedAt, UpdatedAt, Currency, VatAmount, CouponDiscountAmount, CampaignDiscountAmount)
OUTPUT INSERTED.Id
VALUES 
('$orderNumber', $($custRow.Id), 0, '$($custRow.FirstName) $($custRow.LastName)', 
 '05551112233', '$($custRow.Email)', 'Test Mahallesi Test Sokak No:1 Daire:2', 'Istanbul',
 $totalPrice, $totalPrice, -1, GETDATE(), 'Pending', 'Standard', 0, 0, 1, GETDATE(), GETDATE(), 'TRY', 0, 0, 0)
"@

$orderId = Invoke-SqlScalar -Query $createOrder

if ($orderId -eq $null) {
    Write-Err "Siparis olusturulamadi!"
    exit 1
}

Write-Success "Siparis olusturuldu: $orderNumber (ID: $orderId)"

# Siparis kalemi ekle
$createOrderItem = @"
INSERT INTO OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, TotalPrice, IsActive, CreatedAt, UpdatedAt)
VALUES ($orderId, $($prodRow.Id), '$($prodRow.Name)', $unitPrice, $quantity, $totalPrice, 1, GETDATE(), GETDATE())
"@
Invoke-SqlQuery -Query $createOrderItem

# Siparis gecmisi ekle
$addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 0, -1, 'System', 'Siparis olusturuldu', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Info "  -> Toplam: $totalPrice TL"
Write-Info "  -> Durum: New (-1)"

# ==========================================================================
# ADIM 4: SIPARISI ONAYLA (New -> Confirmed)
# ==========================================================================
Write-Step "ADIM 4: Siparis onaylaniyor (New -> Confirmed)..."
Start-Sleep -Seconds 1

$confirmOrder = "UPDATE Orders SET Status = -2, ConfirmedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
Invoke-SqlQuery -Query $confirmOrder

$addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, -1, -2, 'Admin', 'Siparis onaylandi', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis onaylandi: Confirmed (-2)"

# ==========================================================================
# ADIM 5: HAZIRLANIYOR (Confirmed -> Processing)
# ==========================================================================
Write-Step "ADIM 5: Siparis hazirlaniyor (Confirmed -> Processing)..."
Start-Sleep -Seconds 1

$processOrder = "UPDATE Orders SET Status = 1, ProcessingStartedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
Invoke-SqlQuery -Query $processOrder

$addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, -2, 1, 'Admin', 'Siparis hazirlaniyor', GETDATE(), 1, GETDATE())
"@
Invoke-SqlQuery -Query $addHistory

Write-Success "Siparis hazirlaniyor: Processing (1)"

# ==========================================================================
# ADIM 6: KURYE ATA (Processing -> Assigned)
# ==========================================================================
if ($hasCourier) {
    Write-Step "ADIM 6: Kurye ataniyor (Processing -> Assigned)..."
    Start-Sleep -Seconds 1

    $assignCourier = "UPDATE Orders SET Status = 4, CourierId = $($courRow.Id), AssignedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    Invoke-SqlQuery -Query $assignCourier

    $addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 1, 4, 'Admin', 'Kurye atandi: $($courRow.FirstName) $($courRow.LastName)', GETDATE(), 1, GETDATE())
"@
    Invoke-SqlQuery -Query $addHistory

    Write-Success "Kurye atandi: $($courRow.FirstName) $($courRow.LastName)"
    
    # ==========================================================================
    # ADIM 7: KURYE YOLA CIKTI (Assigned -> OutForDelivery)
    # ==========================================================================
    Write-Step "ADIM 7: Kurye yola cikiyor (Assigned -> OutForDelivery)..."
    Start-Sleep -Seconds 1

    $outForDelivery = "UPDATE Orders SET Status = 6, OutForDeliveryAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    Invoke-SqlQuery -Query $outForDelivery

    $addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 4, 6, 'Courier', 'Kurye yola cikti', GETDATE(), 1, GETDATE())
"@
    Invoke-SqlQuery -Query $addHistory

    Write-Success "Kurye yola cikti: OutForDelivery (6)"

    # ==========================================================================
    # ADIM 8: TESLIM EDILDI (OutForDelivery -> Delivered)
    # ==========================================================================
    Write-Step "ADIM 8: Siparis teslim ediliyor (OutForDelivery -> Delivered)..."
    Start-Sleep -Seconds 1

    $delivered = "UPDATE Orders SET Status = 7, DeliveredAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    Invoke-SqlQuery -Query $delivered

    $addHistory = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 6, 7, 'Courier', 'Siparis teslim edildi', GETDATE(), 1, GETDATE())
"@
    Invoke-SqlQuery -Query $addHistory

    Write-Success "Siparis teslim edildi: Delivered (7)"
} else {
    Write-Info "Kurye olmadigi icin 6-7-8 adimlari atlandi"
}

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
WHERE o.Id = $orderId
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
    if ($order.ConfirmedAt -and $order.ConfirmedAt -ne [DBNull]::Value) {
        Write-Host "  Onaylandi     : $($order.ConfirmedAt)" -ForegroundColor Gray
    }
    if ($order.AssignedAt -and $order.AssignedAt -ne [DBNull]::Value) {
        Write-Host "  Kurye Atandi  : $($order.AssignedAt)" -ForegroundColor Gray
    }
    if ($order.OutForDeliveryAt -and $order.OutForDeliveryAt -ne [DBNull]::Value) {
        Write-Host "  Yola Cikildi  : $($order.OutForDeliveryAt)" -ForegroundColor Gray
    }
    if ($order.DeliveredAt -and $order.DeliveredAt -ne [DBNull]::Value) {
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
WHERE OrderId = $orderId
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
if ($hasCourier) {
    Write-Host "   4. Kurye atandi (Assigned = 4)" -ForegroundColor Green
    Write-Host "   5. Kurye yola cikti (OutForDelivery = 6)" -ForegroundColor Green
    Write-Host "   6. Teslim edildi (Delivered = 7)" -ForegroundColor Green
}
Write-Host ""
Write-Host "Kontrol etmek icin:" -ForegroundColor Yellow
Write-Host "   Admin Panel: http://localhost:3000/admin/orders" -ForegroundColor Cyan
Write-Host "   Kurye Panel: http://localhost:3000/courier/dashboard" -ForegroundColor Cyan
Write-Host "   Siparis Takip: http://localhost:3000/order-tracking" -ForegroundColor Cyan
Write-Host ""
Write-Host "Siparis No: $orderNumber" -ForegroundColor White
Write-Host ""
