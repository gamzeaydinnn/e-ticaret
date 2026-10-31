-- Mikro kategori keşfi - 2. tur (SADECE OKUMA). Her blok bağımsız, F5 ile hepsini çalıştırın.

-- A) Ana gruplar: kod, isim ve ürün sayısı (sonucu "Results to Grid" ile paylaşın)
SELECT g.san_kod, g.san_isim,
       (SELECT COUNT(*) FROM STOKLAR s WHERE s.sto_anagrup_kod = g.san_kod AND s.sto_pasif_fl = 0) AS aktif_urun
FROM STOK_ANA_GRUPLARI g
ORDER BY CASE WHEN ISNUMERIC(g.san_kod) = 1 THEN CAST(g.san_kod AS int) ELSE 99999 END, g.san_kod;
GO

-- B) Alt gruplar: ana grup adı ve ürün sayısı ile
SELECT a.sta_ana_grup_kod, g.san_isim AS ana_isim, a.sta_kod, a.sta_isim,
       (SELECT COUNT(*) FROM STOKLAR s
         WHERE s.sto_altgrup_kod = a.sta_kod AND s.sto_anagrup_kod = a.sta_ana_grup_kod AND s.sto_pasif_fl = 0) AS aktif_urun
FROM STOK_ALT_GRUPLARI a
LEFT JOIN STOK_ANA_GRUPLARI g ON g.san_kod = a.sta_ana_grup_kod
ORDER BY a.sta_ana_grup_kod, a.sta_kod;
GO

-- C) Ana grubu tanımsız ürün kodları (STOK_ANA_GRUPLARI'nda karşılığı olmayanlar)
SELECT s.sto_anagrup_kod, COUNT(*) AS adet
FROM STOKLAR s
LEFT JOIN STOK_ANA_GRUPLARI g ON g.san_kod = s.sto_anagrup_kod
WHERE s.sto_pasif_fl = 0 AND g.san_kod IS NULL
GROUP BY s.sto_anagrup_kod;
GO

-- D) Alt grubu, ana grubunun altında tanımlı olmayan ürünler (ör. 500/402)
SELECT s.sto_anagrup_kod, s.sto_altgrup_kod, COUNT(*) AS adet
FROM STOKLAR s
LEFT JOIN STOK_ALT_GRUPLARI a ON a.sta_kod = s.sto_altgrup_kod AND a.sta_ana_grup_kod = s.sto_anagrup_kod
WHERE s.sto_pasif_fl = 0 AND ISNULL(s.sto_altgrup_kod,'') <> '' AND a.sta_kod IS NULL
GROUP BY s.sto_anagrup_kod, s.sto_altgrup_kod
ORDER BY 1, 2;
GO

-- E) Yeni düzen (3-4 haneli) / eski düzen (1-2 haneli) ürün dağılımı
SELECT CASE WHEN LEN(ISNULL(sto_anagrup_kod,'')) >= 3 THEN 'YENI (3-4 hane)'
            WHEN LEN(ISNULL(sto_anagrup_kod,'')) = 0 THEN 'BOS'
            ELSE 'ESKI (1-2 hane)' END AS duzen,
       COUNT(*) AS aktif_urun
FROM STOKLAR WHERE sto_pasif_fl = 0
GROUP BY CASE WHEN LEN(ISNULL(sto_anagrup_kod,'')) >= 3 THEN 'YENI (3-4 hane)'
              WHEN LEN(ISNULL(sto_anagrup_kod,'')) = 0 THEN 'BOS'
              ELSE 'ESKI (1-2 hane)' END;
GO

-- F) Kategori tablosu (ktg) tam liste: 99=KDV gibi duruyor, kategori mi başka amaç mı?
SELECT ktg_kod, ktg_isim FROM STOK_KATEGORILERI ORDER BY 1;
GO

