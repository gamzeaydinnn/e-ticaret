#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== MEVCUT COMPOSE AUTOCREATE AYARI ==="
grep -n 'AutoCreate' docker-compose.prod.yml || echo "BULUNAMADI!"

echo ""
echo "=== API CONTAINER ENV AUTOCREATE ==="
docker inspect ecommerce-api-prod --format '{{range .Config.Env}}{{println .}}{{end}}' 2>/dev/null | grep -i 'AutoCreate' || echo "ENV AYARLANMAMIS!"

echo ""
echo "=== TUM AUTO-OLUSTURULAN KATEGORILER SIL ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
-- Once bu kategorilerdeki urunleri Diger'e tasi
UPDATE Products SET CategoryId = 12
WHERE CategoryId IN (
  SELECT Id FROM Categories WHERE Description LIKE '%otomatik oluşturuldu%'
);
-- Mapping tablosunu guncelle
UPDATE MikroCategoryMappings SET CategoryId = 12
WHERE CategoryId IN (
  SELECT Id FROM Categories WHERE Description LIKE '%otomatik oluşturuldu%'
);
-- Sil
DELETE FROM Categories WHERE Description LIKE '%otomatik oluşturuldu%';
SELECT 'Silinen sahte kategori: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
SELECT COUNT(*) AS KalanKategori FROM Categories;"

echo ""
echo "=== COMPOSE DOSYASINA AUTOCREATE FALSE EKLE (eger yoksa) ==="
if ! grep -q 'AutoCreateCategories=false' docker-compose.prod.yml; then
  echo "EKLENIYOR..."
  # api service environment altina ekle
  sed -i '/CategoryMapping__DefaultCategoryName/a\      - CategoryMapping__AutoCreateCategories=false' docker-compose.prod.yml
  echo "Eklendi"
else
  echo "Zaten mevcut"
fi

echo ""
echo "=== COMPOSE AUTOCREATE KONTROL ==="
grep -n 'AutoCreate\|DefaultCategory' docker-compose.prod.yml

echo ""
echo "=== API RESTART ==="
docker-compose -f docker-compose.prod.yml up -d --no-deps api
sleep 5

echo ""
echo "=== API CONTAINER ENV KONTROL ==="
docker inspect ecommerce-api-prod --format '{{range .Config.Env}}{{println .}}{{end}}' | grep -i 'auto\|category'
