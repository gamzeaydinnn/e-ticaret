# 🚀 SUNUCU SIFIRDAN DEPLOY - HIZLI REHBER

## 📋 ÖNCEKİ SORUN

`ecommerce/` klasörü gereksiz bir şekilde iç içe geçmiş (nested submodule) olarak vardı.
Bu durum sunucuda yanlış frontend kodunun çalışmasına neden oluyordu.

## ✅ ÇÖZÜM UYGULANDII

- `ecommerce/` klasörü yerel projeden tamamen silindi
- `.gitignore`'a eklendi (tekrar oluşmasını engeller)
- GitHub'a push edildi

---

## 🖥️ SUNUCU KOMUTLARI (SIFIRDAN)

### 1. SSH ile Bağlan

```bash
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
```

### 2. Eski Her Şeyi Temizle

```bash
cd ~
# Varsa eski konteynerleri durdur
docker stop $(docker ps -aq) 2>/dev/null || true

# Eski klasörleri sil
rm -rf ecommerce
rm -rf eticaret

# Docker temizliği
docker system prune -af
docker volume rm $(docker volume ls -q) 2>/dev/null || true
```

### 3. Projeyi Yeniden Çek

```bash
cd ~
git clone https://github.com/gamzeaydinnn/e-ticaret.git eticaret
cd ~/eticaret
```

### 4. .env Dosyasını Oluştur

```bash
cat > .env << 'EOF'
DB_PASSWORD=ECom1234
DB_PORT=1435
FRONTEND_PORT=3000
ASPNETCORE_ENVIRONMENT=Production
EOF
```

### 5. Docker Build ve Başlat

```bash
# Build (5-10 dakika sürer)
docker-compose -f docker-compose.prod.yml build --no-cache

# Başlat
docker-compose -f docker-compose.prod.yml up -d
```

### 6. Bekle ve Kontrol Et

```bash
# 30 saniye bekle
sleep 30

# Durumu kontrol et
docker-compose -f docker-compose.prod.yml ps

# API testi
curl http://localhost:5000/api/categories

# Log izle (sorun varsa)
docker-compose -f docker-compose.prod.yml logs -f
```

---

## 📍 ERİŞİM ADRESLERİ

| Servis   | Adres                        |
| -------- | ---------------------------- |
| Frontend | http://31.186.24.78:3000     |
| API      | http://31.186.24.78:5000/api |

---

## 🔧 SORUN GİDERME

### API 502/503 hatası veriyorsa:

```bash
docker-compose -f docker-compose.prod.yml logs api
```

### SQL Server bağlantı sorunu:

```bash
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "ECom1234" -C \
  -Q "SELECT name FROM sys.databases"
```

### Kategoriler görünmüyorsa:

```bash
# Seed data yükle
cat seed-products.sql | docker exec -i ecommerce-sql-prod \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

---

## ⚠️ ÖNEMLİ NOTLAR

1. **Sunucu klasör adı**: `~/eticaret` (ecommerce DEĞİL!)
2. **API Portu**: 5000 (container içinde de 5000)
3. **Frontend Portu**: 3000 (container içinde nginx 80)
4. **SQL Portu**: 1435 (dış) -> 1433 (iç)

---

**Son Güncelleme**: 2026-01-10
**Git Commit**: ecommerce submodule kaldırıldı
