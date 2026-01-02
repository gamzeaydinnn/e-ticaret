#!/bin/bash
# Sunucuya Deploy Script
# Kullanım: ssh ile sunucuya bağlandıktan sonra bu komutları çalıştır

echo "🚀 Deploy işlemi başlıyor..."

# Proje dizinine git
cd ~/eticaret || { echo "❌ Proje dizini bulunamadı!"; exit 1; }

echo "📥 Git'ten son değişiklikleri çek..."
git fetch origin
git pull origin main

echo "🛑 Konteynerleri durdur..."
docker-compose -f docker-compose.prod.yml down

echo "🗄️ Seed data'yı veritabanına yükle..."
# SQL Server'ın hazır olmasını bekle
docker-compose -f docker-compose.prod.yml up -d sqlserver
sleep 15

# Seed script'i çalıştır (eğer daha önce çalıştırılmadıysa)
if [ -f "seed-products.sql" ]; then
    echo "📦 Ürünleri ve kategorileri veritabanına ekle..."
    cat seed-products.sql | docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_PASSWORD:-ECom1234}" -C
    echo "✅ Seed data yüklendi!"
fi

echo "🏗️ Docker image'larını yeniden oluştur..."
docker-compose -f docker-compose.prod.yml build --no-cache

echo "🚀 Tüm servisleri başlat..."
docker-compose -f docker-compose.prod.yml up -d

echo "⏳ Servislerin başlamasını bekle..."
sleep 20

echo "📊 Konteyner durumlarını kontrol et..."
docker-compose -f docker-compose.prod.yml ps

echo "✅ Deploy tamamlandı!"
echo ""
echo "📍 Kontrol Et:"
echo "   Frontend: http://31.186.24.78:3000"
echo "   API: http://31.186.24.78:5000/api/products"
echo ""
echo "📝 Logları görmek için:"
echo "   docker-compose -f docker-compose.prod.yml logs -f"
