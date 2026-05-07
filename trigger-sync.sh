#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MIKRO ILGILI TABLOLAR ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE '%ikro%' OR TABLE_NAME LIKE '%Stok%' OR TABLE_NAME LIKE '%Cache%' OR TABLE_NAME LIKE '%Sync%' ORDER BY TABLE_NAME;"

echo "=== HANGFIRE JOBS ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TOP 10 StateName, InvocationData, CreatedAt FROM HangFire.Job ORDER BY CreatedAt DESC;" 2>/dev/null || echo "Hangfire tablosu yok veya bos"

echo "=== FULL SYNC TETIKLE ==="
curl -s -X POST "http://localhost:5000/api/mikro/sync/full" \
  -H "Content-Type: application/json" \
  --max-time 10 2>&1 || echo "Full sync endpoint cagrilamadi"

echo "=== STOK SYNC TETIKLE ==="
curl -s -X POST "http://localhost:5000/api/mikro/sync/stok" \
  -H "Content-Type: application/json" \
  --max-time 10 2>&1 || echo "Stok sync endpoint cagrilamadi"

echo "=== HANGFIRE DASHBOARD ULASILABILIRLIK ==="
curl -s -o /dev/null -w "%{http_code}" "http://localhost:5000/hangfire" --max-time 5
