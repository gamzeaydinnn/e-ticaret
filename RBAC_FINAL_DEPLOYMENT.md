# 🚀 RBAC Sistemi Final Deployment Rehberi

## 📋 Bu Güncelleme İçeriği

### ✅ Backend Değişiklikleri

1. **JWT Token Fix**: `JwtTokenHelper.cs` - Duplicate `NameIdentifier` claim sorunu çözüldü
2. **RefreshToken Index Fix**: `ECommerceDbContext.cs` - UNIQUE index `Token` → `HashedToken` taşındı
3. **Migration**: `20260112215737_FixRefreshTokenIndex.cs` eklendi
4. **Admin Credentials**: `admin@admin.com` / `admin123`

### ✅ Frontend Değişiklikleri

1. **AdminUsers.jsx**: RBAC İzin Matrisi tablosu eklendi
2. Tüm roller için erişim kontrolleri görsel olarak gösteriliyor

---

## 🔧 SUNUCU DEPLOYMENT ADIMLARI

### Adım 1: SSH Bağlantısı

```bash
ssh root@31.186.24.78
cd /var/www/ecommerce
```

### Adım 2: Git Pull (Güncellemeleri Çek)

```bash
git pull origin main
```

### Adım 3: Backend Database Migration

```bash
# Backend container içine gir
docker exec -it ecommerce-api-prod sh

# Migration uygula
cd /app
dotnet ef database update

# Container'dan çık
exit
```

### Adım 4: Container'ları Yeniden Oluştur

```bash
# Tüm container'ları durdur
docker-compose -f docker-compose.prod.yml down

# Image'ları yeniden build et (cache olmadan)
docker-compose -f docker-compose.prod.yml build --no-cache

# Container'ları başlat
docker-compose -f docker-compose.prod.yml up -d
```

### Adım 5: Veritabanı Seed Kontrolü

```bash
# Backend loglarını kontrol et
docker logs ecommerce-api-prod --tail 50 | grep -i "seed\|rbac\|permission"

# RBAC verileri seed edildi mi kontrol et
docker exec -it ecommerce-db-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -d ECommerceDb \
  -Q "SELECT COUNT(*) AS RoleCount FROM Roles; SELECT COUNT(*) AS PermissionCount FROM Permissions;"
```

### Adım 6: API Test

```bash
# Login test
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"admin123"}'

# Token al ve permissions test et
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@admin.com","password":"admin123"}' | jq -r '.Token')

curl -X GET http://localhost:5000/api/auth/permissions \
  -H "Authorization: Bearer $TOKEN"
```

### Adım 7: Frontend Kontrolü

```bash
# Frontend build kontrolü
docker logs ecommerce-frontend-prod --tail 30

# Nginx config kontrolü
docker exec ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf | grep admin
```

---

## 🌐 HOST NGINX KONTROLÜ

Eğer host nginx kullanılıyorsa config kontrolü:

```bash
# Nginx config test
sudo nginx -t

# Config içeriği
sudo cat /etc/nginx/sites-available/golkoygurme

# Nginx reload
sudo systemctl reload nginx
```

### Örnek Host Nginx Config (Gerekirse)

```nginx
server {
    listen 80;
    server_name golkoygurme.com.tr www.golkoygurme.com.tr;

    location / {
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }

    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
        client_max_body_size 10M;
    }

    location /uploads {
        proxy_pass http://localhost:5000/uploads;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        expires 1d;
        add_header Cache-Control "public, max-age=86400";
    }
}
```

---

## ✅ DOĞRULAMA CHECKLIST

### Backend

- [ ] Migration başarıyla uygulandı
- [ ] Admin login çalışıyor (admin@admin.com / admin123)
- [ ] `/api/auth/permissions` endpoint 200 dönüyor
- [ ] Permissions listesi geliyor (60+ izin)
- [ ] RefreshToken hatası yok

### Frontend

- [ ] Admin panel açılıyor
- [ ] Kullanıcılar sayfası görünüyor
- [ ] RBAC İzin Matrisi tablosu görünüyor
- [ ] Rol değiştirme çalışıyor

### Database

- [ ] Roles tablosu: 5 kayıt
- [ ] Permissions tablosu: 60+ kayıt
- [ ] RolePermissions tablosu: İlişkiler mevcut
- [ ] RefreshTokens tablosunda hata yok

---

## 🔥 HIZLI DEPLOY KOMUTU (Tek Satır)

```bash
cd /var/www/ecommerce && \
git pull origin main && \
docker-compose -f docker-compose.prod.yml down && \
docker-compose -f docker-compose.prod.yml build --no-cache && \
docker-compose -f docker-compose.prod.yml up -d && \
sleep 30 && \
docker logs ecommerce-api-prod --tail 20
```

---

## 📞 SORUN GİDERME

### Hata: "Cannot insert duplicate key" (RefreshToken)

```bash
# Database'i sıfırla (DİKKAT: Veriler silinir!)
docker exec -it ecommerce-db-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' \
  -Q "DROP DATABASE ECommerceDb; CREATE DATABASE ECommerceDb;"

# Backend'i restart et (migration otomatik çalışır)
docker restart ecommerce-api-prod
```

### Hata: 401 Unauthorized (Permissions)

```bash
# Backend loglarını kontrol et
docker logs ecommerce-api-prod --tail 100 | grep -i "jwt\|auth\|401"

# Token'ı decode et
echo "TOKEN_BURAYA" | cut -d'.' -f2 | base64 -d 2>/dev/null | jq
```

### Hata: Frontend 404

```bash
# Nginx config kontrolü
docker exec ecommerce-frontend-prod cat /etc/nginx/conf.d/default.conf

# Build dosyaları kontrolü
docker exec ecommerce-frontend-prod ls -la /usr/share/nginx/html/
```

---

## 📅 Güncelleme Tarihi: 13 Ocak 2026

**Yapılan Değişiklikler:**

- JWT Token claim düzeltmesi (NameIdentifier duplicate fix)
- RefreshToken UNIQUE index düzeltmesi
- Frontend RBAC izin matrisi tablosu
- Admin credentials güncelleme
