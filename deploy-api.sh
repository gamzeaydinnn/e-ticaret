#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== CONTROLLER DOSYASI KOPYALAMA ==="
# Yeni controller zaten scp ile gelecek, burda sadece rebuild yapiyoruz

echo "=== API IMAGE REBUILD ==="
docker-compose -f docker-compose.prod.yml build --no-cache api 2>&1 | tail -20

echo ""
echo "=== API CONTAINER RESTART ==="
docker-compose -f docker-compose.prod.yml up -d --no-deps api

sleep 8

echo ""
echo "=== API HEALTH KONTROL ==="
curl -s 'http://localhost:5000/api/products?page=1&size=3' 2>&1 | python3 -c "
import sys, json
try:
    d = json.load(sys.stdin)
    if isinstance(d, list):
        print('API OK - Urun sayisi:', len(d))
        if d: print('Ilk urun:', d[0].get('name','?'), '| Kategori:', d[0].get('categoryName','?'))
    else:
        print('API response:', str(d)[:200])
except Exception as e:
    print('Parse error:', e)
" 2>/dev/null || echo "API yanit vermedi"
