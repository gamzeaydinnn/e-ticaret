# 🚀 Sunucuda Demo Kurye Hesabını Aktifleştirme

## 1️⃣ SSH ile Bağlan

PuTTY veya MobaXterm kullanarak:

```
Host: 31.186.24.78
User: root
```

## 2️⃣ Sunucuda Çalıştır (Sırasıyla)

### Adım 1: Proje Klasörüne Git

```bash
cd /home/eticaret
```

### Adım 2: Son Kodu Çek

```bash
git pull origin main
```

### Adım 3: API Container'ı Yeniden Başlat

```bash
docker-compose -f docker-compose.prod.yml restart api
```

### Adım 4: 15 Saniye Bekle (Container başlasın)

```bash
sleep 15
```

### Adım 5: Demo Kurye Seed Endpoint'ini Çağır

```bash
curl -X POST http://localhost:5000/api/courier/seed-demo
```

Çıktı şöyle olmalı:

```json
{ "message": "Demo kurye ve user başarıyla eklendi." }
```

### Adım 6: Log Kontrol Et

```bash
docker logs ecommerce-api-prod 2>&1 | tail -30
```

Şunu göreceksin:

```
info: Demo kurye eklendi: ahmett@courier.com
```

---

## 🧪 Test Et

Artık giriş yapabilirsin:

- 📧 **ahmett@courier.com**
- 🔐 **Ahmet.123**

**URL:** https://golkoygurme.com.tr/courier/login

---

## ❌ Hala Çalışmazsa - SQL ile Manuel Aktif Et

SSH'de çalıştır:

```bash
docker exec -it ecommerce-db-prod /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'YourStrong@Passw0rd' -Q "UPDATE Users SET IsActive = 1, EmailConfirmed = 1 WHERE Email = 'ahmett@courier.com'; UPDATE Couriers SET IsActive = 1 WHERE UserId IN (SELECT Id FROM Users WHERE Email = 'ahmett@courier.com'); SELECT u.Email, u.IsActive as UserActive, c.IsActive as CourierActive FROM Users u LEFT JOIN Couriers c ON c.UserId = u.Id WHERE u.Email = 'ahmett@courier.com';"
```

---

## 📝 Tek Komutta Tümü

```bash
cd /home/eticaret && git pull origin main && docker-compose -f docker-compose.prod.yml restart api && sleep 15 && curl -X POST http://localhost:5000/api/courier/seed-demo && docker logs ecommerce-api-prod 2>&1 | tail -30
```
