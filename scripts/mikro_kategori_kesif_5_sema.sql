-- Mikro grup tabloları şema keşfi (SADECE OKUMA) - geri yazma tasarımı için
-- Bu sorgular veri değiştirmez. Çıktıları paylaşın.

-- 1) Kolonlar: zorunlu mu (IS_NULLABLE), varsayılan değer, tip
SELECT TABLE_NAME, ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('STOK_ANA_GRUPLARI', 'STOK_ALT_GRUPLARI')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO

-- 2) Identity / hesaplanan kolonlar
SELECT OBJECT_NAME(object_id) AS tablo, name AS kolon, is_identity, is_computed
FROM sys.columns
WHERE object_id IN (OBJECT_ID('STOK_ANA_GRUPLARI'), OBJECT_ID('STOK_ALT_GRUPLARI'))
  AND (is_identity = 1 OR is_computed = 1);
GO

-- 3) Tetikleyiciler (trigger) var mı? (Mikro kayıt sırasında checksum/RECno vb. işler yapıyor olabilir)
SELECT OBJECT_NAME(parent_id) AS tablo, name AS trigger_adi, is_disabled
FROM sys.triggers
WHERE parent_id IN (OBJECT_ID('STOK_ANA_GRUPLARI'), OBJECT_ID('STOK_ALT_GRUPLARI'));
GO

-- 4) Kısıtlar (PK / UNIQUE / FK / CHECK)
SELECT OBJECT_NAME(parent_object_id) AS tablo, name, type_desc
FROM sys.objects
WHERE type IN ('PK', 'UQ', 'F', 'C')
  AND parent_object_id IN (OBJECT_ID('STOK_ANA_GRUPLARI'), OBJECT_ID('STOK_ALT_GRUPLARI'));
GO

-- 5) Bir ANA grup ve bir ALT grup kaydının tüm kolonları (şablon kayıt örneği)
SELECT TOP 3 * FROM STOK_ANA_GRUPLARI WHERE san_kod IN ('100', '500') ORDER BY san_kod;
SELECT TOP 3 * FROM STOK_ALT_GRUPLARI WHERE sta_ana_grup_kod = '100' ORDER BY sta_kod;
GO

-- 6) Mikro'da kayıt oluştururken kullanılan kullanıcı numaraları (create_user dağılımı)
SELECT san_create_user, COUNT(*) AS adet FROM STOK_ANA_GRUPLARI GROUP BY san_create_user;
SELECT sta_create_user, COUNT(*) AS adet FROM STOK_ALT_GRUPLARI GROUP BY sta_create_user;
GO

-- 7) En yüksek yeni düzen kodları (sıradaki kodu hesaplamak için)
SELECT TOP 5 san_kod FROM STOK_ANA_GRUPLARI WHERE LEN(san_kod) >= 3 AND ISNUMERIC(san_kod) = 1 ORDER BY CAST(san_kod AS int) DESC;
SELECT TOP 5 sta_kod FROM STOK_ALT_GRUPLARI WHERE LEN(sta_kod) >= 3 AND ISNUMERIC(sta_kod) = 1 ORDER BY CAST(sta_kod AS int) DESC;
GO

