#!/bin/bash
# Demo kurye hesabını sunucuda aktif et

echo "🔧 Demo kurye hesabı aktifleştiriliyor..."

# 1. Git pull
echo "📥 Kod güncelleniyor..."
cd /home/eticaret
git pull origin main

# 2. Backend container'ı yeniden başlat
echo "🐳 Backend yeniden başlatılıyor..."
docker-compose -f docker-compose.prod.yml restart api

# 3. 10 saniye bekle
echo "⏳ Container başlatılıyor (10 saniye)..."
sleep 10

# 4. Demo kurye endpoint'ini çağır
echo "👤 Demo kurye oluşturuluyor/aktifleştiriliyor..."
curl -X POST http://localhost:5000/api/courier/seed-demo

echo ""
echo "✅ İşlem tamamlandı!"
echo ""

# 5. Backend loglarını kontrol et
echo "📋 Backend logları (son 20 satır):"
docker logs ecommerce-api-prod 2>&1 | tail -20

echo ""
echo "🧪 Test için giriş yap:"
echo "   Email: ahmet@courier.com"
echo "   Şifre: Ahmet.123"
