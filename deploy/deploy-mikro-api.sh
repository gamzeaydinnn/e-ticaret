#!/bin/bash
# ============================================================================
# MİKRO API DEPLOY SCRİPT - Sunucuda çalıştırılacak
# 
# Kullanım: 
#   chmod +x deploy-mikro-api.sh
#   ./deploy-mikro-api.sh
# ============================================================================

set -e  # Hata durumunda dur

echo "=========================================="
echo "🔄 MİKRO API ENTEGRASYON DEPLOY"
echo "=========================================="
echo ""

# Proje dizinine git
cd /root/eticaret

# 1. Mikro API durumunu kontrol et
echo "📍 Adım 1/6: Mikro API durumu kontrol ediliyor..."
if curl -k -s https://localhost:8094/Api/APIMethods/HealthCheck > /dev/null 2>&1; then
    echo "✅ Mikro API erişilebilir (port 8094)"
else
    echo "⚠️  Mikro API erişilemiyor! Port 8094'ü kontrol edin."
    echo "    Devam edilsin mi? (y/n)"
    read -r continue_deploy
    if [ "$continue_deploy" != "y" ]; then
        echo "Deploy iptal edildi."
        exit 1
    fi
fi

# 2. Mevcut container'ları durdur
echo ""
echo "📍 Adım 2/6: Container'lar durduruluyor..."
docker-compose -f docker-compose.prod.yml down || true

# 3. Backend image'ı yeniden oluştur
echo ""
echo "📍 Adım 3/6: Backend image build ediliyor..."
docker-compose -f docker-compose.prod.yml build api --no-cache

# 4. Container'ları başlat
echo ""
echo "📍 Adım 4/6: Container'lar başlatılıyor..."
docker-compose -f docker-compose.prod.yml up -d

# 5. Sağlık kontrolü
echo ""
echo "📍 Adım 5/6: Sağlık kontrolleri yapılıyor..."
sleep 10  # Container'ların başlamasını bekle

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

# 6. Mikro bağlantı testi
echo ""
echo "📍 Adım 6/6: Mikro API bağlantı testi..."
docker exec ecommerce-api-prod curl -k -s https://host.docker.internal:8094/Api/APIMethods/HealthCheck > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "  ✅ Container'dan Mikro API'ye erişim başarılı"
else
    echo "  ⚠️  Container'dan Mikro API'ye erişilemiyor!"
    echo "      Muhtemel çözümler:"
    echo "      1. extra_hosts ayarını kontrol edin"
    echo "      2. Firewall kurallarını kontrol edin"
    echo "      3. Mikro API SSL sertifikasını kontrol edin"
fi

# Sonuç
echo ""
echo "=========================================="
echo "📊 DEPLOY TAMAMLANDI"
echo "=========================================="
echo ""
echo "Container durumları:"
docker-compose -f docker-compose.prod.yml ps
echo ""
echo "Mikro API loglarını görmek için:"
echo "  docker logs ecommerce-api-prod 2>&1 | grep -i mikro"
echo ""
echo "Hangfire dashboard:"
echo "  https://golkoygurme.com.tr/hangfire"
echo ""
