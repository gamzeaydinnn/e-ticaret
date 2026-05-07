#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MikroCategoryMappings GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;

-- Mevcut auto-mapped kayitlari sil, temiz mapping kur
DELETE FROM MikroCategoryMappings WHERE MikroAnagrupKod IN ('700','800','100','400','900','1000','1400','1500','1600','1200','1300','1900','2000','2200','21','12');

-- Dogru mappingleri ekle
INSERT INTO MikroCategoryMappings (MikroAnagrupKod, MikroAltgrupKod, MikroMarkaKod, CategoryId, BrandId, Priority, MikroGrupAciklama, Notes, IsActive, CreatedAt)
VALUES
  ('700',  NULL, NULL, 9,  NULL, 1, 'Temel Gida - Makarna, Baharat, Yag', 'Manuel mapping', 1, GETUTCDATE()),
  ('800',  NULL, NULL, 4,  NULL, 1, 'Sut ve Dondurma Urunleri',           'Manuel mapping', 1, GETUTCDATE()),
  ('100',  NULL, NULL, 6,  NULL, 1, 'Icecekler',                          'Manuel mapping', 1, GETUTCDATE()),
  ('400',  NULL, NULL, 3,  NULL, 1, 'Et ve Et Urunleri - Piliç',          'Manuel mapping', 1, GETUTCDATE()),
  ('900',  NULL, NULL, 8,  NULL, 1, 'Kisisel Bakim',                      'Manuel mapping', 1, GETUTCDATE()),
  ('1000', NULL, NULL, 8,  NULL, 1, 'Temizlik Urunleri',                  'Manuel mapping', 1, GETUTCDATE()),
  ('1400', NULL, NULL, 7,  NULL, 1, 'Atistirmalik',                       'Manuel mapping', 1, GETUTCDATE()),
  ('1500', NULL, NULL, 8,  NULL, 1, 'Kisisel Bakim - Dus Jeli, Kagit',    'Manuel mapping', 1, GETUTCDATE()),
  ('1600', NULL, NULL, 8,  NULL, 1, 'Kisisel Bakim - Deodorant, Dis',     'Manuel mapping', 1, GETUTCDATE()),
  ('1200', NULL, NULL, 9,  NULL, 1, 'Temel Gida - Hazir Yemek, Corba',    'Manuel mapping', 1, GETUTCDATE()),
  ('1300', NULL, NULL, 9,  NULL, 1, 'Temel Gida - Donmus Urun',           'Manuel mapping', 1, GETUTCDATE()),
  ('1900', NULL, NULL, 12, NULL, 1, 'Elektrikli Ev Aletleri',             'Manuel mapping', 1, GETUTCDATE()),
  ('2000', NULL, NULL, 12, NULL, 1, 'Pil ve Elektronik',                  'Manuel mapping', 1, GETUTCDATE()),
  ('2200', NULL, NULL, 5,  NULL, 1, 'Meyve ve Sebze',                     'Manuel mapping', 1, GETUTCDATE()),
  ('21',   NULL, NULL, 12, NULL, 1, 'Diger',                              'Manuel mapping', 1, GETUTCDATE()),
  ('12',   NULL, NULL, 7,  NULL, 1, 'Atistirmalik',                       'Manuel mapping', 1, GETUTCDATE());

SELECT 'Mapping eklendi: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
"

echo "=== URUN KATEGORİLERİNİ GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
UPDATE p
SET p.CategoryId = m.CategoryId
FROM Products p
INNER JOIN MikroProductCache c ON c.StokKod = p.SKU
INNER JOIN MikroCategoryMappings m ON m.MikroAnagrupKod = c.AnagrupKod
WHERE m.IsActive = 1
  AND m.MikroAnagrupKod <> '*';
SELECT 'Urun guncellendi: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
"

echo "=== SONUC ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi
FROM Products p
LEFT JOIN Categories cat ON cat.Id = p.CategoryId
GROUP BY cat.Name
ORDER BY UrunSayisi DESC;"
