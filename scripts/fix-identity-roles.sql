-- ============================================================
-- Identity Role Ataması Düzeltme Script'i
-- ============================================================
-- Bu script, User.Role property'si set edilmiş ama AspNetUserRoles
-- tablosuna eklenmemiş kullanıcıları düzeltir.
--
-- SORUN: user.Role = "StoreManager" yapılmış ama
--        _userManager.AddToRoleAsync() çağrılmamış
--        Bu durumda [Authorize(Roles=...)] ve PermissionService çalışmaz
--
-- ÇÖZÜM: AspNetUserRoles tablosuna eksik rol atamalarını ekle
-- ============================================================

USE [ECommerceDb];
GO

-- SET options (QUOTED_IDENTIFIER hatası için)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
GO

PRINT '=== Identity Role Atamasi Duzeltme Scripti ===';
PRINT '';

-- ============================================================
-- ADIM 1: Mevcut durumu analiz et
-- ============================================================
PRINT 'ADIM 1: Mevcut durum analizi...';

-- User.Role değeri olan ama AspNetUserRoles'da kaydı olmayan kullanıcılar
SELECT 
    u.Id AS UserId,
    u.Email,
    u.Role AS UserRoleProperty,
    ur.RoleId AS IdentityRoleId,
    r.Name AS IdentityRoleName,
    CASE 
        WHEN ur.RoleId IS NULL THEN 'EKSIK - DUZELTILECEK'
        WHEN u.Role != r.Name THEN 'UYUMSUZ - KONTROL EDILMELI'
        ELSE 'OK'
    END AS Durum
FROM Users u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Role IS NOT NULL AND u.Role != ''
ORDER BY 
    CASE WHEN ur.RoleId IS NULL THEN 0 ELSE 1 END,
    u.Email;

PRINT '';

-- ============================================================
-- ADIM 2: Eksik rolleri AspNetUserRoles'a ekle
-- ============================================================
PRINT 'ADIM 2: Eksik Identity rol atamalarini duzeltme...';

-- Her bir rol için ayrı ayrı işle
DECLARE @TargetRole NVARCHAR(50);
DECLARE @RoleId INT;
DECLARE @AffectedCount INT;

-- Tüm rolleri dolaş
DECLARE RoleCursor CURSOR FOR
SELECT DISTINCT u.Role
FROM Users u
WHERE u.Role IS NOT NULL 
  AND u.Role != ''
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id
  );

OPEN RoleCursor;
FETCH NEXT FROM RoleCursor INTO @TargetRole;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Bu rolün AspNetRoles'daki ID'sini bul
    SELECT @RoleId = Id FROM AspNetRoles WHERE Name = @TargetRole;
    
    IF @RoleId IS NOT NULL
    BEGIN
        -- Bu role ait olması gereken ama AspNetUserRoles'da olmayan kullanıcıları ekle
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        SELECT u.Id, @RoleId
        FROM Users u
        WHERE u.Role = @TargetRole
          AND NOT EXISTS (
              SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id
          );
        
        SET @AffectedCount = @@ROWCOUNT;
        PRINT 'Rol: ' + @TargetRole + ' - ' + CAST(@AffectedCount AS VARCHAR(10)) + ' kullaniciya Identity rolu atandi.';
    END
    ELSE
    BEGIN
        -- Rol AspNetRoles'da yok, önce oluştur
        PRINT 'UYARI: "' + @TargetRole + '" rolu AspNetRoles''da bulunamadi. Olusturuluyor...';
        
        INSERT INTO AspNetRoles (Name, NormalizedName, ConcurrencyStamp)
        VALUES (@TargetRole, UPPER(@TargetRole), NEWID());
        
        SELECT @RoleId = Id FROM AspNetRoles WHERE Name = @TargetRole;
        
        -- Şimdi kullanıcıları ekle
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        SELECT u.Id, @RoleId
        FROM Users u
        WHERE u.Role = @TargetRole
          AND NOT EXISTS (
              SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id
          );
        
        SET @AffectedCount = @@ROWCOUNT;
        PRINT 'Rol: ' + @TargetRole + ' olusturuldu ve ' + CAST(@AffectedCount AS VARCHAR(10)) + ' kullaniciya atandi.';
    END
    
    FETCH NEXT FROM RoleCursor INTO @TargetRole;
END

CLOSE RoleCursor;
DEALLOCATE RoleCursor;

PRINT '';

-- ============================================================
-- ADIM 3: Düzeltme sonrası doğrulama
-- ============================================================
PRINT 'ADIM 3: Duzeltme sonrasi dogrulama...';

-- Artık tüm kullanıcılar Identity rolüne sahip olmalı
SELECT 
    u.Id AS UserId,
    u.Email,
    u.Role AS UserRoleProperty,
    r.Name AS IdentityRoleName,
    CASE 
        WHEN r.Name = u.Role THEN 'TUTARLI'
        ELSE 'UYUMSUZ'
    END AS Durum
FROM Users u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.Role IS NOT NULL AND u.Role != ''
ORDER BY u.Email;

PRINT '';

-- Hala eksik olan var mı kontrol et
DECLARE @StillMissing INT;
SELECT @StillMissing = COUNT(*)
FROM Users u
WHERE u.Role IS NOT NULL 
  AND u.Role != ''
  AND NOT EXISTS (
      SELECT 1 FROM AspNetUserRoles ur WHERE ur.UserId = u.Id
  );

IF @StillMissing > 0
BEGIN
    PRINT 'UYARI: Hala ' + CAST(@StillMissing AS VARCHAR(10)) + ' kullanicinin Identity rolu eksik!';
END
ELSE
BEGIN
    PRINT 'BASARILI: Tum kullanicilarin Identity rolleri atandi.';
END

PRINT '';
PRINT '=== Script tamamlandi ===';
GO
