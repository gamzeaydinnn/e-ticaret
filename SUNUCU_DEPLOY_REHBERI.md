# SUNUCUYA DEPLOY REHBERİ

## Bağlantı Bilgileri

- **IP:** 31.186.24.78
- **User:** huseyinadm
- **Pass:** Passwd1122FFGG

---

## ADIM 1: Sunucuya Bağlan

### Windows PowerShell (Admin yetkisi ile):

```powershell
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
```

### PuTTY ile:

1. PuTTY'yi aç
2. Host: `31.186.24.78`, Port: `22`
3. Open'a tıkla
4. Username: `huseyinadm`
5. Password: `Passwd1122FFGG`

---

## ADIM 2: Proje Dizinine Git

```bash
cd ~/eticaret
# veya
cd /home/huseyinadm/eticaret
```

---

## ADIM 3: Son Değişiklikleri Çek

```bash
git pull origin main
```

---

## ADIM 4: Konteynerleri Durdur

```bash
docker-compose -f docker-compose.prod.yml down
```

---

## ADIM 5: Veritabanı Seed Data (İlk Kez Çalıştırılacak)

### SQL Server'ı Başlat:

```bash
docker-compose -f docker-compose.prod.yml up -d sqlserver
sleep 15
```

### Seed Data'yı Yükle:

```bash
cat seed-products.sql | docker exec -i ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

**NOT:** Eğer daha önce seed data yüklendiyse, bu adımı atlayabilirsin veya önce tabloları temizle:

```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "DELETE FROM ECommerceDb.dbo.Products; DELETE FROM ECommerceDb.dbo.Categories;"
```

---

## ADIM 6: Docker Image'larını Yeniden Oluştur (Opsiyonel - kodda değişiklik varsa)

```bash
# Sadece API değişti ise:
docker-compose -f docker-compose.prod.yml build api

# Sadece Frontend değişti ise:
docker-compose -f docker-compose.prod.yml build frontend

# Her ikisi de değişti ise:
docker-compose -f docker-compose.prod.yml build
```

---

## ADIM 7: Servisleri Başlat

```bash
docker-compose -f docker-compose.prod.yml up -d
```

---

## ADIM 8: Durumu Kontrol Et

### Konteyner Durumları:

```bash
docker-compose -f docker-compose.prod.yml ps
```

### Logları İzle:

```bash
# Tüm servislerin logları:
docker-compose -f docker-compose.prod.yml logs -f

# Sadece API logları:
docker logs -f ecommerce-api-prod

# Sadece Frontend logları:
docker logs -f ecommerce-frontend-prod
```

### API Test:

```bash
curl http://localhost:5000/api/products
```

---

## ADIM 9: Tarayıcıdan Kontrol Et

- **Frontend:** http://31.186.24.78:3000
- **API:** http://31.186.24.78:5000/api/products

---

## HIZLI DEPLOY (Değişiklikleri hızlıca deploy et)

```bash
cd ~/eticaret && \
git pull origin main && \
docker-compose -f docker-compose.prod.yml down && \
docker-compose -f docker-compose.prod.yml up -d --build && \
docker-compose -f docker-compose.prod.yml ps
```

---

## SORUN GİDERME

### Konteyner Yeniden Başlatma:

```bash
docker restart ecommerce-api-prod
docker restart ecommerce-frontend-prod
docker restart ecommerce-sql-prod
```

### Logları Kontrol Et:

```bash
docker logs ecommerce-api-prod --tail 50
```

### Veritabanı Bağlantısını Test Et:

```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) FROM ECommerceDb.dbo.Products"
```

### Disk Alanını Temizle:

```bash
docker system prune -a --volumes
```

---

## ÖNEMLİ NOTLAR

1. **İlk Deploy:** Seed data'yı mutlaka yükle (ADIM 5)
2. **Kod Değişikliği:** Build adımını (ADIM 6) atla**ma**
3. **Migration Değişikliği:** Veritabanını sıfırla veya migration'ları manuel çalıştır
4. **Port Kontrolü:** 3000 ve 5000 portlarının açık olduğundan emin ol

---

## FIREWALL AYARLARI (Gerekirse)

```bash
# Port 3000 aç (Frontend)
sudo ufw allow 3000/tcp

# Port 5000 aç (API)
sudo ufw allow 5000/tcp

# Durumu kontrol et
sudo ufw status
```

---

## YEDEKLEME

### Veritabanı Yedeği Al:

```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "BACKUP DATABASE ECommerceDb TO DISK = '/backups/ECommerceDb_$(date +%Y%m%d_%H%M%S).bak'"
```

### Yedekleri Listele:

```bash
ls -lh ~/eticaret/backups/
```
