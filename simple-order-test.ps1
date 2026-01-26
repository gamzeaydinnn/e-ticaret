# ==========================================================================
# SIMPLE-ORDER-TEST.ps1 - Basit Siparis Test Scripti
# ==========================================================================

$connStr = "Server=localhost,1435;Database=ECommerceDb;User Id=sa;Password=ECom1234;TrustServerCertificate=True;"

Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "          SIPARIS AKISI TEST SCRIPTI                                    " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta

# 1. Baglanti testi
Write-Host "`n[ADIM 1] Veritabani baglantisi..." -ForegroundColor Yellow
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Host "[OK] Baglanti basarili" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Baglanti basarisiz: $_" -ForegroundColor Red
    exit 1
}

# 2. Musteri bul
Write-Host "`n[ADIM 2] Musteri araniyor..." -ForegroundColor Yellow
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT TOP 1 Id, FirstName, LastName, Email FROM Users WHERE IsActive = 1"
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    $customerId = $reader["Id"]
    $customerName = "$($reader['FirstName']) $($reader['LastName'])"
    $customerEmail = $reader["Email"]
    Write-Host "[OK] Musteri: $customerName (ID: $customerId)" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Musteri bulunamadi" -ForegroundColor Red
    $conn.Close()
    exit 1
}
$reader.Close()

# 3. Urun bul
Write-Host "`n[ADIM 3] Urun araniyor..." -ForegroundColor Yellow
$cmd.CommandText = "SELECT TOP 1 Id, Name, Price FROM Products WHERE IsActive = 1 AND StockQuantity > 0"
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    $productId = $reader["Id"]
    $productName = $reader["Name"]
    $productPrice = $reader["Price"]
    Write-Host "[OK] Urun: $productName - $productPrice TL (ID: $productId)" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Urun bulunamadi" -ForegroundColor Red
    $conn.Close()
    exit 1
}
$reader.Close()

# 4. Kurye bul (opsiyonel)
Write-Host "`n[ADIM 4] Kurye araniyor..." -ForegroundColor Yellow
$cmd.CommandText = "SELECT TOP 1 c.Id, u.FirstName, u.LastName FROM Couriers c INNER JOIN Users u ON c.UserId = u.Id WHERE c.IsActive = 1"
$reader = $cmd.ExecuteReader()
$hasCourier = $false
if ($reader.Read()) {
    $courierId = $reader["Id"]
    $courierName = "$($reader['FirstName']) $($reader['LastName'])"
    $hasCourier = $true
    Write-Host "[OK] Kurye: $courierName (ID: $courierId)" -ForegroundColor Green
} else {
    Write-Host "[INFO] Kurye bulunamadi - kurye adimlari atlanacak" -ForegroundColor Cyan
}
$reader.Close()

# 5. Siparis olustur
Write-Host "`n[ADIM 5] Siparis olusturuluyor..." -ForegroundColor Yellow
$orderNumber = "ORD-TEST-" + (Get-Date -Format "yyyyMMddHHmmss")
$totalPrice = 2 * $productPrice

$cmd.CommandText = @"
INSERT INTO Orders (OrderNumber, UserId, IsGuestOrder, CustomerName, CustomerPhone, CustomerEmail,
    ShippingAddress, ShippingCity, TotalPrice, FinalPrice, Status, OrderDate, PaymentStatus, ShippingMethod,
    ShippingCost, DiscountAmount, IsActive, CreatedAt, UpdatedAt, Currency, VatAmount, CouponDiscountAmount, CampaignDiscountAmount, Priority)
OUTPUT INSERTED.Id
VALUES 
('$orderNumber', $customerId, 0, '$customerName', '05551112233', '$customerEmail', 
 'Test Mahallesi Test Sokak No:1', 'Istanbul', $totalPrice, $totalPrice, -1, GETDATE(), 
 0, 'Standard', 0, 0, 1, GETDATE(), GETDATE(), 'TRY', 0, 0, 0, 'Normal')
"@

$orderId = $cmd.ExecuteScalar()
Write-Host "[OK] Siparis olusturuldu: $orderNumber (ID: $orderId)" -ForegroundColor Green

# 6. Siparis kalemi ekle
$cmd.CommandText = @"
INSERT INTO OrderItems (OrderId, ProductId, UnitPrice, Quantity, ExpectedWeightGrams, IsActive, CreatedAt, UpdatedAt)
VALUES ($orderId, $productId, $productPrice, 2, 0, 1, GETDATE(), GETDATE())
"@
$cmd.ExecuteNonQuery() | Out-Null

# 7. Siparis gecmisi - New
$cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 0, -1, 'System', 'Siparis olusturuldu', GETDATE(), 1, GETDATE())
"@
$cmd.ExecuteNonQuery() | Out-Null

# ===== DURUM GECISLERI =====

# 8. New -> Confirmed
Write-Host "`n[ADIM 6] New -> Confirmed..." -ForegroundColor Yellow
Start-Sleep -Milliseconds 500
$cmd.CommandText = "UPDATE Orders SET Status = -2, ConfirmedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
$cmd.ExecuteNonQuery() | Out-Null
$cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, -1, -2, 'Admin', 'Siparis onaylandi', GETDATE(), 1, GETDATE())
"@
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "[OK] Siparis onaylandi (Confirmed = -2)" -ForegroundColor Green

# 9. Confirmed -> Processing
Write-Host "`n[ADIM 7] Confirmed -> Processing..." -ForegroundColor Yellow
Start-Sleep -Milliseconds 500
$cmd.CommandText = "UPDATE Orders SET Status = 1, ProcessingStartedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
$cmd.ExecuteNonQuery() | Out-Null
$cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, -2, 1, 'Admin', 'Siparis hazirlaniyor', GETDATE(), 1, GETDATE())
"@
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "[OK] Siparis hazirlaniyor (Processing = 1)" -ForegroundColor Green

if ($hasCourier) {
    # 10. Processing -> Assigned
    Write-Host "`n[ADIM 8] Processing -> Assigned..." -ForegroundColor Yellow
    Start-Sleep -Milliseconds 500
    $cmd.CommandText = "UPDATE Orders SET Status = 4, CourierId = $courierId, AssignedAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    $cmd.ExecuteNonQuery() | Out-Null
    $cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 1, 4, 'Admin', 'Kurye atandi: $courierName', GETDATE(), 1, GETDATE())
"@
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[OK] Kurye atandi: $courierName (Assigned = 4)" -ForegroundColor Green

    # 11. Assigned -> OutForDelivery
    Write-Host "`n[ADIM 9] Assigned -> OutForDelivery..." -ForegroundColor Yellow
    Start-Sleep -Milliseconds 500
    $cmd.CommandText = "UPDATE Orders SET Status = 6, OutForDeliveryAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    $cmd.ExecuteNonQuery() | Out-Null
    $cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 4, 6, 'Courier', 'Kurye yola cikti', GETDATE(), 1, GETDATE())
"@
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[OK] Kurye yola cikti (OutForDelivery = 6)" -ForegroundColor Green

    # 12. OutForDelivery -> Delivered
    Write-Host "`n[ADIM 10] OutForDelivery -> Delivered..." -ForegroundColor Yellow
    Start-Sleep -Milliseconds 500
    $cmd.CommandText = "UPDATE Orders SET Status = 7, DeliveredAt = GETDATE(), UpdatedAt = GETDATE() WHERE Id = $orderId"
    $cmd.ExecuteNonQuery() | Out-Null
    $cmd.CommandText = @"
INSERT INTO OrderStatusHistories (OrderId, PreviousStatus, NewStatus, ChangedBy, Reason, ChangedAt, IsActive, CreatedAt)
VALUES ($orderId, 6, 7, 'Courier', 'Siparis teslim edildi', GETDATE(), 1, GETDATE())
"@
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Host "[OK] Siparis teslim edildi (Delivered = 7)" -ForegroundColor Green
}

# 13. Sonuc
Write-Host "`n[SONUC] Siparis durumu kontrol ediliyor..." -ForegroundColor Yellow
$cmd.CommandText = @"
SELECT 
    OrderNumber,
    Status,
    CASE Status 
        WHEN -1 THEN 'New'
        WHEN -2 THEN 'Confirmed'
        WHEN 1 THEN 'Processing'
        WHEN 4 THEN 'Assigned'
        WHEN 6 THEN 'OutForDelivery'
        WHEN 7 THEN 'Delivered'
        ELSE 'Unknown'
    END AS StatusText,
    TotalPrice,
    CreatedAt
FROM Orders WHERE Id = $orderId
"@
$reader = $cmd.ExecuteReader()
if ($reader.Read()) {
    Write-Host ""
    Write-Host "========================================================================" -ForegroundColor Green
    Write-Host "  SIPARIS: $($reader['OrderNumber'])" -ForegroundColor White
    Write-Host "  DURUM  : $($reader['StatusText']) ($($reader['Status']))" -ForegroundColor White
    Write-Host "  TUTAR  : $($reader['TotalPrice']) TL" -ForegroundColor White
    Write-Host "  TARIH  : $($reader['CreatedAt'])" -ForegroundColor White
    Write-Host "========================================================================" -ForegroundColor Green
}
$reader.Close()

# 14. Gecmis
Write-Host "`n[GECMIS] Siparis durum gecmisi:" -ForegroundColor Yellow
$cmd.CommandText = @"
SELECT 
    CASE NewStatus 
        WHEN -1 THEN 'New'
        WHEN -2 THEN 'Confirmed'
        WHEN 1 THEN 'Processing'
        WHEN 4 THEN 'Assigned'
        WHEN 6 THEN 'OutForDelivery'
        WHEN 7 THEN 'Delivered'
        ELSE CAST(NewStatus AS VARCHAR)
    END AS StatusText,
    ChangedBy,
    Reason,
    ChangedAt
FROM OrderStatusHistories 
WHERE OrderId = $orderId
ORDER BY ChangedAt
"@
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "  [$($reader['StatusText'])] $($reader['ChangedAt']) - $($reader['ChangedBy']): $($reader['Reason'])" -ForegroundColor Cyan
}
$reader.Close()

$conn.Close()

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host "               TEST BASARIYLA TAMAMLANDI!                               " -ForegroundColor Magenta
Write-Host "========================================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Siparis No: $orderNumber" -ForegroundColor White
Write-Host ""
Write-Host "Web'de kontrol icin:" -ForegroundColor Yellow
Write-Host "  Admin Panel: http://localhost:3000/admin/orders" -ForegroundColor Cyan
Write-Host "  Siparis Takip: http://localhost:3000/order-tracking" -ForegroundColor Cyan
Write-Host ""
