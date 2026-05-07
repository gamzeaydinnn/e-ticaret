#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MIKRO PRODUCT CACHE KAYIT SAYISI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT 'Cache kayit' as Tip, CAST(COUNT(*) AS VARCHAR) as Sayi FROM MikroProductCache;"

echo "=== MIKRO SYNC STATE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT SyncType, LastSyncTime, LastSyncStatus, LastErrorMessage FROM MikroSyncStates ORDER BY LastSyncTime DESC;"

echo "=== MICRO SYNC LOGS (son 10) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TOP 10 SyncType, StartTime, Status, RecordsProcessed, ErrorMessage FROM MicroSyncLogs ORDER BY StartTime DESC;"

echo "=== API LOGLARI MIKRO SYNC (son 50) ==="
docker logs ecommerce-api-prod --tail 50 2>&1 | grep -i "stok\|sync\|mikro\|error\|hata\|urun\|cache" | tail -30
