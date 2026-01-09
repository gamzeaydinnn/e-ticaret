# 🚨 SUNUCUDA CONTAINER BAŞLAMAMA SORUNU - ÇÖZÜM

## ⚠️ SORUNLAR
1. **SQL Server container yok:** `ecommerce-sql-prod` bulunamıyor
2. **Seed data yüklenmedi:** Database boş (banner, kategori, ürün yok)
3. **404 hatası:** API çalışıyor ama veri yok

## ✅ ÇÖZÜM (SIRAYLA YAPTIRINIZ)

### ADIM 1: Mevcut Container'ları Durdur
```bash
cd /home/huseyinadm/eticaret
docker-compose -f docker-compose.prod.yml down -v
```

### ADIM 2: Container Durumunu Kontrol Et
```bash
docker ps -a
```
**Beklenen:** Hiç container olmacak

### ADIM 3: Tüm Veriyi Temizle
```bash
docker system prune -a -f
rm -rf logs/*
```

### ADIM 4: Tüm Image'ları Yeniden Oluştur (Başından)
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```
⏱️ **5-10 dakika bekleyin!**

### ADIM 5: Container'ları Başlat
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### ADIM 6: Container'ları Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```

**Beklenen Output:**
```
NAME                      IMAGE               STATUS
ecommerce-sql-prod        ...                 Up (healthy)
ecommerce-api-prod        ...                 Up
ecommerce-frontend-prod   ...                 Up
```

### ADIM 7: SQL Server'ın Hazır Olmasını Bekle (1-2 dakika)
```bash
sleep 120
```

### ADIM 8: API Loglarını Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml logs api --tail=50
```

**Beklenen:** Migration logları ve seed işlemleri

### ADIM 9: Seed Data Kontrol Et
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C -Q "SELECT COUNT(*) as [Ürün] FROM ECommerceDb.dbo.Products"
```

**Beklenen:** 50+ sayı

### ADIM 10: API Test Et
```bash
curl http://localhost:5000/api/categories
```

**Beklenen:** JSON dizi (kategoriler)

---

## 🔴 EĞER SORUN DEVAM EDERSE

### Kontrol 1: Docker Daemon Çalışıyor mu?
```bash
docker ps
```

### Kontrol 2: Container Log'larını Ayrıntılı Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml logs sqlserver --tail=100
```

### Kontrol 3: SQL Server Memory/CPU Sorunu
```bash
docker stats
```

### Kontrol 4: Network Sorunu
```bash
docker-compose -f docker-compose.prod.yml ps
docker network ls
```

### Çözüm 5: Tamamen Yeni Baştan (Son Resort)
```bash
# Tüm veriyi sil
docker-compose -f docker-compose.prod.yml down -v
docker system prune -a -f

# Kodu güncelleyip rebuild yap
git pull origin main
docker-compose -f docker-compose.prod.yml build --no-cache --pull
docker-compose -f docker-compose.prod.yml up -d

# Bekle
sleep 120

# Logları kontrol et
docker-compose -f docker-compose.prod.yml logs api -f
```

---

## 📋 KONTROL LİSTESİ (Hepsi "✅" olmalı)

- [ ] `docker-compose ps` tüm container'ları gösteriyor (Up)
- [ ] `curl localhost:5000/api/health` - 200 OK
- [ ] `curl localhost:5000/api/categories` - JSON dizi
- [ ] `curl localhost:3000` - HTML
- [ ] Veritabanı sorgusu sonuç veriyor

---

## 🆘 HIZLI KOPYALA-YAPISTIR

Sunucuda tek satır çalıştırın:
```bash
cd /home/huseyinadm/eticaret && docker-compose -f docker-compose.prod.yml down -v && docker system prune -a -f && docker-compose -f docker-compose.prod.yml build --no-cache && docker-compose -f docker-compose.prod.yml up -d && sleep 120 && docker-compose -f docker-compose.prod.yml logs api -f
```

Sonunda "All seed operations completed successfully" görünce CTRL+C ile çık.

---

**Sonra kontrolleri yapın ve bize bildir!**
