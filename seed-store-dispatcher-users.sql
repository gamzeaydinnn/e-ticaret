-- ============================================================
-- STORE ATTENDANT VE DISPATCHER ROLLERI İÇİN SEED SCRIPT
-- ============================================================
-- Bu script, StoreAttendant ve Dispatcher rollerini ve test
-- kullanıcılarını veritabanına ekler.
-- Çalıştırma: SSMS veya sqlcmd ile ECommerceDb üzerinde çalıştırın
-- ============================================================

USE ECommerceDb;
GO

-- ============================================================
-- 1. ROLLERI EKLE (EĞER YOKSA)
-- ============================================================
PRINT '📦 Roller kontrol ediliyor...';

-- StoreAttendant rolü
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE [Name] = 'StoreAttendant')
BEGIN
    INSERT INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp)
    VALUES ('StoreAttendant', 'STOREATTENDANT', NEWID());
    PRINT '✅ StoreAttendant rolü eklendi';
END
ELSE
BEGIN
    PRINT 'ℹ️ StoreAttendant rolü zaten mevcut';
END

-- Dispatcher rolü
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE [Name] = 'Dispatcher')
BEGIN
    INSERT INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp)
    VALUES ('Dispatcher', 'DISPATCHER', NEWID());
    PRINT '✅ Dispatcher rolü eklendi';
END
ELSE
BEGIN
    PRINT 'ℹ️ Dispatcher rolü zaten mevcut';
END

GO

-- ============================================================
-- 2. TEST KULLANICILARI EKLE (EĞER YOKSA)
-- ============================================================
-- Şifre: Test123!
-- Password Hash: ASP.NET Core Identity tarafından üretilmiş
-- NOT: Gerçek ortamda bu hash değeri farklı olacaktır!
-- Bu değerler sadece geliştirme ortamı içindir.
-- ============================================================

PRINT '👤 Test kullanıcıları kontrol ediliyor...';

-- StoreAttendant test kullanıcısı
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'storeattendant@test.com')
BEGIN
    DECLARE @StoreAttendantId INT;
    
    INSERT INTO AspNetUsers (
        UserName, 
        NormalizedUserName, 
        Email, 
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnabled,
        AccessFailedCount,
        FirstName,
        LastName,
        FullName,
        IsActive,
        Role,
        CreatedAt
    )
    VALUES (
        'storeattendant@test.com',
        'STOREATTENDANT@TEST.COM',
        'storeattendant@test.com',
        'STOREATTENDANT@TEST.COM',
        1, -- EmailConfirmed = true
        'AQAAAAIAAYagAAAAELtUQPEsHMvMnPmCe6tPe9XpX9hKXmF1wC5H5bGmjwrPw/DM5zhB8M7ZKGP9IOAKTA==', -- Test123!
        NEWID(),
        NEWID(),
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Market',
        'Görevlisi',
        'Market Görevlisi',
        1, -- IsActive
        'StoreAttendant',
        GETUTCDATE()
    );
    
    SET @StoreAttendantId = SCOPE_IDENTITY();
    
    -- Rolü ata
    DECLARE @StoreAttendantRoleId INT;
    SELECT @StoreAttendantRoleId = Id FROM AspNetRoles WHERE [Name] = 'StoreAttendant';
    
    IF @StoreAttendantRoleId IS NOT NULL
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@StoreAttendantId, @StoreAttendantRoleId);
    END
    
    PRINT '✅ storeattendant@test.com kullanıcısı oluşturuldu';
END
ELSE
BEGIN
    PRINT 'ℹ️ storeattendant@test.com kullanıcısı zaten mevcut';
END

-- Dispatcher test kullanıcısı
IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = 'dispatcher@test.com')
BEGIN
    DECLARE @DispatcherId INT;
    
    INSERT INTO AspNetUsers (
        UserName, 
        NormalizedUserName, 
        Email, 
        NormalizedEmail,
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnabled,
        AccessFailedCount,
        FirstName,
        LastName,
        FullName,
        IsActive,
        Role,
        CreatedAt
    )
    VALUES (
        'dispatcher@test.com',
        'DISPATCHER@TEST.COM',
        'dispatcher@test.com',
        'DISPATCHER@TEST.COM',
        1, -- EmailConfirmed = true
        'AQAAAAIAAYagAAAAELtUQPEsHMvMnPmCe6tPe9XpX9hKXmF1wC5H5bGmjwrPw/DM5zhB8M7ZKGP9IOAKTA==', -- Test123!
        NEWID(),
        NEWID(),
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Sevkiyat',
        'Görevlisi',
        'Sevkiyat Görevlisi',
        1, -- IsActive
        'Dispatcher',
        GETUTCDATE()
    );
    
    SET @DispatcherId = SCOPE_IDENTITY();
    
    -- Rolü ata
    DECLARE @DispatcherRoleId INT;
    SELECT @DispatcherRoleId = Id FROM AspNetRoles WHERE [Name] = 'Dispatcher';
    
    IF @DispatcherRoleId IS NOT NULL
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@DispatcherId, @DispatcherRoleId);
    END
    
    PRINT '✅ dispatcher@test.com kullanıcısı oluşturuldu';
END
ELSE
BEGIN
    PRINT 'ℹ️ dispatcher@test.com kullanıcısı zaten mevcut';
END

GO

-- ============================================================
-- 3. SONUÇ KONTROLÜ
-- ============================================================
PRINT '';
PRINT '📊 SONUÇ RAPORU:';
PRINT '================';

SELECT 
    'Roller' AS Kategori,
    COUNT(*) AS Toplam
FROM AspNetRoles
WHERE Name IN ('StoreAttendant', 'Dispatcher')
UNION ALL
SELECT 
    'Test Kullanıcıları' AS Kategori,
    COUNT(*) AS Toplam
FROM AspNetUsers
WHERE Email IN ('storeattendant@test.com', 'dispatcher@test.com');

PRINT '';
PRINT '✅ Seed script tamamlandı!';
PRINT '';
PRINT '📝 Test Giriş Bilgileri:';
PRINT '   • storeattendant@test.com / Test123!';
PRINT '   • dispatcher@test.com / Test123!';

GO
