-- ============================================================
-- HTML ENTITY → TÜRKÇE KARAKTER DÜZELTMESİ
-- ============================================================
-- Veritabanında HTML entity olarak kaydedilmiş Türkçe karakterleri düzeltir.
-- Örnek: &#x15F; → ş, &#x131; → ı, &#xFC; → ü vb.
-- ============================================================

USE ECommerceDb;
GO

-- Products tablosundaki Name alanını düzelt
UPDATE Products SET Name = REPLACE(Name, '&#x15F;', 'ş');    -- ş
UPDATE Products SET Name = REPLACE(Name, '&#x15E;', 'Ş');    -- Ş
UPDATE Products SET Name = REPLACE(Name, '&#x131;', 'ı');    -- ı
UPDATE Products SET Name = REPLACE(Name, '&#x130;', 'İ');    -- İ
UPDATE Products SET Name = REPLACE(Name, '&#xFC;', 'ü');     -- ü
UPDATE Products SET Name = REPLACE(Name, '&#xDC;', 'Ü');     -- Ü
UPDATE Products SET Name = REPLACE(Name, '&#xF6;', 'ö');     -- ö
UPDATE Products SET Name = REPLACE(Name, '&#xD6;', 'Ö');     -- Ö
UPDATE Products SET Name = REPLACE(Name, '&#xE7;', 'ç');     -- ç
UPDATE Products SET Name = REPLACE(Name, '&#xC7;', 'Ç');     -- Ç
UPDATE Products SET Name = REPLACE(Name, '&#x11F;', 'ğ');    -- ğ
UPDATE Products SET Name = REPLACE(Name, '&#x11E;', 'Ğ');    -- Ğ

-- Decimal HTML entities de olabilir
UPDATE Products SET Name = REPLACE(Name, '&#351;', 'ş');     -- ş
UPDATE Products SET Name = REPLACE(Name, '&#350;', 'Ş');     -- Ş
UPDATE Products SET Name = REPLACE(Name, '&#305;', 'ı');     -- ı
UPDATE Products SET Name = REPLACE(Name, '&#304;', 'İ');     -- İ
UPDATE Products SET Name = REPLACE(Name, '&#252;', 'ü');     -- ü
UPDATE Products SET Name = REPLACE(Name, '&#220;', 'Ü');     -- Ü
UPDATE Products SET Name = REPLACE(Name, '&#246;', 'ö');     -- ö
UPDATE Products SET Name = REPLACE(Name, '&#214;', 'Ö');     -- Ö
UPDATE Products SET Name = REPLACE(Name, '&#231;', 'ç');     -- ç
UPDATE Products SET Name = REPLACE(Name, '&#199;', 'Ç');     -- Ç
UPDATE Products SET Name = REPLACE(Name, '&#287;', 'ğ');     -- ğ
UPDATE Products SET Name = REPLACE(Name, '&#286;', 'Ğ');     -- Ğ

-- Description alanını da düzelt
UPDATE Products SET Description = REPLACE(Description, '&#x15F;', 'ş');
UPDATE Products SET Description = REPLACE(Description, '&#x15E;', 'Ş');
UPDATE Products SET Description = REPLACE(Description, '&#x131;', 'ı');
UPDATE Products SET Description = REPLACE(Description, '&#x130;', 'İ');
UPDATE Products SET Description = REPLACE(Description, '&#xFC;', 'ü');
UPDATE Products SET Description = REPLACE(Description, '&#xDC;', 'Ü');
UPDATE Products SET Description = REPLACE(Description, '&#xF6;', 'ö');
UPDATE Products SET Description = REPLACE(Description, '&#xD6;', 'Ö');
UPDATE Products SET Description = REPLACE(Description, '&#xE7;', 'ç');
UPDATE Products SET Description = REPLACE(Description, '&#xC7;', 'Ç');
UPDATE Products SET Description = REPLACE(Description, '&#x11F;', 'ğ');
UPDATE Products SET Description = REPLACE(Description, '&#x11E;', 'Ğ');

PRINT 'Türkçe karakterler düzeltildi!';
GO
