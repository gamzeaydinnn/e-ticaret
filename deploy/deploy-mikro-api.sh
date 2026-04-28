#!/bin/bash
# ============================================================================
# MİKRO API DEPLOY SCRİPT - Sunucuda çalıştırılacak
# 
# Kullanım: 
#   chmod +x deploy-mikro-api.sh
#   ./deploy-mikro-api.sh
# ============================================================================

set -e  # Hata durumunda dur

docker_cmd() {
    if docker info > /dev/null 2>&1; then
        docker "$@"
    else
        sudo docker "$@"
    fi
}

load_env_file() {
    local env_path="$1"
    local line
    local key
    local value

    while IFS= read -r line || [ -n "$line" ]; do
        line="${line%$'\r'}"

        case "$line" in
            '' | '#'* )
                continue
                ;;
        esac

        key="${line%%=*}"
        value="${line#*=}"

        if [ "$key" = "$line" ]; then
            continue
        fi

        if [ "${value#\"}" != "$value" ] && [ "${value%\"}" != "$value" ]; then
            value="${value#\"}"
            value="${value%\"}"
        elif [ "${value#\'}" != "$value" ] && [ "${value%\'}" != "$value" ]; then
            value="${value#\'}"
            value="${value%\'}"
        fi

        export "$key=$value"
    done < "$env_path"
}

compose() {
    if docker info > /dev/null 2>&1 && docker compose version > /dev/null 2>&1; then
        docker compose "$@"
    elif sudo docker compose version > /dev/null 2>&1; then
        sudo docker compose "$@"
    else
        sudo docker-compose "$@"
    fi
}

echo "=========================================="
echo "🔄 MİKRO API ENTEGRASYON DEPLOY"
echo "=========================================="
echo ""

# Scriptin bulundugu repodan proje kokunu bul
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Proje dizinine git
cd "$PROJECT_ROOT"

ENV_FILE=".env"
SANITIZED_ENV_FILE="$(mktemp)"
cleanup() {
    rm -f "$SANITIZED_ENV_FILE"
}
trap cleanup EXIT

if [ ! -f "$ENV_FILE" ]; then
    echo "❌ .env bulunamadı. Mikro VPN ve Mikro SQL ayarlari olmadan deploy devam etmeyecek."
    exit 1
fi

# Windows'tan gelen UTF-8 BOM shell source islemini bozmasin diye ilk satirdan temizle.
sed '1s/^\xEF\xBB\xBF//' "$ENV_FILE" > "$SANITIZED_ENV_FILE"

load_env_file "$SANITIZED_ENV_FILE"

vpn_config_path="${VPN_CONFIG_PATH:-./vpn.ovpn}"
case "$vpn_config_path" in
    /*) ;;
    *) vpn_config_path="$PROJECT_ROOT/${vpn_config_path#./}" ;;
esac

# 1. VPN config dosyasını kontrol et
echo "📍 Adım 1/7: VPN konfigürasyonu kontrol ediliyor..."
if [ ! -s "$vpn_config_path" ]; then
    echo "❌ VPN config dosyası bulunamadı veya boş: $vpn_config_path"
    echo "   Gerçek OpenVPN config dosyasını bu path'e koymadan deploy devam etmez."
    exit 1
fi
echo "✅ VPN config hazır: $vpn_config_path"

if grep -q '^MIKRO_SQL_USER=CHANGE_ME$' .env || grep -q '^MIKRO_SQL_PASSWORD=CHANGE_ME$' .env; then
    echo "❌ .env icindeki MIKRO_SQL_USER / MIKRO_SQL_PASSWORD hala CHANGE_ME durumda."
    echo "   Gercek Mikro SQL kullanici bilgilerini yazmadan deploy etmeyin."
    exit 1
fi

if grep -q '^MIKRO_API_KEY=CHANGE_ME$' .env \
    || grep -q '^MIKRO_FIRMA_KODU=CHANGE_ME$' .env \
    || grep -q '^MIKRO_KULLANICI_KODU=CHANGE_ME$' .env \
    || grep -q '^MIKRO_SIFRE=CHANGE_ME$' .env; then
    echo "❌ .env icindeki Mikro API kimlik alanlari eksik."
    echo "   MIKRO_API_KEY, MIKRO_FIRMA_KODU, MIKRO_KULLANICI_KODU ve MIKRO_SIFRE gercek degerler olmali."
    exit 1
fi

# 2. Mevcut container'ları durdur
echo ""
echo "📍 Adım 2/7: Container'lar durduruluyor..."
compose -f docker-compose.prod.yml down || true

# 3. Backend image'ı yeniden oluştur
echo ""
echo "📍 Adım 3/7: Gerekli image'lar build ediliyor..."
compose -f docker-compose.prod.yml build api mikro-api-relay mikro-sql-relay --no-cache

# 4. Container'ları başlat
echo ""
echo "📍 Adım 4/7: Container'lar başlatılıyor..."
compose -f docker-compose.prod.yml up -d

# 5. VPN sidecar erişimini kontrol et
echo ""
echo "📍 Adım 5/7: VPN sidecar erişimi kontrol ediliyor..."
sleep 10  # Container'ların başlamasını bekle

if docker_cmd exec mikro-vpn wget -q -O- http://127.0.0.1:8000/v1/openvpn/status > /dev/null 2>&1; then
    echo "  ✅ mikro-vpn kontrol sunucusu yanıt veriyor"
else
    echo "  ⚠️  mikro-vpn kontrol sunucusuna erişilemedi"
fi

# 6. Sağlık kontrolü
echo ""
echo "📍 Adım 6/7: Uygulama sağlık kontrolleri yapılıyor..."

# Backend health check
echo "  → Backend health check..."
for i in {1..30}; do
    if curl -s http://localhost:5000/api/health > /dev/null 2>&1; then
        echo "  ✅ Backend çalışıyor"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ⚠️  Backend yanıt vermiyor!"
    fi
    sleep 2
done

# Frontend health check
echo "  → Frontend health check..."
if curl -s http://localhost:3000 > /dev/null 2>&1; then
    echo "  ✅ Frontend çalışıyor"
else
    echo "  ⚠️  Frontend yanıt vermiyor!"
fi

# 7. Mikro bağlantı testi
echo ""
echo "📍 Adım 7/7: Mikro API bağlantı testi..."
docker_cmd exec ecommerce-api-prod curl -k -s https://mikro-vpn:${MIKRO_API_RELAY_PORT:-8084}/Api/APIMethods/HealthCheck > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "  ✅ API container'ından mikro-vpn relay üzerinden Mikro API'ye erişim başarılı"
else
    echo "  ⚠️  mikro-vpn relay üzerinden Mikro API'ye erişilemiyor!"
    echo "      Muhtemel çözümler:"
    echo "      1. vpn.ovpn dosyasının gecerli oldugunu kontrol edin"
    echo "      2. .env icindeki MIKRO_API_TARGET_HOST / PORT degerlerini kontrol edin"
    echo "      3. MikroSettings__IgnoreSslErrors ayarini kapatmadiğinizdan emin olun"
fi

# Sonuç
echo ""
echo "=========================================="
echo "📊 DEPLOY TAMAMLANDI"
echo "=========================================="
echo ""
echo "Container durumları:"
compose -f docker-compose.prod.yml ps
echo ""
echo "Mikro API loglarını görmek için:"
echo "  docker logs ecommerce-api-prod 2>&1 | grep -i mikro"
echo "  docker logs mikro-vpn"
echo ""
echo "Hangfire dashboard:"
echo "  https://golkoygurme.com.tr/hangfire"
echo ""
