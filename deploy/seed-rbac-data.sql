-- ============================================================
-- RBAC Sistemi Manuel Seed Script
-- ============================================================
-- Bu script sadece IdentitySeeder çalışmazsa FALLBACK olarak kullanılmalıdır.
-- Normal şartlarda Program.cs içindeki IdentitySeeder.SeedAsync() otomatik çalışır.
--
-- Kullanım:
-- docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
--   -S localhost -U sa -P 'ECom1234' -C < seed-rbac-data.sql
-- ============================================================

USE [ECommerceDb];
GO

-- ============================================================
-- ADIM 1: Permissions Seed
-- ============================================================
PRINT 'Seeding Permissions...';

-- Permission tablosunu temizle (sadece yeniden seed için)
-- DELETE FROM RolePermissions; -- Foreign key nedeniyle önce bu
-- DELETE FROM Permissions;

-- Dashboard İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('dashboard.view', 'Dashboard Görüntüleme', 'Dashboard modülü için Görüntüleme yetkisi', 'Dashboard', 1, 1, GETUTCDATE()),
('dashboard.statistics', 'Dashboard İstatistikler', 'Dashboard modülü için İstatistikler yetkisi', 'Dashboard', 2, 1, GETUTCDATE()),
('dashboard.revenue', 'Dashboard Gelir', 'Dashboard modülü için Gelir yetkisi', 'Dashboard', 3, 1, GETUTCDATE());

-- Ürün İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('products.view', 'Ürün Görüntüleme', 'Products modülü için Görüntüleme yetkisi', 'Products', 4, 1, GETUTCDATE()),
('products.create', 'Ürün Oluşturma', 'Products modülü için Oluşturma yetkisi', 'Products', 5, 1, GETUTCDATE()),
('products.update', 'Ürün Güncelleme', 'Products modülü için Güncelleme yetkisi', 'Products', 6, 1, GETUTCDATE()),
('products.delete', 'Ürün Silme', 'Products modülü için Silme yetkisi', 'Products', 7, 1, GETUTCDATE()),
('products.stock', 'Ürün Stok Yönetimi', 'Products modülü için Yönetimi yetkisi', 'Products', 8, 1, GETUTCDATE()),
('products.pricing', 'Ürün Fiyat Yönetimi', 'Products modülü için Yönetimi yetkisi', 'Products', 9, 1, GETUTCDATE()),
('products.import', 'Ürün İçe Aktarma', 'Products modülü için Aktarma yetkisi', 'Products', 10, 1, GETUTCDATE()),
('products.export', 'Ürün Dışa Aktarma', 'Products modülü için Aktarma yetkisi', 'Products', 11, 1, GETUTCDATE());

-- Kategori İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('categories.view', 'Kategori Görüntüleme', 'Categories modülü için Görüntüleme yetkisi', 'Categories', 12, 1, GETUTCDATE()),
('categories.create', 'Kategori Oluşturma', 'Categories modülü için Oluşturma yetkisi', 'Categories', 13, 1, GETUTCDATE()),
('categories.update', 'Kategori Güncelleme', 'Categories modülü için Güncelleme yetkisi', 'Categories', 14, 1, GETUTCDATE()),
('categories.delete', 'Kategori Silme', 'Categories modülü için Silme yetkisi', 'Categories', 15, 1, GETUTCDATE());

-- Sipariş İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('orders.view', 'Sipariş Görüntüleme', 'Orders modülü için Görüntüleme yetkisi', 'Orders', 16, 1, GETUTCDATE()),
('orders.details', 'Sipariş Detay', 'Orders modülü için Detay yetkisi', 'Orders', 17, 1, GETUTCDATE()),
('orders.status', 'Sipariş Durum Güncelleme', 'Orders modülü için Güncelleme yetkisi', 'Orders', 18, 1, GETUTCDATE()),
('orders.cancel', 'Sipariş İptal Etme', 'Orders modülü için Etme yetkisi', 'Orders', 19, 1, GETUTCDATE()),
('orders.refund', 'Sipariş İade İşlemi', 'Orders modülü için İşlemi yetkisi', 'Orders', 20, 1, GETUTCDATE()),
('orders.assign_courier', 'Sipariş Kurye Atama', 'Orders modülü için Atama yetkisi', 'Orders', 21, 1, GETUTCDATE()),
('orders.customer_info', 'Sipariş Müşteri Bilgisi', 'Orders modülü için Bilgisi yetkisi', 'Orders', 22, 1, GETUTCDATE()),
('orders.export', 'Sipariş Dışa Aktarma', 'Orders modülü için Aktarma yetkisi', 'Orders', 23, 1, GETUTCDATE());

-- Kullanıcı İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('users.view', 'Kullanıcı Görüntüleme', 'Users modülü için Görüntüleme yetkisi', 'Users', 24, 1, GETUTCDATE()),
('users.create', 'Kullanıcı Oluşturma', 'Users modülü için Oluşturma yetkisi', 'Users', 25, 1, GETUTCDATE()),
('users.update', 'Kullanıcı Güncelleme', 'Users modülü için Güncelleme yetkisi', 'Users', 26, 1, GETUTCDATE()),
('users.delete', 'Kullanıcı Silme', 'Users modülü için Silme yetkisi', 'Users', 27, 1, GETUTCDATE()),
('users.roles', 'Kullanıcı Rol Atama', 'Users modülü için Atama yetkisi', 'Users', 28, 1, GETUTCDATE()),
('users.sensitive', 'Kullanıcı Hassas Veri Erişimi', 'Users modülü için Erişimi yetkisi', 'Users', 29, 1, GETUTCDATE()),
('users.export', 'Kullanıcı Dışa Aktarma', 'Users modülü için Aktarma yetkisi', 'Users', 30, 1, GETUTCDATE());

-- Rol İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('roles.view', 'Rol Görüntüleme', 'Roles modülü için Görüntüleme yetkisi', 'Roles', 31, 1, GETUTCDATE()),
('roles.create', 'Rol Oluşturma', 'Roles modülü için Oluşturma yetkisi', 'Roles', 32, 1, GETUTCDATE()),
('roles.update', 'Rol Güncelleme', 'Roles modülü için Güncelleme yetkisi', 'Roles', 33, 1, GETUTCDATE()),
('roles.delete', 'Rol Silme', 'Roles modülü için Silme yetkisi', 'Roles', 34, 1, GETUTCDATE()),
('roles.permissions', 'Rol İzin Yönetimi', 'Roles modülü için Yönetimi yetkisi', 'Roles', 35, 1, GETUTCDATE());

-- Kampanya İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('campaigns.view', 'Kampanya Görüntüleme', 'Campaigns modülü için Görüntüleme yetkisi', 'Campaigns', 36, 1, GETUTCDATE()),
('campaigns.create', 'Kampanya Oluşturma', 'Campaigns modülü için Oluşturma yetkisi', 'Campaigns', 37, 1, GETUTCDATE()),
('campaigns.update', 'Kampanya Güncelleme', 'Campaigns modülü için Güncelleme yetkisi', 'Campaigns', 38, 1, GETUTCDATE()),
('campaigns.delete', 'Kampanya Silme', 'Campaigns modülü için Silme yetkisi', 'Campaigns', 39, 1, GETUTCDATE());

-- Kupon İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('coupons.view', 'Kupon Görüntüleme', 'Coupons modülü için Görüntüleme yetkisi', 'Coupons', 40, 1, GETUTCDATE()),
('coupons.create', 'Kupon Oluşturma', 'Coupons modülü için Oluşturma yetkisi', 'Coupons', 41, 1, GETUTCDATE()),
('coupons.update', 'Kupon Güncelleme', 'Coupons modülü için Güncelleme yetkisi', 'Coupons', 42, 1, GETUTCDATE()),
('coupons.delete', 'Kupon Silme', 'Coupons modülü için Silme yetkisi', 'Coupons', 43, 1, GETUTCDATE());

-- Kurye İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('couriers.view', 'Kurye Görüntüleme', 'Couriers modülü için Görüntüleme yetkisi', 'Couriers', 44, 1, GETUTCDATE()),
('couriers.create', 'Kurye Oluşturma', 'Couriers modülü için Oluşturma yetkisi', 'Couriers', 45, 1, GETUTCDATE()),
('couriers.update', 'Kurye Güncelleme', 'Couriers modülü için Güncelleme yetkisi', 'Couriers', 46, 1, GETUTCDATE()),
('couriers.delete', 'Kurye Silme', 'Couriers modülü için Silme yetkisi', 'Couriers', 47, 1, GETUTCDATE()),
('couriers.assign', 'Kurye Atama', 'Couriers modülü için Atama yetkisi', 'Couriers', 48, 1, GETUTCDATE()),
('couriers.performance', 'Kurye Performans', 'Couriers modülü için Performans yetkisi', 'Couriers', 49, 1, GETUTCDATE());

-- Kargo İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('shipping.pending', 'Kargo Bekleyenler', 'Shipping modülü için Bekleyenler yetkisi', 'Shipping', 50, 1, GETUTCDATE()),
('shipping.tracking', 'Kargo Takip Numarası', 'Shipping modülü için Numarası yetkisi', 'Shipping', 51, 1, GETUTCDATE()),
('shipping.ship', 'Kargo Kargoya Ver', 'Shipping modülü için Ver yetkisi', 'Shipping', 52, 1, GETUTCDATE()),
('shipping.deliver', 'Kargo Teslim Et', 'Shipping modülü için Et yetkisi', 'Shipping', 53, 1, GETUTCDATE());

-- Rapor İzinleri
-- GÜNCELLEME: reports.view genel izni eklendi
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('reports.view', 'Rapor Görüntüleme', 'Reports modülü için genel Görüntüleme yetkisi', 'Reports', 53, 1, GETUTCDATE()),
('reports.sales', 'Rapor Satış', 'Reports modülü için Satış yetkisi', 'Reports', 54, 1, GETUTCDATE()),
('reports.inventory', 'Rapor Envanter', 'Reports modülü için Envanter yetkisi', 'Reports', 55, 1, GETUTCDATE()),
('reports.customers', 'Rapor Müşteriler', 'Reports modülü için Müşteriler yetkisi', 'Reports', 56, 1, GETUTCDATE()),
('reports.financial', 'Rapor Finansal', 'Reports modülü için Finansal yetkisi', 'Reports', 57, 1, GETUTCDATE()),
('reports.weight', 'Rapor Ağırlık', 'Reports modülü için Ağırlık yetkisi', 'Reports', 58, 1, GETUTCDATE()),
('reports.export', 'Rapor Dışa Aktarma', 'Reports modülü için Aktarma yetkisi', 'Reports', 59, 1, GETUTCDATE());

-- Banner İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('banners.view', 'Banner Görüntüleme', 'Banners modülü için Görüntüleme yetkisi', 'Banners', 60, 1, GETUTCDATE()),
('banners.create', 'Banner Oluşturma', 'Banners modülü için Oluşturma yetkisi', 'Banners', 61, 1, GETUTCDATE()),
('banners.update', 'Banner Güncelleme', 'Banners modülü için Güncelleme yetkisi', 'Banners', 62, 1, GETUTCDATE()),
('banners.delete', 'Banner Silme', 'Banners modülü için Silme yetkisi', 'Banners', 63, 1, GETUTCDATE());

-- Marka İzinleri
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('brands.view', 'Marka Görüntüleme', 'Brands modülü için Görüntüleme yetkisi', 'Brands', 64, 1, GETUTCDATE()),
('brands.create', 'Marka Oluşturma', 'Brands modülü için Oluşturma yetkisi', 'Brands', 65, 1, GETUTCDATE()),
('brands.update', 'Marka Güncelleme', 'Brands modülü için Güncelleme yetkisi', 'Brands', 66, 1, GETUTCDATE()),
('brands.delete', 'Marka Silme', 'Brands modülü için Silme yetkisi', 'Brands', 67, 1, GETUTCDATE());

-- Ayar İzinleri
-- GÜNCELLEME: settings.system izni eklendi
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('settings.view', 'Ayar Görüntüleme', 'Settings modülü için Görüntüleme yetkisi', 'Settings', 68, 1, GETUTCDATE()),
('settings.update', 'Ayar Güncelleme', 'Settings modülü için Güncelleme yetkisi', 'Settings', 69, 1, GETUTCDATE()),
('settings.system', 'Sistem Ayarları', 'Settings modülü için Sistem yetkisi', 'Settings', 70, 1, GETUTCDATE()),
('settings.payment', 'Ayar Ödeme', 'Settings modülü için Ödeme yetkisi', 'Settings', 71, 1, GETUTCDATE()),
('settings.sms', 'Ayar SMS', 'Settings modülü için SMS yetkisi', 'Settings', 72, 1, GETUTCDATE()),
('settings.email', 'Ayar E-posta', 'Settings modülü için E-posta yetkisi', 'Settings', 73, 1, GETUTCDATE());

-- Log İzinleri
-- GÜNCELLEME: logs.view genel izni eklendi
INSERT INTO Permissions (Name, DisplayName, Description, Module, SortOrder, IsActive, CreatedAt)
VALUES
('logs.view', 'Log Görüntüleme', 'Logs modülü için genel Görüntüleme yetkisi', 'Logs', 74, 1, GETUTCDATE()),
('logs.audit', 'Log Denetim', 'Logs modülü için Denetim yetkisi', 'Logs', 75, 1, GETUTCDATE()),
('logs.error', 'Log Hata', 'Logs modülü için Hata yetkisi', 'Logs', 76, 1, GETUTCDATE()),
('logs.sync', 'Log Senkronizasyon', 'Logs modülü için Senkronizasyon yetkisi', 'Logs', 77, 1, GETUTCDATE()),
('logs.export', 'Log Dışa Aktarma', 'Logs modülü için Aktarma yetkisi', 'Logs', 78, 1, GETUTCDATE());

PRINT 'Permissions seeded successfully!';
GO

-- ============================================================
-- ADIM 2: RolePermissions Seed (Rol-İzin Eşlemeleri)
-- ============================================================
PRINT 'Seeding RolePermissions...';

-- Değişkenler: Rol ID'lerini dinamik al
DECLARE @SuperAdminRoleId INT;
DECLARE @StoreManagerRoleId INT;
DECLARE @CustomerSupportRoleId INT;
DECLARE @LogisticsRoleId INT;

-- Rollerin ID'lerini al (AspNetRoles tablosundan)
SELECT @SuperAdminRoleId = Id FROM AspNetRoles WHERE Name = 'SuperAdmin';
SELECT @StoreManagerRoleId = Id FROM AspNetRoles WHERE Name = 'StoreManager';
SELECT @CustomerSupportRoleId = Id FROM AspNetRoles WHERE Name = 'CustomerSupport';
SELECT @LogisticsRoleId = Id FROM AspNetRoles WHERE Name = 'Logistics';

-- ============================================================
-- SuperAdmin: TÜM İZİNLER
-- ============================================================
IF @SuperAdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    SELECT @SuperAdminRoleId, Id, GETUTCDATE()
    FROM Permissions
    WHERE IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @SuperAdminRoleId AND PermissionId = Permissions.Id
    );
    PRINT 'SuperAdmin permissions assigned.';
END
ELSE
BEGIN
    PRINT 'WARNING: SuperAdmin role not found!';
END

-- ============================================================
-- StoreManager: Ürün, Kategori, Kampanya, Stok Yönetimi
-- GÜNCELLEME: users.view, couriers.view ve reports.view izinleri eklendi
-- ============================================================
IF @StoreManagerRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    SELECT @StoreManagerRoleId, Id, GETUTCDATE()
    FROM Permissions
    WHERE IsActive = 1
    AND Name IN (
        -- Dashboard
        'dashboard.view', 'dashboard.statistics', 'dashboard.revenue',
        -- Products - Tam yetki
        'products.view', 'products.create', 'products.update', 'products.delete',
        'products.stock', 'products.pricing', 'products.import', 'products.export',
        -- Categories - Tam yetki
        'categories.view', 'categories.create', 'categories.update', 'categories.delete',
        -- Orders - Görüntüleme ve güncelleme
        'orders.view', 'orders.details', 'orders.status', 'orders.customer_info', 'orders.export',
        -- Campaigns
        'campaigns.view', 'campaigns.create', 'campaigns.update', 'campaigns.delete',
        -- Coupons
        'coupons.view', 'coupons.create', 'coupons.update', 'coupons.delete',
        -- Brands
        'brands.view', 'brands.create', 'brands.update', 'brands.delete',
        -- Banners
        'banners.view', 'banners.create', 'banners.update', 'banners.delete',
        -- Reports - Genel görüntüleme dahil (YENİ)
        'reports.view', 'reports.sales', 'reports.inventory', 'reports.export',
        -- Users - Sadece görüntüleme (YENİ - Madde 4.1)
        'users.view',
        -- Couriers - Görüntüleme (YENİ - Madde 4.2)
        'couriers.view'
    )
    AND NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @StoreManagerRoleId AND PermissionId = Permissions.Id
    );
    PRINT 'StoreManager permissions assigned.';
END
ELSE
BEGIN
    PRINT 'WARNING: StoreManager role not found!';
END

-- ============================================================
-- CustomerSupport: Sipariş Yönetimi, Müşteri İletişimi
-- GÜNCELLEME: reports.view izni eklendi
-- ============================================================
IF @CustomerSupportRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    SELECT @CustomerSupportRoleId, Id, GETUTCDATE()
    FROM Permissions
    WHERE IsActive = 1
    AND Name IN (
        -- Dashboard - Sadece görüntüleme
        'dashboard.view',
        -- Products - Sadece görüntüleme
        'products.view',
        -- Categories - Sadece görüntüleme
        'categories.view',
        -- Orders - Tam yetki (iptal/iade dahil)
        'orders.view', 'orders.details', 'orders.status', 'orders.cancel', 
        'orders.refund', 'orders.customer_info',
        -- Users - Sadece görüntüleme
        'users.view',
        -- Reports - Görüntüleme (YENİ - Madde 4.3)
        'reports.view', 'reports.sales'
    )
    AND NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @CustomerSupportRoleId AND PermissionId = Permissions.Id
    );
    PRINT 'CustomerSupport permissions assigned.';
END
ELSE
BEGIN
    PRINT 'WARNING: CustomerSupport role not found!';
END

-- ============================================================
-- Logistics: Kargo ve Teslimat Operasyonları
-- GÜNCELLEME: reports.view ve reports.weight izinleri eklendi
-- ============================================================
IF @LogisticsRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt)
    SELECT @LogisticsRoleId, Id, GETUTCDATE()
    FROM Permissions
    WHERE IsActive = 1
    AND Name IN (
        -- Dashboard - Sadece görüntüleme
        'dashboard.view',
        -- Orders - Sınırlı erişim (müşteri bilgisi YOK)
        'orders.view', 'orders.status',
        -- Shipping - Tam yetki
        'shipping.pending', 'shipping.tracking', 'shipping.ship', 'shipping.deliver',
        -- Couriers - Görüntüleme ve atama
        'couriers.view', 'couriers.assign',
        -- Reports - Genel görüntüleme ve ağırlık raporları (YENİ)
        'reports.view', 'reports.weight'
    )
    AND NOT EXISTS (
        SELECT 1 FROM RolePermissions 
        WHERE RoleId = @LogisticsRoleId AND PermissionId = Permissions.Id
    );
    PRINT 'Logistics permissions assigned.';
END
ELSE
BEGIN
    PRINT 'WARNING: Logistics role not found!';
END

PRINT 'RolePermissions seeded successfully!';
GO

-- ============================================================
-- ADIM 3: Doğrulama Sorguları
-- ============================================================
PRINT '===== VERIFICATION QUERIES =====';

-- Permission sayısı
SELECT COUNT(*) AS TotalPermissions FROM Permissions WHERE IsActive = 1;

-- Rol başına izin sayısı
SELECT 
    r.Name AS RoleName,
    COUNT(rp.Id) AS PermissionCount
FROM AspNetRoles r
LEFT JOIN RolePermissions rp ON CAST(r.Id AS INT) = rp.RoleId
GROUP BY r.Name
ORDER BY r.Name;

-- En son eklenen izinler (son 10)
SELECT TOP 10 Name, DisplayName, Module, CreatedAt 
FROM Permissions 
ORDER BY CreatedAt DESC;

PRINT 'Seed script completed successfully!';
GO
