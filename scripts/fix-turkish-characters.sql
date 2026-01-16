-- ============================================================
-- TÜRKÇE KARAKTER DÜZELTMESİ VE COLLATION AYARI
-- ============================================================
-- Bu script, veritabanındaki bozuk Türkçe karakterleri düzeltir
-- ve tüm text sütunlarına Turkish_CI_AS collation uygular.
-- 
-- Sorun: HTML Entity encode edilmiş Türkçe karakterler
-- Örnek: "Dana Ku&#x15F;ba&#x15F;&#x131;" -> "Dana Kuşbaşı"
-- ============================================================

USE ECommerceDb;
GO

-- ============================================================
-- ADIM 1: DATABASE SEVİYESİNDE COLLATION DEĞİŞTİRME
-- ============================================================
-- NOT: Bu işlem veritabanı tek kullanıcı modundayken yapılmalıdır
-- Eğer Docker container içindeyseniz, bu adımı atlayabilirsiniz.
-- EF Core Migration bu işlemi otomatik yapacaktır.

-- ALTER DATABASE ECommerceDb COLLATE Turkish_CI_AS;
-- GO

-- ============================================================
-- ADIM 2: HTML ENTITY DECODE - BOZUK TÜRKÇE KARAKTERLERİ DÜZELT
-- ============================================================
-- &#x15F; = ş (küçük ş)
-- &#x15E; = Ş (büyük Ş)
-- &#x131; = ı (küçük ı - noktasız)
-- &#x130; = İ (büyük İ - noktalı)
-- &#xFC;  = ü (küçük ü)
-- &#xDC;  = Ü (büyük Ü)
-- &#xF6;  = ö (küçük ö)
-- &#xD6;  = Ö (büyük Ö)
-- &#xE7;  = ç (küçük ç)
-- &#xC7;  = Ç (büyük Ç)
-- &#x11F; = ğ (küçük ğ)
-- &#x11E; = Ğ (büyük Ğ)

PRINT 'Türkçe karakter düzeltmesi başlatılıyor...';

-- ============================================================
-- PRODUCTS TABLOSU DÜZELTME
-- ============================================================
PRINT 'Products tablosu düzeltiliyor...';

UPDATE Products
SET Name = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           Name,
           '&#x15F;', N'ş'),    -- küçük ş
           '&#x15E;', N'Ş'),    -- büyük Ş
           '&#x131;', N'ı'),    -- küçük ı (noktasız)
           '&#x130;', N'İ'),    -- büyük İ (noktalı)
           '&#xFC;', N'ü'),     -- küçük ü
           '&#xDC;', N'Ü'),     -- büyük Ü
           '&#xF6;', N'ö'),     -- küçük ö
           '&#xD6;', N'Ö'),     -- büyük Ö
           '&#xE7;', N'ç'),     -- küçük ç
           '&#xC7;', N'Ç'),     -- büyük Ç
           '&#x11F;', N'ğ'),    -- küçük ğ
           '&#x11E;', N'Ğ')     -- büyük Ğ
WHERE Name LIKE '%&#x%';

-- Description alanı için de aynı işlem
UPDATE Products
SET Description = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  Description,
                  '&#x15F;', N'ş'),
                  '&#x15E;', N'Ş'),
                  '&#x131;', N'ı'),
                  '&#x130;', N'İ'),
                  '&#xFC;', N'ü'),
                  '&#xDC;', N'Ü'),
                  '&#xF6;', N'ö'),
                  '&#xD6;', N'Ö'),
                  '&#xE7;', N'ç'),
                  '&#xC7;', N'Ç'),
                  '&#x11F;', N'ğ'),
                  '&#x11E;', N'Ğ')
WHERE Description LIKE '%&#x%';

-- ============================================================
-- CATEGORIES TABLOSU DÜZELTME
-- ============================================================
PRINT 'Categories tablosu düzeltiliyor...';

UPDATE Categories
SET Name = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           Name,
           '&#x15F;', N'ş'),
           '&#x15E;', N'Ş'),
           '&#x131;', N'ı'),
           '&#x130;', N'İ'),
           '&#xFC;', N'ü'),
           '&#xDC;', N'Ü'),
           '&#xF6;', N'ö'),
           '&#xD6;', N'Ö'),
           '&#xE7;', N'ç'),
           '&#xC7;', N'Ç'),
           '&#x11F;', N'ğ'),
           '&#x11E;', N'Ğ')
WHERE Name LIKE '%&#x%';

UPDATE Categories
SET Description = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  Description,
                  '&#x15F;', N'ş'),
                  '&#x15E;', N'Ş'),
                  '&#x131;', N'ı'),
                  '&#x130;', N'İ'),
                  '&#xFC;', N'ü'),
                  '&#xDC;', N'Ü'),
                  '&#xF6;', N'ö'),
                  '&#xD6;', N'Ö'),
                  '&#xE7;', N'ç'),
                  '&#xC7;', N'Ç'),
                  '&#x11F;', N'ğ'),
                  '&#x11E;', N'Ğ')
WHERE Description LIKE '%&#x%';

-- ============================================================
-- USERS TABLOSU DÜZELTME
-- ============================================================
PRINT 'Users tablosu düzeltiliyor...';

UPDATE Users
SET FirstName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                FirstName,
                '&#x15F;', N'ş'),
                '&#x15E;', N'Ş'),
                '&#x131;', N'ı'),
                '&#x130;', N'İ'),
                '&#xFC;', N'ü'),
                '&#xDC;', N'Ü'),
                '&#xF6;', N'ö'),
                '&#xD6;', N'Ö'),
                '&#xE7;', N'ç'),
                '&#xC7;', N'Ç'),
                '&#x11F;', N'ğ'),
                '&#x11E;', N'Ğ')
WHERE FirstName LIKE '%&#x%';

UPDATE Users
SET LastName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
               REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
               LastName,
               '&#x15F;', N'ş'),
               '&#x15E;', N'Ş'),
               '&#x131;', N'ı'),
               '&#x130;', N'İ'),
               '&#xFC;', N'ü'),
               '&#xDC;', N'Ü'),
               '&#xF6;', N'ö'),
               '&#xD6;', N'Ö'),
               '&#xE7;', N'ç'),
               '&#xC7;', N'Ç'),
               '&#x11F;', N'ğ'),
               '&#x11E;', N'Ğ')
WHERE LastName LIKE '%&#x%';

-- ============================================================
-- BRANDS TABLOSU DÜZELTME
-- ============================================================
PRINT 'Brands tablosu düzeltiliyor...';

UPDATE Brands
SET Name = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           Name,
           '&#x15F;', N'ş'),
           '&#x15E;', N'Ş'),
           '&#x131;', N'ı'),
           '&#x130;', N'İ'),
           '&#xFC;', N'ü'),
           '&#xDC;', N'Ü'),
           '&#xF6;', N'ö'),
           '&#xD6;', N'Ö'),
           '&#xE7;', N'ç'),
           '&#xC7;', N'Ç'),
           '&#x11F;', N'ğ'),
           '&#x11E;', N'Ğ')
WHERE Name LIKE '%&#x%';

-- ============================================================
-- BANNERS TABLOSU DÜZELTME
-- ============================================================
PRINT 'Banners tablosu düzeltiliyor...';

UPDATE Banners
SET Title = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
            Title,
            '&#x15F;', N'ş'),
            '&#x15E;', N'Ş'),
            '&#x131;', N'ı'),
            '&#x130;', N'İ'),
            '&#xFC;', N'ü'),
            '&#xDC;', N'Ü'),
            '&#xF6;', N'ö'),
            '&#xD6;', N'Ö'),
            '&#xE7;', N'ç'),
            '&#xC7;', N'Ç'),
            '&#x11F;', N'ğ'),
            '&#x11E;', N'Ğ')
WHERE Title LIKE '%&#x%';

-- ============================================================
-- DOĞRULAMA SORGUSU
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'DÜZELTME SONRASI KONTROL';
PRINT '============================================================';

-- Hala bozuk karakter var mı kontrol et
SELECT 'Products - Bozuk Kayıt Sayısı' AS Tablo, COUNT(*) AS Sayi 
FROM Products WHERE Name LIKE '%&#x%' OR Description LIKE '%&#x%'
UNION ALL
SELECT 'Categories - Bozuk Kayıt Sayısı', COUNT(*) 
FROM Categories WHERE Name LIKE '%&#x%' OR Description LIKE '%&#x%'
UNION ALL
SELECT 'Users - Bozuk Kayıt Sayısı', COUNT(*) 
FROM Users WHERE FirstName LIKE '%&#x%' OR LastName LIKE '%&#x%';

-- Örnek düzeltilmiş ürünleri göster
PRINT '';
PRINT 'Örnek Ürünler (Türkçe karakterli):';
SELECT TOP 10 Id, Name, Price 
FROM Products 
WHERE Name LIKE N'%ş%' 
   OR Name LIKE N'%ğ%' 
   OR Name LIKE N'%ü%' 
   OR Name LIKE N'%ö%' 
   OR Name LIKE N'%ç%' 
   OR Name LIKE N'%ı%'
ORDER BY Name;

PRINT '';
PRINT 'Türkçe karakter düzeltmesi tamamlandı!';
GO
