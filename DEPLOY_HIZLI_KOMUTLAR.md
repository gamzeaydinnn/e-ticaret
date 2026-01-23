# ⚡ SUNUCUYA DEPLOY - HIZLI KOMUTLAR

## 🚀 TEK TEK KOPYALA YAPIŞTIR

### 1. SSH Bağlantısı

```bash
ssh root@31.186.24.78
```

---

### 2. Proje Dizinine Git

```bash
cd /home/eticaret
```

---

### 3. Güncellemeleri Çek

```bash
git pull origin main
```

---

### 4. Container'ları Durdur

```bash
docker-compose -f docker-compose.prod.yml down
```

---

### 5. Container'ları Yeniden Başlat (Build ile)

```bash
docker-compose -f docker-compose.prod.yml up -d --build
```

**⏱️ Süre: 3-5 dakika**

---

### 6. Log'ları İzle (CTRL+C ile çık)

```bash
docker logs -f ecommerce-api-prod
```

**ARANACAK MESAJLAR:**

```
ℹ️ IdentitySeeder: Roller zaten mevcut, seed ATLANILIYOR
ℹ️ ProductSeeder: Mevcut veriler var, seed ATLANILIYOR
✅✅✅ TÜM SEED İŞLEMLERİ BAŞARIYLA TAMAMLANDI!
```

---

### 7. Health Check

```bash
curl http://localhost:5000/health
```

**Beklenen:** `Healthy`

---

### 8. Kupon API Test

```bash
curl http://localhost:5000/api/coupon/active
```

**Beklenen:** JSON response

---

### 9. Container Durumu

```bash
docker ps
```

**Beklenen:** 3 container çalışıyor olmalı

---

### 10. Frontend Test

Tarayıcıda aç:

```
http://31.186.24.78:3000
```

---

## 📊 EK KOMUTLAR

### Container Log'larını Göster (Son 100 satır)

```bash
docker logs --tail 100 ecommerce-api-prod
```

---

### Tüm Container'ların Durumu

```bash
docker ps -a
```

---

### Volume Listesi

```bash
docker volume ls
```

---

### Disk Kullanımı

```bash
df -h
```

---

### Uploads Klasörü

```bash
ls -lah /home/eticaret/uploads/
```

---

### SQL Server Bağlantısı

```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ECom1234' -C
```

**SQL Komutları:**

```sql
-- Tabloları listele
SELECT name FROM sys.tables ORDER BY name;
GO

-- Kuponları listele
SELECT Id, Code, Type, Value, IsActive FROM Coupons;
GO

-- Çıkış
EXIT
```

---

## 🔄 HER ŞEYİ TEK SEFERDE (COPY-PASTE)

```bash
cd /home/eticaret && \
git pull origin main && \
docker-compose -f docker-compose.prod.yml down && \
docker-compose -f docker-compose.prod.yml up -d --build && \
echo "✅ Deploy tamamlandı! Log'ları izlemek için: docker logs -f ecommerce-api-prod"
```

---

## ✅ BAŞARILI DEPLOY KONTROL

- [ ] Git pull başarılı
- [ ] Container'lar başladı (3 adet)
- [ ] Log'larda "seed ATLANILIYOR" mesajı var
- [ ] Health check: `Healthy`
- [ ] Frontend açılıyor: http://31.186.24.78:3000
- [ ] Admin panel açılıyor: http://31.186.24.78:3000/admin

---

## 🎉 TAMAMLANDI!

Artık sunucunuz güncel ve çalışıyor! 🚀
