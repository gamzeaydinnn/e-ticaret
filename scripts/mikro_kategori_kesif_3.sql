-- Mikro kategori keşfi - 3. tur (SADECE OKUMA)
-- Amaç: "web'e açık" bayrağını bulmak ve grup/ürün sayılarını SADECE web'e açık ürünlerle yeniden çıkarmak.

-- A) STOKLAR tablosunda web ile ilgili kolonlar
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'STOKLAR' AND (COLUMN_NAME LIKE '%web%' OR COLUMN_NAME LIKE '%eticaret%' OR COLUMN_NAME LIKE '%internet%' OR COLUMN_NAME LIKE '%b2b%')
ORDER BY COLUMN_NAME;
GO

-- B) Bulunan kolonların değer dağılımı (dinamik, sadece SELECT)
DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + N'SELECT ''' + COLUMN_NAME + N''' AS kolon, CAST(' + COLUMN_NAME + N' AS nvarchar(50)) AS deger, COUNT(*) AS adet FROM STOKLAR WHERE sto_pasif_fl = 0 GROUP BY ' + COLUMN_NAME + N' UNION ALL '
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'STOKLAR' AND (COLUMN_NAME LIKE '%web%' OR COLUMN_NAME LIKE '%eticaret%' OR COLUMN_NAME LIKE '%internet%' OR COLUMN_NAME LIKE '%b2b%');
IF LEN(@sql) > 0
BEGIN
    SET @sql = LEFT(@sql, LEN(@sql) - 10) + N' ORDER BY 1, 2';
    EXEC sp_executesql @sql;
END
GO

-- C) A sorgusunda çıkan GERÇEK kolon adını aşağıdaki iki sorguda KOLON_ADI yerine yazıp çalıştırın
--    (örn. sto_webe_gonderilsin_fl). 1 = web'e açık varsayıldı; B sorgusu değeri doğrular.

-- C1) Web'e açık ürünlerin ana grup bazında sayısı (isimle)
-- SELECT s.sto_anagrup_kod, g.san_isim, COUNT(*) AS web_acik_urun
-- FROM STOKLAR s
-- LEFT JOIN STOK_ANA_GRUPLARI g ON g.san_kod = s.sto_anagrup_kod
-- WHERE s.sto_pasif_fl = 0 AND s.KOLON_ADI = 1
-- GROUP BY s.sto_anagrup_kod, g.san_isim
-- ORDER BY CASE WHEN ISNUMERIC(s.sto_anagrup_kod) = 1 THEN CAST(s.sto_anagrup_kod AS int) ELSE 99999 END;

-- C2) Web'e açık ürünlerin ana + alt grup bazında sayısı
-- SELECT s.sto_anagrup_kod, s.sto_altgrup_kod, a.sta_isim, COUNT(*) AS web_acik_urun
-- FROM STOKLAR s
-- LEFT JOIN STOK_ALT_GRUPLARI a ON a.sta_kod = s.sto_altgrup_kod AND a.sta_ana_grup_kod = s.sto_anagrup_kod
-- WHERE s.sto_pasif_fl = 0 AND s.KOLON_ADI = 1
-- GROUP BY s.sto_anagrup_kod, s.sto_altgrup_kod, a.sta_isim
-- ORDER BY 1, 2;

