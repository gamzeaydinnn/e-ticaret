#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== URUN DURUMU ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT 'Toplam urun' as Tip, CAST(COUNT(*) AS VARCHAR) as Sayi FROM Products UNION ALL SELECT 'Aktif urun', CAST(COUNT(*) AS VARCHAR) FROM Products WHERE IsActive=1 UNION ALL SELECT 'Diger kategorisindeki', CAST(COUNT(*) AS VARCHAR) FROM Products WHERE CategoryId IN (SELECT Id FROM Categories WHERE Slug='diger' OR Name='Diger');"

echo "=== KATEGORI URUN DAGILIMI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT c.Name as Kategori, COUNT(p.Id) as UrunSayisi FROM Categories c LEFT JOIN Products p ON p.CategoryId=c.Id GROUP BY c.Name ORDER BY COUNT(p.Id) DESC;"

echo "=== MIKRO CACHE DURUMU ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT 'MikroCache kayit sayisi' as Tip, CAST(COUNT(*) AS VARCHAR) as Sayi FROM MikroProductCaches; SELECT TOP 5 StokKodu, StokAdi, AnagrupKod, GrupKod FROM MikroProductCaches;"

echo "=== API LOGLARI (son 30 satir) ==="
docker logs ecommerce-api-prod --tail 30 2>&1
