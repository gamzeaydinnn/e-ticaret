#!/bin/bash
# =============================================================================
# SUNUCU SIFIRDAN DEPLOY SCRIPTI
# =============================================================================
# Bu script sunucuyu tamamen temizler ve sıfırdan deploy eder.
# Çalıştırmadan önce: chmod +x SUNUCU_SIFIRDAN_DEPLOY.sh
#
# KULLANIM:
# 1. SSH ile sunucuya bağlan: ssh huseyinadm@31.186.24.78
# 2. Bu dosyayı sunucuya kopyala veya içeriğini yapıştır
# 3. chmod +x SUNUCU_SIFIRDAN_DEPLOY.sh && ./SUNUCU_SIFIRDAN_DEPLOY.sh
# =============================================================================

set -e

# Renkli çıktı için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

echo ""
echo "=============================================="
echo "  E-TICARET SUNUCU SIFIRDAN DEPLOY"
echo "=============================================="
echo ""

# 1. ADIM: Mevcut konteynerları durdur
log_info "ADIM 1: Mevcut konteynerları durduruluyor..."
cd ~
if [ -d "eticaret" ]; then
    cd ~/eticaret
    docker-compose -f docker-compose.prod.yml down 2>/dev/null || true
    cd ~
fi
if [ -d "ecommerce" ]; then
    cd ~/ecommerce
    docker-compose -f docker-compose.prod.yml down 2>/dev/null || true
    cd ~
fi
log_success "Konteynerlar durduruldu"

# 2. ADIM: Eski klasörleri temizle
log_info "ADIM 2: Eski proje klasörleri temizleniyor..."
cd ~
rm -rf ecommerce 2>/dev/null || true
rm -rf eticaret 2>/dev/null || true
log_success "Eski klasörler temizlendi"

# 3. ADIM: Docker temizliği
log_info "ADIM 3: Docker temizliği yapılıyor..."
docker system prune -af 2>/dev/null || true
docker volume prune -f 2>/dev/null || true
log_success "Docker temizliği tamamlandı"

# 4. ADIM: Projeyi GitHub'dan çek
log_info "ADIM 4: Proje GitHub'dan çekiliyor..."
cd ~
git clone https://github.com/gamzeaydinnn/e-ticaret.git eticaret
cd ~/eticaret
log_success "Proje klonlandı"

# 5. ADIM: .env dosyasını oluştur
log_info "ADIM 5: .env dosyası oluşturuluyor..."
cat > .env << 'EOF'
# Database Configuration
DB_PASSWORD=ECom1234
DB_PORT=1435

# Frontend Configuration
FRONTEND_PORT=3000

# API Configuration
ASPNETCORE_ENVIRONMENT=Production
EOF
log_success ".env dosyası oluşturuldu"

# 6. ADIM: Docker build
log_info "ADIM 6: Docker image'ları build ediliyor (bu 5-10 dakika sürebilir)..."
docker-compose -f docker-compose.prod.yml build --no-cache
log_success "Docker build tamamlandı"

# 7. ADIM: Servisleri başlat
log_info "ADIM 7: Servisler başlatılıyor..."
docker-compose -f docker-compose.prod.yml up -d
log_success "Servisler başlatıldı"

# 8. ADIM: SQL Server'ın hazır olmasını bekle
log_info "ADIM 8: SQL Server'ın hazır olması bekleniyor..."
sleep 30
for i in {1..30}; do
    if docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT 1" &>/dev/null; then
        log_success "SQL Server hazır!"
        break
    fi
    echo "  Bekleniyor... ($i/30)"
    sleep 5
done

# 9. ADIM: Veritabanını oluştur
log_info "ADIM 9: Veritabanı kontrol ediliyor..."
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ECommerceDb')
BEGIN
    CREATE DATABASE ECommerceDb
    PRINT 'ECommerceDb oluşturuldu'
END
ELSE
    PRINT 'ECommerceDb zaten mevcut'
"
log_success "Veritabanı hazır"

# 10. ADIM: API'nin migration'ları çalıştırmasını bekle
log_info "ADIM 10: API başlatılması ve migration bekleniyor..."
sleep 20

# API health check
for i in {1..20}; do
    if curl -s http://localhost:5000/api/categories > /dev/null 2>&1; then
        log_success "API çalışıyor!"
        break
    fi
    echo "  API bekleniyor... ($i/20)"
    sleep 5
done

# 11. ADIM: Seed data yükle (opsiyonel)
log_info "ADIM 11: Seed data kontrol ediliyor..."
if [ -f "seed-products.sql" ]; then
    PRODUCT_COUNT=$(docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products" -h-1 2>/dev/null | head -1 | tr -d ' ')
    if [ "$PRODUCT_COUNT" = "0" ] || [ -z "$PRODUCT_COUNT" ]; then
        log_info "Seed data yükleniyor..."
        cat seed-products.sql | docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
        log_success "Seed data yüklendi"
    else
        log_success "Veritabanında zaten $PRODUCT_COUNT ürün var"
    fi
fi

# 12. ADIM: Durum kontrolü
echo ""
echo "=============================================="
echo "  DEPLOY TAMAMLANDI - DURUM RAPORU"
echo "=============================================="
echo ""

log_info "Konteyner durumları:"
docker-compose -f docker-compose.prod.yml ps

echo ""
log_info "API Testi:"
API_RESPONSE=$(curl -s http://localhost:5000/api/categories 2>/dev/null || echo "HATA")
if [[ "$API_RESPONSE" == *"["* ]]; then
    log_success "API çalışıyor!"
    CATEGORY_COUNT=$(echo $API_RESPONSE | grep -o '"id"' | wc -l)
    echo "  Kategori sayısı: $CATEGORY_COUNT"
else
    log_warning "API yanıt vermiyor. Logları kontrol edin:"
    echo "  docker-compose -f docker-compose.prod.yml logs api"
fi

echo ""
log_info "Frontend Testi:"
FRONTEND_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000 2>/dev/null || echo "000")
if [ "$FRONTEND_RESPONSE" = "200" ]; then
    log_success "Frontend çalışıyor!"
else
    log_warning "Frontend yanıt vermiyor. HTTP: $FRONTEND_RESPONSE"
fi

echo ""
echo "=============================================="
echo "  ERİŞİM ADRESLERİ"
echo "=============================================="
echo ""
echo "  Frontend: http://31.186.24.78:3000"
echo "  API:      http://31.186.24.78:5000/api"
echo ""
echo "  Logları görmek için:"
echo "    docker-compose -f docker-compose.prod.yml logs -f"
echo ""
echo "=============================================="
