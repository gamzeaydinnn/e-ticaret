-- MikroProductCache tablosuna AnagrupKod kolonu ekleme
-- NEDEN: Mikro'dan gelen sto_anagrup_kod değeri cache'de saklanmıyordu.
-- Bu kolon olmadan MikroCategoryMapping ile doğru eşleme yapılamıyordu.

USE ECommerceDb;
GO

-- 1. AnagrupKod kolonu ekle (idempotent)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'MikroProductCache' AND COLUMN_NAME = 'AnagrupKod'
)
BEGIN
    ALTER TABLE MikroProductCache ADD AnagrupKod NVARCHAR(50) NULL;
    PRINT 'AnagrupKod kolonu eklendi.';
END
ELSE
BEGIN
    PRINT 'AnagrupKod kolonu zaten mevcut.';
END
GO

-- 2. AnagrupKod index'i oluştur
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_MikroProductCache_AnagrupKod' AND object_id = OBJECT_ID('MikroProductCache')
)
BEGIN
    CREATE INDEX IX_MikroProductCache_AnagrupKod ON MikroProductCache (AnagrupKod);
    PRINT 'IX_MikroProductCache_AnagrupKod index oluşturuldu.';
END
GO

-- 3. "Diğer" kategorisi ekle (idempotent)
IF NOT EXISTS (SELECT 1 FROM Categories WHERE Slug = 'diger')
BEGIN
    INSERT INTO Categories (Name, Description, ImageUrl, Slug, SortOrder, IsActive, CreatedAt, UpdatedAt)
    VALUES (N'Diğer', N'Henüz kategorize edilmemiş ürünler', NULL, 'diger', 999, 1, GETDATE(), GETDATE());
    PRINT '"Diğer" kategorisi oluşturuldu.';
END
ELSE
BEGIN
    PRINT '"Diğer" kategorisi zaten mevcut.';
END
GO

-- 4. Wildcard (*) mapping ekle (idempotent)
DECLARE @digerCategoryId INT;
SELECT @digerCategoryId = Id FROM Categories WHERE Slug = 'diger';

IF @digerCategoryId IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM MikroCategoryMappings WHERE MikroAnagrupKod = '*'
)
BEGIN
    INSERT INTO MikroCategoryMappings (MikroAnagrupKod, MikroAltgrupKod, MikroMarkaKod, CategoryId, BrandId, Priority, IsActive, MikroGrupAciklama, Notes, CreatedAt)
    VALUES (N'*', NULL, NULL, @digerCategoryId, NULL, 999, 1, N'Varsayılan eşleme — eşlenemeyen ürünler Diğer kategorisine atanır', N'Migration script', GETDATE());
    PRINT 'Wildcard (*) mapping oluşturuldu → CategoryId=' + CAST(@digerCategoryId AS VARCHAR);
END
ELSE
BEGIN
    PRINT 'Wildcard (*) mapping zaten mevcut veya Diğer kategorisi bulunamadı.';
END
GO

PRINT '✅ Migration tamamlandı.';
GO
