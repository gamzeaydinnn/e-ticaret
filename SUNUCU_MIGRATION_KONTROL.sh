#!/bin/bash
# =============================================================================
# SUNUCU VERITABANI KONTROL VE MANUAL MIGRATION KOMUTLARI
# =============================================================================
# Migration sorun yaşarsanız bu komutları kullanın
# (Normalde gerekli değil - otomatik oluyor!)

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_error() { echo -e "${RED}[✗]${NC} $1"; }

echo ""
echo "=============================================="
echo "  SUNUCU VERİTABANI KONTROL SCRIPTLERI"
echo "=============================================="
echo ""

# Proje dizinine git
cd ~/eticaret

# ADIM 1: SQL Server Sağlığı Kontrol Et
log_info "SQL Server sağlığı kontrol ediliyor..."
if docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1" &>/dev/null; then
    log_success "SQL Server çalışıyor!"
else
    log_error "SQL Server yanıt vermiyor!"
    exit 1
fi

echo ""

# ADIM 2: Veritabanı Mevcut Mu?
log_info "ECommerceDb veritabanı kontrol ediliyor..."
DB_EXISTS=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM sys.databases WHERE name='ECommerceDb'" -h-1 2>/dev/null | head -1 | tr -d ' ')

if [ "$DB_EXISTS" = "1" ]; then
    log_success "ECommerceDb mevcut"
else
    log_error "ECommerceDb bulunamadı! API'nin otomatik oluşturması bekleniyor..."
    echo "  Komut: docker-compose -f docker-compose.prod.yml logs -f api"
fi

echo ""

# ADIM 3: Tablo Sayısı Kontrol Et
log_info "Tablo sayısı kontrol ediliyor..."
TABLE_COUNT=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM ECommerceDb.INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE'" -h-1 2>/dev/null | head -1 | tr -d ' ')

log_success "Toplam $TABLE_COUNT tablo bulundu"

# Kritik tablolar
CRITICAL_TABLES=("AspNetUsers" "AspNetRoles" "Products" "Categories" "Orders")
for table in "${CRITICAL_TABLES[@]}"; do
    EXISTS=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "ECom1234" -C \
        -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='$table'" -h-1 2>/dev/null | head -1 | tr -d ' ')
    
    if [ "$EXISTS" = "1" ]; then
        echo "  ✓ $table"
    else
        echo "  ✗ $table (HATA!)"
    fi
done

echo ""

# ADIM 4: Veri Sayıları
log_info "Veri sayıları kontrol ediliyor..."

USERS=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.AspNetUsers" -h-1 2>/dev/null | head -1 | tr -d ' ')

ROLES=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.AspNetRoles" -h-1 2>/dev/null | head -1 | tr -d ' ')

PRODUCTS=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products" -h-1 2>/dev/null | head -1 | tr -d ' ')

CATEGORIES=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "ECom1234" -C \
    -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Categories" -h-1 2>/dev/null | head -1 | tr -d ' ')

echo "  📊 Kullanıcılar: $USERS"
echo "  🎭 Roller: $ROLES"
echo "  📦 Ürünler: $PRODUCTS"
echo "  🏷️  Kategoriler: $CATEGORIES"

echo ""

# ADIM 5: API Kontrolü
log_info "API health check..."
API_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/categories 2>/dev/null || echo "000")

if [ "$API_RESPONSE" = "200" ]; then
    log_success "API çalışıyor! (HTTP 200)"
    
    # Kategori sayısını göster
    CATEGORY_JSON=$(curl -s http://localhost:5000/api/categories 2>/dev/null || echo "[]")
    CATEGORY_COUNT=$(echo "$CATEGORY_JSON" | grep -o '"id"' | wc -l)
    echo "  Kategoriler endpoint'ten: $CATEGORY_COUNT kategori"
else
    log_error "API yanıt vermedi (HTTP $API_RESPONSE)"
fi

echo ""
echo "=============================================="
echo "  SORUN GİDERME KOMUTLARI"
echo "=============================================="
echo ""
echo "API log'larını görüntüle:"
echo "  docker-compose -f docker-compose.prod.yml logs api | tail -100"
echo ""
echo "SQL Server'a eriş:"
echo "  docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \\"
echo "    -S localhost -U sa -P 'ECom1234' -C"
echo ""
echo "Tüm veritabanlarını listele:"
echo "  docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \\"
echo "    -S localhost -U sa -P 'ECom1234' -C \\"
echo "    -Q 'SELECT name FROM sys.databases'"
echo ""
echo "API'yi restart et (migration'ı tekrar çalıştırmak için):"
echo "  docker-compose -f docker-compose.prod.yml restart api"
echo ""
echo "=============================================="
