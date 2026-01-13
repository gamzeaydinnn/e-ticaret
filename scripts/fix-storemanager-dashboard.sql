-- ============================================================
-- StoreManager Dashboard İzni Düzeltme Script
-- ============================================================
-- Bu script StoreManager rolüne dashboard.view iznini ekler
-- Kullanım: SQL Server Management Studio veya sqlcmd ile çalıştır
-- ============================================================

USE [ECommerceDb];
GO

-- StoreManager rol ID'sini al
DECLARE @StoreManagerRoleId INT;
SELECT @StoreManagerRoleId = Id FROM AspNetRoles WHERE Name = 'StoreManager';

-- dashboard.view permission ID'sini al
DECLARE @DashboardViewPermissionId INT;
SELECT @DashboardViewPermissionId = Id FROM Permissions WHERE Name = 'dashboard.view';

-- Kontrol: Değerler bulundu mu?
IF @StoreManagerRoleId IS NULL
BEGIN
    PRINT 'HATA: StoreManager rolü bulunamadı!';
    RETURN;
END

IF @DashboardViewPermissionId IS NULL
BEGIN
    PRINT 'HATA: dashboard.view izni bulunamadı!';
    -- İzin yoksa oluştur
    INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
    VALUES ('dashboard.view', 'Dashboard Görüntüleme', 'Dashboard modülü için Görüntüleme yetkisi', 'Dashboard', 1, 1, GETUTCDATE());
    
    SELECT @DashboardViewPermissionId = Id FROM Permissions WHERE Name = 'dashboard.view';
    PRINT 'dashboard.view izni oluşturuldu.';
END

-- Mevcut izin var mı kontrol et
IF EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @StoreManagerRoleId AND PermissionId = @DashboardViewPermissionId)
BEGIN
    PRINT 'StoreManager zaten dashboard.view iznine sahip.';
END
ELSE
BEGIN
    -- İzni ekle
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    VALUES (@StoreManagerRoleId, @DashboardViewPermissionId, GETUTCDATE());
    PRINT 'StoreManager için dashboard.view izni eklendi.';
END

-- Diğer dashboard izinlerini de ekle (statistics, revenue)
DECLARE @DashboardStatsPermissionId INT;
DECLARE @DashboardRevenuePermissionId INT;

SELECT @DashboardStatsPermissionId = Id FROM Permissions WHERE Name = 'dashboard.statistics';
SELECT @DashboardRevenuePermissionId = Id FROM Permissions WHERE Name = 'dashboard.revenue';

-- dashboard.statistics
IF @DashboardStatsPermissionId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @StoreManagerRoleId AND PermissionId = @DashboardStatsPermissionId)
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    VALUES (@StoreManagerRoleId, @DashboardStatsPermissionId, GETUTCDATE());
    PRINT 'StoreManager için dashboard.statistics izni eklendi.';
END

-- dashboard.revenue
IF @DashboardRevenuePermissionId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @StoreManagerRoleId AND PermissionId = @DashboardRevenuePermissionId)
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    VALUES (@StoreManagerRoleId, @DashboardRevenuePermissionId, GETUTCDATE());
    PRINT 'StoreManager için dashboard.revenue izni eklendi.';
END

-- Doğrulama
PRINT '';
PRINT '===== DOĞRULAMA =====';
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    p.DisplayName
FROM RolePermissions rp
INNER JOIN AspNetRoles r ON CAST(r.Id AS INT) = rp.RoleId
INNER JOIN Permissions p ON p.Id = rp.PermissionId
WHERE r.Name = 'StoreManager' AND p.Module = 'Dashboard'
ORDER BY p.Name;

PRINT 'Script tamamlandı!';
GO
