-- ============================================================
-- Admin Kullanıcı Temizleme ve Yeniden Oluşturma Script
-- ============================================================
-- Bu script eski admin kullanıcıyı siler ve IdentitySeeder'ın
-- yeni admin kullanıcıyı (admin@admin.com) oluşturmasını sağlar
-- ============================================================

USE [ECommerceDb];
GO

PRINT '🧹 Eski admin kullanıcıları temizleniyor...';

-- Eski admin email'leri
DECLARE @OldAdminEmails TABLE (Email NVARCHAR(256));
INSERT INTO @OldAdminEmails VALUES ('admin@local'), ('admin@admin.com');

-- Admin kullanıcılarının ID'lerini al
DECLARE @AdminUserIds TABLE (UserId INT);
INSERT INTO @AdminUserIds
SELECT Id FROM Users WHERE Email IN (SELECT Email FROM @OldAdminEmails);

-- 1. RefreshTokens tablosundan admin'in token'larını sil
DELETE FROM RefreshTokens WHERE UserId IN (SELECT UserId FROM @AdminUserIds);
PRINT '✅ RefreshTokens temizlendi';

-- 2. AspNetUserRoles tablosundan admin rol atamalarını sil
DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT UserId FROM @AdminUserIds);
PRINT '✅ AspNetUserRoles temizlendi';

-- 3. Users tablosundan admin kullanıcılarını sil
DELETE FROM Users WHERE Id IN (SELECT UserId FROM @AdminUserIds);
PRINT '✅ Users tablosundan admin kullanıcıları silindi';

-- 4. AspNetUsers tablosundan da sil (Identity tablosu)
DELETE FROM AspNetUsers WHERE Email IN (SELECT Email FROM @OldAdminEmails);
PRINT '✅ AspNetUsers temizlendi';

PRINT '';
PRINT '✅✅✅ Temizleme tamamlandı! ✅✅✅';
PRINT 'Şimdi backend\'i yeniden başlatın, IdentitySeeder yeni admin kullanıcıyı oluşturacak.';
PRINT '';
PRINT 'Yeni Admin Bilgileri:';
PRINT '  Email: admin@admin.com';
PRINT '  Password: admin123';
GO
