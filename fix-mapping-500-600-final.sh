#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== 500 ve 600 MAPPING DUZELT ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
-- 500 = Sut Urunleri (CategoryId=4)
UPDATE MikroCategoryMappings SET CategoryId = 4 WHERE MikroAnagrupKod = '500' AND IsActive = 1;
-- 600 = Temel Gida (CategoryId=9)
UPDATE MikroCategoryMappings SET CategoryId = 9 WHERE MikroAnagrupKod = '600' AND IsActive = 1;
SELECT 'Guncellendi' AS Sonuc;
SELECT Id, MikroAnagrupKod, CategoryId FROM MikroCategoryMappings WHERE MikroAnagrupKod IN ('500','600');"

echo ""
echo "=== URUN KATEGORILERINI GUNCELLE (500 ve 600) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
-- 500 urunleri Sut kategorisine al
UPDATE p SET p.CategoryId = 4
FROM Products p
JOIN MikroProductCaches mc ON mc.StokKod = p.SKU
WHERE mc.AnagrupKod = '500';
-- 600 urunleri Temel Gida kategorisine al
UPDATE p SET p.CategoryId = 9
FROM Products p
JOIN MikroProductCaches mc ON mc.StokKod = p.SKU
WHERE mc.AnagrupKod = '600';
SELECT 'Urunler guncellendi' AS Sonuc;"

echo ""
echo "=== FINAL KATEGORI DAGILIMI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT c.Name, COUNT(p.Id) AS UrunSayisi FROM Categories c LEFT JOIN Products p ON p.CategoryId=c.Id GROUP BY c.Name ORDER BY COUNT(p.Id) DESC;"

echo ""
echo "=== API TEST ==="
curl -s 'http://localhost:5000/api/products?page=1&size=3' 2>&1 | python3 -c "import sys,json; d=json.load(sys.stdin); print('OK - Urun sayisi:', len(d) if isinstance(d,list) else d.get('totalCount','?'), 'Ilk urun:', d[0]['name'] if isinstance(d,list) and len(d)>0 else d)" 2>/dev/null || curl -s 'http://localhost:5000/api/products?page=1&size=3' 2>&1 | head -200
