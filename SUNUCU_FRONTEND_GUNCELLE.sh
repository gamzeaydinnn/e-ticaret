#!/bin/bash
# =============================================================================
# SUNUCU PORT 3000 VERİ TRANSFER KOMUTLARI
# =============================================================================
# Yerel localhost:3000 çalışan kodu sunucuya taşımak için komutlar
# Kullanım: Sunucu üzerinde bu komutları çalıştır
#
# SENARYO:
# 1. Yerel Windows PC'de frontend kod çalışıyor (localhost:3000)
# 2. Sunucudaki eski/yanlış kodu yeni kodla değiştir
# 3. Sunucuda frontend rebuild et ve başlat
# =============================================================================

set -e

# Renkli çıktı
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
echo "  SUNUCU FRONTEND GÜNCELLEME"
echo "  (Port 3000 verisini taşıma)"
echo "=============================================="
echo ""

# ADIM 1: Sunucudaki frontend konteynerini durdur
log_info "ADIM 1: Frontend konteynerini durduruluyor..."
cd ~/eticaret
docker-compose -f docker-compose.prod.yml stop frontend 2>/dev/null || true
sleep 5
log_success "Frontend durduruldu"

# ADIM 2: Git'ten son kodu çek
log_info "ADIM 2: GitHub'dan son kod çekiliyor..."
git fetch origin
git pull origin main
log_success "Son kod indirildi"

# ADIM 3: Frontend image'ını yeniden build et
log_info "ADIM 3: Frontend Docker image'ı yeniden build ediliyor..."
docker-compose -f docker-compose.prod.yml build --no-cache frontend
log_success "Frontend build tamamlandı"

# ADIM 4: Frontend konteynerini başlat
log_info "ADIM 4: Frontend başlatılıyor..."
docker-compose -f docker-compose.prod.yml up -d frontend
sleep 10
log_success "Frontend başlatıldı"

# ADIM 5: Durum kontrolü
echo ""
echo "=============================================="
echo "  DURUM KONTROL"
echo "=============================================="
echo ""

log_info "Frontend konteyner durumu:"
docker-compose -f docker-compose.prod.yml ps frontend

echo ""
log_info "Frontend port kontrolü..."
FRONTEND_HTTP=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:3000 2>/dev/null || echo "000")

if [ "$FRONTEND_HTTP" = "200" ]; then
    log_success "Frontend çalışıyor! (HTTP 200)"
else
    log_warning "Frontend yanıt vermedi (HTTP $FRONTEND_HTTP)"
fi

echo ""
echo "=============================================="
echo "  ERİŞİM"
echo "=============================================="
echo ""
echo "  🌐 Frontend: http://31.186.24.78:3000"
echo "  🔍 API: http://31.186.24.78:5000/api"
echo ""
echo "  Logları izle:"
echo "    docker-compose -f docker-compose.prod.yml logs -f frontend"
echo ""
echo "=============================================="
