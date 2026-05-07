#!/bin/bash
cd /home/huseyinadm/eticaret

# .env dosyasından admin bilgilerini oku
ADMIN_EMAIL=$(grep 'ADMIN_EMAIL\|SEED_ADMIN_EMAIL\|DEFAULT_ADMIN' .env 2>/dev/null | head -1 | cut -d= -f2 | tr -d '"' | tr -d "'")
API_URL="http://localhost:5000"

echo "=== ADMIN EMAIL: $ADMIN_EMAIL ==="

# Admin şifresini .env'den al
ADMIN_PASS=$(grep 'ADMIN_PASS\|SEED_ADMIN_PASS\|DEFAULT_ADMIN_PASS' .env 2>/dev/null | head -1 | cut -d= -f2 | tr -d '"' | tr -d "'")

echo "=== LOGIN DENENIYOR ==="
TOKEN=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" \
  --max-time 15 | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "Token alinamadi, .env icerigi:"
  grep -i 'admin\|seed\|default_user\|INITIAL' .env 2>/dev/null | grep -v '#' | head -20
else
  echo "TOKEN ALINDI: ${TOKEN:0:30}..."

  echo "=== CACHE FULL SYNC TETIKLENIYOR ==="
  curl -s -X POST "$API_URL/api/admin/micro/cache/sync?syncMode=full" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    --max-time 120 | head -c 500

  echo ""
  echo "=== PRODUCT TABLE SYNC ==="
  curl -s -X POST "$API_URL/api/admin/mikro-diagnostics/sync-product-table" \
    -H "Authorization: Bearer $TOKEN" \
    --max-time 60 | head -c 300

  echo ""
  echo "=== YENİ URUN SAYISI ==="
  docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
    -Q "SELECT 'Toplam urun' as Tip, CAST(COUNT(*) AS VARCHAR) as Sayi FROM Products UNION ALL SELECT 'Cache LocalProductId dolu', CAST(COUNT(*) AS VARCHAR) FROM MikroProductCache WHERE LocalProductId IS NOT NULL;"
fi
