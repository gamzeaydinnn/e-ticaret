-- Mikro kategori keşfi - 4. tur (SADECE OKUMA)
-- Web'e açık bayrağı: sto_webe_gonderilecek_fl = 1  (2.961 aktif ürün)

-- C1) Web'e açık ürünlerin ANA GRUP bazında sayısı
SELECT s.sto_anagrup_kod, g.san_isim, COUNT(*) AS web_acik_urun
FROM STOKLAR s
LEFT JOIN STOK_ANA_GRUPLARI g ON g.san_kod = s.sto_anagrup_kod
WHERE s.sto_pasif_fl = 0 AND s.sto_webe_gonderilecek_fl = 1
GROUP BY s.sto_anagrup_kod, g.san_isim
ORDER BY CASE WHEN ISNUMERIC(s.sto_anagrup_kod) = 1 THEN CAST(s.sto_anagrup_kod AS int) ELSE 99999 END, s.sto_anagrup_kod;
GO

-- C2) Web'e açık ürünlerin ANA + ALT GRUP bazında sayısı
SELECT s.sto_anagrup_kod, g.san_isim, s.sto_altgrup_kod, a.sta_isim, COUNT(*) AS web_acik_urun
FROM STOKLAR s
LEFT JOIN STOK_ANA_GRUPLARI g ON g.san_kod = s.sto_anagrup_kod
LEFT JOIN STOK_ALT_GRUPLARI a ON a.sta_kod = s.sto_altgrup_kod AND a.sta_ana_grup_kod = s.sto_anagrup_kod
WHERE s.sto_pasif_fl = 0 AND s.sto_webe_gonderilecek_fl = 1
GROUP BY s.sto_anagrup_kod, g.san_isim, s.sto_altgrup_kod, a.sta_isim
ORDER BY CASE WHEN ISNUMERIC(s.sto_anagrup_kod) = 1 THEN CAST(s.sto_anagrup_kod AS int) ELSE 99999 END, s.sto_anagrup_kod, s.sto_altgrup_kod;
GO

-- C3) Web'e açık olup pasif=1 olanlar var mı? (sync filtresi için)
SELECT sto_pasif_fl, COUNT(*) AS adet FROM STOKLAR WHERE sto_webe_gonderilecek_fl = 1 GROUP BY sto_pasif_fl;
GO

-- C4) Web'e açık ürünlerin yeni/eski düzen dağılımı
SELECT CASE WHEN LEN(ISNULL(sto_anagrup_kod,'')) >= 3 THEN 'YENI (3-4 hane)'
            WHEN LEN(ISNULL(sto_anagrup_kod,'')) = 0 THEN 'BOS'
            ELSE 'ESKI (1-2 hane)' END AS duzen, COUNT(*) AS web_acik_urun
FROM STOKLAR WHERE sto_pasif_fl = 0 AND sto_webe_gonderilecek_fl = 1
GROUP BY CASE WHEN LEN(ISNULL(sto_anagrup_kod,'')) >= 3 THEN 'YENI (3-4 hane)'
              WHEN LEN(ISNULL(sto_anagrup_kod,'')) = 0 THEN 'BOS'
              ELSE 'ESKI (1-2 hane)' END;
GO

-- C5) Web'e açık ve eski düzen (1-2 hane) olan ürünlerden örnek (en çok 100)
SELECT TOP 100 sto_anagrup_kod, sto_altgrup_kod, sto_kod, sto_isim
FROM STOKLAR
WHERE sto_pasif_fl = 0 AND sto_webe_gonderilecek_fl = 1 AND LEN(ISNULL(sto_anagrup_kod,'')) BETWEEN 1 AND 2
ORDER BY sto_anagrup_kod;
GO

