#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== DIGER KATEGORİSİNDEKİ URUNLERIN ANAGRUPKOD DAGILIMI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
SELECT c.AnagrupKod, COUNT(*) AS UrunSayisi, MIN(c.StokAd) AS Ornek
FROM Products p
INNER JOIN MikroProductCache c ON c.StokKod = p.SKU
WHERE p.CategoryId = 12
GROUP BY c.AnagrupKod
ORDER BY COUNT(*) DESC;"
