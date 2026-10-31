-- Mikro kategori keşfi: SADECE OKUMA (SELECT). Veri değiştirmez.
-- Her blok (GO ile ayrılmış) bağımsız çalışır; biri hata verse diğerleri çalışır.
-- Tümünü seçip F5 ile çalıştırın, "Results" sekmelerindeki çıktıları paylaşın.

-- 1) Ana gruplar
SELECT * FROM STOK_ANA_GRUPLARI ORDER BY 1;
GO

-- 2) Alt gruplar
SELECT * FROM STOK_ALT_GRUPLARI ORDER BY 1;
GO

-- 3) Kategoriler
SELECT * FROM STOK_KATEGORILERI ORDER BY 1;
GO

-- 4) Reyonlar
SELECT * FROM STOK_REYONLARI ORDER BY 1;
GO

-- 5) Sektörler
SELECT * FROM STOK_SEKTORLERI ORDER BY 1;
GO

-- 6) Ürünlerde hangi grup kolonu dolu, kaç ürün var
SELECT sto_anagrup_kod, sto_altgrup_kod, sto_kategori_kodu, COUNT(*) AS adet
FROM STOKLAR
WHERE sto_pasif_fl = 0
GROUP BY sto_anagrup_kod, sto_altgrup_kod, sto_kategori_kodu
ORDER BY 1, 2;
GO

-- 7) Gerçek kolon adları
SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('STOK_ANA_GRUPLARI','STOK_ALT_GRUPLARI','STOK_KATEGORILERI','STOK_REYONLARI','STOK_SEKTORLERI')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
GO
