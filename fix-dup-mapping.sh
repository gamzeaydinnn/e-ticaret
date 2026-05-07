#!/bin/bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
-- Tum 500 ve 600 mappinglari goster
SELECT Id, MikroAnagrupKod, CategoryId, Notes FROM MikroCategoryMappings WHERE MikroAnagrupKod IN ('500','600') ORDER BY Id;

-- Eski cakisan mappinglari sil, sadece son eklenenler kalsın
DELETE FROM MikroCategoryMappings
WHERE MikroAnagrupKod IN ('500','600')
  AND Id NOT IN (
    SELECT MAX(Id) FROM MikroCategoryMappings WHERE MikroAnagrupKod IN ('500','600') GROUP BY MikroAnagrupKod
  );
SELECT 'Silinen cakisan: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;

-- Simdi guncelle
UPDATE p SET p.CategoryId = m.CategoryId
FROM Products p
INNER JOIN MikroProductCache c ON c.StokKod = p.SKU
INNER JOIN MikroCategoryMappings m ON m.MikroAnagrupKod = c.AnagrupKod AND m.IsActive = 1
WHERE c.AnagrupKod IN ('500','600');
SELECT 'Urun guncellendi: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;

SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi
FROM Products p LEFT JOIN Categories cat ON cat.Id = p.CategoryId
GROUP BY cat.Name ORDER BY UrunSayisi DESC;"
