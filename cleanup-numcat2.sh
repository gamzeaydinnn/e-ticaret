#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MAPPING GUNCELLE (QUOTED_IDENTIFIER fix) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
UPDATE MikroCategoryMappings
SET CategoryId = 12
WHERE CategoryId IN (
  SELECT Id FROM Categories
  WHERE (Name NOT LIKE '%[^0-9]%' AND LEN(Name) > 0)
     OR Name IN ('alkol', 'deneme')
);
SELECT 'Mapping guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;"

echo "=== NUMERIC KATEGORİLERİ SİL ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DELETE FROM Categories
WHERE (Name NOT LIKE '%[^0-9]%' AND LEN(Name) > 0)
   OR Name IN ('alkol', 'deneme');
SELECT 'Silinen kategori: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;"

echo "=== KALAN KATEGORİLER ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi
FROM Products p
LEFT JOIN Categories cat ON cat.Id = p.CategoryId
GROUP BY cat.Name
ORDER BY UrunSayisi DESC;"
