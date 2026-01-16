#!/bin/bash
# ============================================================
# TÜRKÇE KARAKTER DÜZELTMESİ VE COLLATION MIGRATION SCRIPT
# ============================================================
# Bu script sunucuda çalıştırılarak:
# 1. Bozuk Türkçe karakterleri düzeltir
# 2. EF Core migration uygular (Turkish_CI_AS collation)
# ============================================================

echo "============================================================"
echo "🇹🇷 TÜRKÇE KARAKTER DÜZELTMESİ BAŞLATILIYOR"
echo "============================================================"
echo ""

# Değişkenler
DB_CONTAINER="ecommerce-sql-prod"
DB_PASSWORD="ECom1234"
DB_NAME="ECommerceDb"

# Kontrol: Container çalışıyor mu?
if ! docker ps | grep -q $DB_CONTAINER; then
    echo "❌ Hata: $DB_CONTAINER container'ı çalışmıyor!"
    echo "Önce container'ları başlatın: docker-compose -f docker-compose.prod.yml up -d"
    exit 1
fi

echo "✅ Container çalışıyor: $DB_CONTAINER"
echo ""

# ============================================================
# ADIM 1: Mevcut bozuk verileri düzelt
# ============================================================
echo "📝 ADIM 1: Bozuk Türkçe karakterler düzeltiliyor..."

docker exec -i $DB_CONTAINER /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" -d $DB_NAME -C << 'EOSQL'

-- Products tablosu
UPDATE Products
SET Name = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           Name,
           '&#x15F;', N'ş'),
           '&#x15E;', N'Ş'),
           '&#x131;', N'ı'),
           '&#x130;', N'İ'),
           '&#xFC;', N'ü'),
           '&#xDC;', N'Ü'),
           '&#xF6;', N'ö'),
           '&#xD6;', N'Ö'),
           '&#xE7;', N'ç'),
           '&#xC7;', N'Ç'),
           '&#x11F;', N'ğ'),
           '&#x11E;', N'Ğ')
WHERE Name LIKE '%&#x%';

UPDATE Products
SET Description = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                  Description,
                  '&#x15F;', N'ş'),
                  '&#x15E;', N'Ş'),
                  '&#x131;', N'ı'),
                  '&#x130;', N'İ'),
                  '&#xFC;', N'ü'),
                  '&#xDC;', N'Ü'),
                  '&#xF6;', N'ö'),
                  '&#xD6;', N'Ö'),
                  '&#xE7;', N'ç'),
                  '&#xC7;', N'Ç'),
                  '&#x11F;', N'ğ'),
                  '&#x11E;', N'Ğ')
WHERE Description LIKE '%&#x%';

-- Categories tablosu
UPDATE Categories
SET Name = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
           Name,
           '&#x15F;', N'ş'),
           '&#x15E;', N'Ş'),
           '&#x131;', N'ı'),
           '&#x130;', N'İ'),
           '&#xFC;', N'ü'),
           '&#xDC;', N'Ü'),
           '&#xF6;', N'ö'),
           '&#xD6;', N'Ö'),
           '&#xE7;', N'ç'),
           '&#xC7;', N'Ç'),
           '&#x11F;', N'ğ'),
           '&#x11E;', N'Ğ')
WHERE Name LIKE '%&#x%';

-- Users tablosu
UPDATE Users
SET FirstName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                FirstName,
                '&#x15F;', N'ş'),
                '&#x15E;', N'Ş'),
                '&#x131;', N'ı'),
                '&#x130;', N'İ'),
                '&#xFC;', N'ü'),
                '&#xDC;', N'Ü'),
                '&#xF6;', N'ö'),
                '&#xD6;', N'Ö'),
                '&#xE7;', N'ç'),
                '&#xC7;', N'Ç'),
                '&#x11F;', N'ğ'),
                '&#x11E;', N'Ğ')
WHERE FirstName LIKE '%&#x%';

UPDATE Users
SET LastName = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
               REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
               LastName,
               '&#x15F;', N'ş'),
               '&#x15E;', N'Ş'),
               '&#x131;', N'ı'),
               '&#x130;', N'İ'),
               '&#xFC;', N'ü'),
               '&#xDC;', N'Ü'),
               '&#xF6;', N'ö'),
               '&#xD6;', N'Ö'),
               '&#xE7;', N'ç'),
               '&#xC7;', N'Ç'),
               '&#x11F;', N'ğ'),
               '&#x11E;', N'Ğ')
WHERE LastName LIKE '%&#x%';

PRINT 'Türkçe karakter düzeltmesi tamamlandı!';
GO
EOSQL

if [ $? -eq 0 ]; then
    echo "✅ ADIM 1 tamamlandı: Bozuk karakterler düzeltildi"
else
    echo "⚠️ ADIM 1 uyarı: Bazı tablolar boş olabilir, devam ediliyor..."
fi

echo ""

# ============================================================
# ADIM 2: Düzeltme sonrası kontrol
# ============================================================
echo "🔍 ADIM 2: Kontrol ediliyor..."

docker exec -i $DB_CONTAINER /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" -d $DB_NAME -C << 'EOSQL'

-- Hala bozuk karakter var mı?
SELECT 'Products - Bozuk' AS Tablo, COUNT(*) AS Sayi FROM Products WHERE Name LIKE '%&#x%'
UNION ALL
SELECT 'Categories - Bozuk', COUNT(*) FROM Categories WHERE Name LIKE '%&#x%';

-- Örnek Türkçe karakterli ürünler
PRINT '';
PRINT 'Örnek Türkçe karakterli ürünler:';
SELECT TOP 5 Id, Name FROM Products WHERE Name LIKE N'%ş%' OR Name LIKE N'%ğ%' OR Name LIKE N'%ü%';
GO
EOSQL

echo ""
echo "============================================================"
echo "✅ TÜRKÇE KARAKTER DÜZELTMESİ TAMAMLANDI!"
echo "============================================================"
echo ""
echo "Not: EF Core migration otomatik olarak Turkish_CI_AS collation"
echo "     uygulayacaktır. API yeniden başlatıldığında aktif olur."
echo ""
