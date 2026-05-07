#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== URUN DURUM ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT COUNT(*) AS Toplam, SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS Aktif, SUM(CASE WHEN IsDeleted=1 THEN 1 ELSE 0 END) AS Silindi, SUM(CASE WHEN Stock > 0 THEN 1 ELSE 0 END) AS StokluUrun, SUM(CASE WHEN Price > 0 THEN 1 ELSE 0 END) AS FiyatliUrun FROM Products;"

echo ""
echo "=== KATEGORI BAZINDA AKTIF URUN ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT c.Name, COUNT(p.Id) AS Toplam, SUM(CASE WHEN p.IsActive=1 THEN 1 ELSE 0 END) AS Aktif FROM Categories c LEFT JOIN Products p ON p.CategoryId=c.Id GROUP BY c.Name ORDER BY COUNT(p.Id) DESC;"

echo ""
echo "=== ORNEK URUN (Et kategorisi) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb -Q \
  "SELECT TOP 3 p.Id, LEFT(p.Name,40) AS Name, p.Price, p.Stock, p.IsActive, p.IsDeleted FROM Products p JOIN Categories c ON p.CategoryId=c.Id WHERE c.Name LIKE '%Et%';"

echo ""
echo "=== API LOG HATALAR ==="
docker logs ecommerce-api-prod --tail 60 2>&1 | grep -iE 'error|exception|warn|product|categor' | tail -20
