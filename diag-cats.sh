#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== DOCKER-COMPOSE AUTOCREATE AYARI ==="
grep -n 'AutoCreate\|HEALTH_TARGET' docker-compose.prod.yml

echo ""
echo "=== API CONTAINER ENV DEGISKENI ==="
docker inspect ecommerce-api-prod --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null | grep -i 'auto\|category' || echo "ENV bulunamadi"

echo ""
echo "=== KATEGORILER (TOPLAM) ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT COUNT(*) AS ToplamKategori FROM Categories; SELECT Id, Name, Description FROM Categories ORDER BY Id;"
