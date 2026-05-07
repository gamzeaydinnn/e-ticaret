#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== DUPLICATE MAPPING KAYITLARI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT MikroAnagrupKod, COUNT(*) AS Adet FROM MikroCategoryMappings WHERE IsActive=1 GROUP BY MikroAnagrupKod HAVING COUNT(*) > 1 ORDER BY MikroAnagrupKod;"

echo ""
echo "=== TUM AKTIF MAPPING ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT Id, MikroAnagrupKod, CategoryId, Priority, IsActive FROM MikroCategoryMappings ORDER BY MikroAnagrupKod, Priority DESC;"

echo ""
echo "=== DUPLICATE TEMIZLE (sadece en yuksek Priority'yi birak) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
-- Her AnagrupKod icin en yuksek Priority'li kaydı bırak, diğerlerini sil
DELETE FROM MikroCategoryMappings
WHERE Id NOT IN (
  SELECT MAX(Id) FROM MikroCategoryMappings
  WHERE IsActive = 1
  GROUP BY MikroAnagrupKod
)
AND IsActive = 1;
SELECT 'Silinen duplicate: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;"

echo ""
echo "=== SONUC: KALAN MAPPING ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT Id, MikroAnagrupKod, CategoryId, IsActive FROM MikroCategoryMappings ORDER BY MikroAnagrupKod;"
