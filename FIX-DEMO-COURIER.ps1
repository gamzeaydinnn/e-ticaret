# Demo Kurye Hesabı Aktifleştirme - Sunucu Komutları

# 1. Commit + Push (Lokal)
cd C:\Users\GAMZE\Desktop\eticaret
git add .
git commit -m "fix: Demo kurye hesabı aktifleştirme ve email düzeltme"
git push origin main

# 2. Sunucuya SSH ile bağlan ve komutları çalıştır
ssh root@31.186.24.78 "cd /home/eticaret && git pull origin main && docker-compose -f docker-compose.prod.yml restart api && sleep 10 && curl -X POST http://localhost:5000/api/courier/seed-demo && docker logs ecommerce-api-prod 2>&1 | tail -30"

# Eğer yukarıdaki uzun komut çalışmazsa, SSH'ye bağlan ve tek tek çalıştır:
# ssh root@31.186.24.78

# Sunucuda çalıştır:
# cd /home/eticaret
# git pull origin main
# docker-compose -f docker-compose.prod.yml restart api
# sleep 10
# curl -X POST http://localhost:5000/api/courier/seed-demo
# docker logs ecommerce-api-prod 2>&1 | tail -30
