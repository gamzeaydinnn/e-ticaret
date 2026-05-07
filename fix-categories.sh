#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== KATEGORİ MAPPING GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "
UPDATE p
SET p.CategoryId = m.CategoryId
FROM Products p
INNER JOIN MikroProductCache c ON c.StokKod = p.SKU
INNER JOIN MikroCategoryMappings m ON m.MikroAnagrupKod = c.AnagrupKod
WHERE m.IsActive = 1
  AND m.MikroAnagrupKod <> '*'
  AND m.Priority = (
    SELECT MIN(m2.Priority)
    FROM MikroCategoryMappings m2
    WHERE m2.MikroAnagrupKod = c.AnagrupKod
      AND m2.IsActive = 1
  );
SELECT 'Kategori guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
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
