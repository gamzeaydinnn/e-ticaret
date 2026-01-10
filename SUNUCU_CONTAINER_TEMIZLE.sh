#!/bin/bash
# =============================================================================
# SUNUCU CONTAINER TEMIZLEME - EXISTING CONTAINER HATASI ÇÖZÜMÜ
# =============================================================================
# Error: "The container name "/ecommerce-sql-prod" is already in use"
# Bu script eski container'ları kaldırır ve temiz deploy sağlar

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[!]${NC} $1"; }

echo ""
echo "=============================================="
echo "  SUNUCU CONTAINER TEMIZLEME"
echo "=============================================="
echo ""

cd ~/eticaret

# ADIM 1: Çalışan konteynerları durdur
log_info "Çalışan konteynerler durduruluyor..."
docker stop $(docker ps -q) 2>/dev/null || true
sleep 5
log_success "Konteynerler durduruldu"

echo ""

# ADIM 2: Belirtilen container'ları kaldır
log_info "Eski container'lar kaldırılıyor..."
docker rm ecommerce-sql-prod 2>/dev/null || log_warning "ecommerce-sql-prod bulunamadı"
docker rm ecommerce-api-prod 2>/dev/null || log_warning "ecommerce-api-prod bulunamadı"
docker rm ecommerce-frontend-prod 2>/dev/null || log_warning "ecommerce-frontend-prod bulunamadı"
log_success "Eski container'lar kaldırıldı"

echo ""

# ADIM 3: Dangling image'ları temizle
log_info "Dangling image'lar kaldırılıyor..."
docker image prune -f 2>/dev/null || true
log_success "Image'lar temizlendi"

echo ""

# ADIM 4: Docker system prune
log_info "Docker system temizliği yapılıyor..."
docker system prune -af 2>/dev/null || true
docker volume prune -f 2>/dev/null || true
log_success "Docker system temizlendi"

echo ""

# ADIM 5: Network kontrol (opsiyonel)
log_info "Network'ler kontrol ediliyor..."
docker network ls | grep ecommerce || log_warning "ecommerce network'ü bulunamadı"

echo ""

# ADIM 6: Fresh build başlat
log_info "Temiz Docker build başlatılıyor..."
docker-compose -f docker-compose.prod.yml build --no-cache --force-rm

log_success "Build tamamlandı"

echo ""

# ADIM 7: Fresh deployment
log_info "Servisleri başlatılıyor..."
docker-compose -f docker-compose.prod.yml up -d

log_success "Servisler başlatıldı"

echo ""

# ADIM 8: Durum kontrol
sleep 30
echo "=============================================="
log_info "Konteyner durumları:"
docker-compose -f docker-compose.prod.yml ps

echo ""
log_info "Sonraki adımlar:"
echo "  1. SQL Server'ın hazır olması için bekle (log'ları izle):"
echo "     docker-compose -f docker-compose.prod.yml logs -f sqlserver"
echo ""
echo "  2. API startup'ını izle (migration mesajları göreceksin):"
echo "     docker-compose -f docker-compose.prod.yml logs -f api"
echo ""
echo "  3. API testi:"
echo "     curl http://localhost:5000/api/categories"
echo ""
echo "=============================================="
