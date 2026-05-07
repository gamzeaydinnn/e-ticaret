#!/bin/bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi FROM Products p LEFT JOIN Categories cat ON cat.Id = p.CategoryId GROUP BY cat.Name ORDER BY UrunSayisi DESC;"
