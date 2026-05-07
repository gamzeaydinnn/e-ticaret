#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== NUMERIC KATEGORİLERİ DİĞER'E TAŞI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "
-- Numeric isimli kategorilerdeki ürünleri Diger'e taşı (Id=12)
UPDATE Products
SET CategoryId = 12
WHERE CategoryId IN (
  SELECT Id FROM Categories
  WHERE Name NOT LIKE '%[^0-9]%' AND LEN(Name) > 0
     OR Name IN ('alkol', 'deneme')
);
SELECT 'Ürün Diger kategorisingüncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
"

echo "=== NUMERIC KATEGORİ MAPPINGLERİ GÜNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "
-- MikroCategoryMappings'deki numeric kategorileri Diger'e yönlendir
UPDATE MikroCategoryMappings
SET CategoryId = 12
WHERE CategoryId IN (
  SELECT Id FROM Categories
  WHERE Name NOT LIKE '%[^0-9]%' AND LEN(Name) > 0
     OR Name IN ('alkol', 'deneme')
);
SELECT 'Mapping guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
"

echo "=== NUMERIC VE TEST KATEGORİLERİNİ SİL ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "
DELETE FROM Categories
WHERE (Name NOT LIKE '%[^0-9]%' AND LEN(Name) > 0)
   OR Name IN ('alkol', 'deneme');
SELECT 'Silinen kategori: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
"

echo "=== SONUC ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "
SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi
FROM Products p
LEFT JOIN Categories cat ON cat.Id = p.CategoryId
GROUP BY cat.Name
ORDER BY UrunSayisi DESC;
"
