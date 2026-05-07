#!/bin/bash
echo "=== API URUN TESTI ==="
result=$(curl -s 'http://localhost:5000/api/products?page=1&size=5')
if echo "$result" | grep -q '"name"'; then
  echo "API CALISYOR - Urunler geliyor"
  echo "$result" | python3 -c "import sys,json; d=json.load(sys.stdin); print('Urun sayisi (page):', len(d)); [print(' -', p['name'][:40], '| Kategori:', p.get('categoryName','?')) for p in d[:3]]"
else
  echo "API HATA:"
  echo "$result" | head -c 300
fi

echo ""
echo "=== KATEGORI BAZLI TEST (Et=3) ==="
result2=$(curl -s 'http://localhost:5000/api/products?categoryId=3&page=1&size=5')
if echo "$result2" | grep -q '"name"'; then
  echo "Kategori filtresi calisyor"
  echo "$result2" | python3 -c "import sys,json; d=json.load(sys.stdin); print('Et urun sayisi (page):', len(d))"
else
  echo "Kategori testi sonuc:"
  echo "$result2" | head -c 300
fi
