#!/bin/bash

# ============================================================
# E-TİCARET SUNUCU DEPLOYMENT SCRIPT
# Son Güncelleme: 13 Ocak 2026
# RBAC Sistemi ve JWT Fix Deployment
# ============================================================

echo "🚀 SUNUCU DEPLOYMENT BAŞLANIYOR..."
echo ""

# 1. Proje dizinine git
cd /var/www/ecommerce || exit 1
echo "✅ Proje dizini: $(pwd)"

# 2. Git'ten güncel kodu çek
echo ""
echo "📥 Git'ten kodlar çekiliyor..."
git pull origin main
if [ $? -ne 0 ]; then
    echo "❌ Git pull başarısız!"
    exit 1
fi
echo "✅ Kodlar güncellendi"

# 3. Container'ları durdur
echo ""
echo "🛑 Container'lar durduruluyor..."
docker-compose -f docker-compose.prod.yml down
echo "✅ Container'lar durduruldu"

# 4. Image'ları rebuild et
echo ""
echo "🔨 Docker image'ları rebuild ediliyor (bu 2-3 dakika sürebilir)..."
docker-compose -f docker-compose.prod.yml build --no-cache
if [ $? -ne 0 ]; then
    echo "❌ Build başarısız!"
    exit 1
fi
echo "✅ Image'lar hazır"

# 5. Container'ları başlat
echo ""
echo "▶️  Container'lar başlatılıyor..."
docker-compose -f docker-compose.prod.yml up -d
if [ $? -ne 0 ]; then
    echo "❌ Container başlatılamadı!"
    exit 1
fi
echo "✅ Container'lar başlatıldı"

# 6. Başlatılmayı bekleme
echo ""
echo "⏳ Servisler başlatılıyor (30 saniye bekleniyor)..."
sleep 30

# 7. Container durumunu kontrol et
echo ""
echo "📊 Container Durumları:"
docker ps

# 8. Backend loglarını kontrol et
echo ""
echo "📋 Backend Logları (son 50 satır):"
docker logs ecommerce-api-prod --tail 50

# 9. Frontend loglarını kontrol et
echo ""
echo "📋 Frontend Logları (son 20 satır):"
docker logs ecommerce-frontend-prod --tail 20

# 10. Veritabanı seed kontrolü
echo ""
echo "🗄️  Veritabanı Kontrol Ediliyor..."
docker exec ecommerce-db-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C \
  -Q "SELECT COUNT(*) as RoleCount FROM Roles; SELECT COUNT(*) as PermissionCount FROM Permissions;" 2>/dev/null

echo ""
echo "✅ DEPLOYMENT TAMAMLANDI!"
echo ""
echo "🔗 Erişim Bilgileri:"
echo "   Frontend: http://31.186.24.78:3000"
echo "   Backend: http://31.186.24.78:5000"
echo "   Admin Email: admin@admin.com"
echo "   Admin Şifre: admin123"
echo ""
