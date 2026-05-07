#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MIKROCATEGORYMAPPINGS KOLONLARI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='MikroCategoryMappings' ORDER BY ORDINAL_POSITION;"

echo "=== ORNEKLER ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TOP 10 * FROM MikroCategoryMappings;"

echo "=== MIKROCACHE ORNEK GRUPKOD ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TOP 10 AnagrupKod, GrupKod FROM MikroProductCache GROUP BY AnagrupKod, GrupKod ORDER BY AnagrupKod;"
