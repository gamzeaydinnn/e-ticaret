-- ==================================================================
-- CustomerSupport Rol İzinlerini Düzeltme Script'i
-- ==================================================================
-- Amaç: CustomerSupport rolünden Products.View, Categories.View ve 
-- Users.View izinlerini kaldırarak backend controller'lar ile 
-- uyumlu hale getirmek.
--
-- Problem: Frontend bu izinlere bakarak menü gösteriyor ama
-- backend AdminLike rol kontrolü yapıyor -> 403 hatası
--
-- Çalıştırmadan önce: Mevcut durumu kontrol et
-- Çalıştırdıktan sonra: Backend'i restart et
-- ==================================================================

-- 1. KONTROL: CustomerSupport'un mevcut izinleri
PRINT '=== MEVCUT İZİNLER (ÖNCE) ==='
SELECT 
    r.Name as RoleName, 
    rc.ClaimValue as Permission
FROM AspNetRoleClaims rc
JOIN AspNetRoles r ON rc.RoleId = r.Id
WHERE r.Name = 'CustomerSupport' 
AND rc.ClaimType = 'Permission'
ORDER BY rc.ClaimValue;

-- 2. SİL: Products.View, Categories.View, Users.View izinlerini kaldır
PRINT '=== İZİNLER KALDIRILIYOR ==='

DELETE rc
FROM AspNetRoleClaims rc
JOIN AspNetRoles r ON rc.RoleId = r.Id
WHERE r.Name = 'CustomerSupport' 
AND rc.ClaimType = 'Permission'
AND rc.ClaimValue IN ('Products.View', 'Categories.View', 'Users.View');

PRINT 'Silinen kayıt sayısı: ' + CAST(@@ROWCOUNT AS VARCHAR(10));

-- 3. KONTROL: Sonrası
PRINT '=== GÜNCEL İZİNLER (SONRA) ==='
SELECT 
    r.Name as RoleName, 
    rc.ClaimValue as Permission
FROM AspNetRoleClaims rc
JOIN AspNetRoles r ON rc.RoleId = r.Id
WHERE r.Name = 'CustomerSupport' 
AND rc.ClaimType = 'Permission'
ORDER BY rc.ClaimValue;

-- ==================================================================
-- Beklenen CustomerSupport izinleri:
-- - Dashboard.View
-- - Orders.View
-- - Orders.ViewDetails
-- - Orders.UpdateStatus
-- - Orders.Cancel
-- - Orders.ProcessRefund
-- - Orders.ViewCustomerInfo
-- - Reports.View
-- - Reports.ViewSales
-- ==================================================================
