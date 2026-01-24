# 🚀 SUNUCUYA DEPLOY KOMUTLARI

## 📌 SUNUCU BİLGİLERİ
```
IP: 31.186.24.78
Port: 22
Kullanıcı: huseyinadm
Şifre: Passwd1122FFGG
Proje Dizini: /home/huseyinadm/eticaret
```

---

## 🎯 DEPLOY ADIMLAR (Sırayla Çalıştır)

### ADIM 1: LOkal Kodları Git'e Puşla
```powershell
# Local bilgisayarında çalıştır
cd C:\Users\GAMZE\Desktop\eticaret
git add .
git commit -m "Deploy: Mailjet integration ve ProductDetailModal"
git push origin main
```

### ADIM 2: SSH ile Sunucuya Bağlan
```powershell
# PowerShell'de
ssh huseyinadm@31.186.24.78
# Şifre: Passwd1122FFGG
```

### ADIM 3: Sunucuda - Projeye Git
```bash
cd /home/huseyinadm/eticaret
```

### ADIM 4: Sunucuda - Kodu Güncelle
```bash
git pull origin main
```

### ADIM 5: Sunucuda - Eski Deployment'ı Temizle
```bash
docker-compose -f docker-compose.prod.yml down
docker rmi ecommerce-frontend:latest ecommerce-api:latest 2>/dev/null || true
docker image prune -f
```

### ADIM 6: Sunucuda - Docker Build
```bash
docker-compose -f docker-compose.prod.yml build --no-cache
```

### ADIM 7: Sunucuda - Container'ları Başlat
```bash
docker-compose -f docker-compose.prod.yml up -d
```

### ADIM 8: Sunucuda - Container Durumunu Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```
**Beklenen:** Tüm servislerin "Up" durumda olması

### ADIM 9: Sunucuda - API Loglarını Takip Et
```bash
docker-compose -f docker-compose.prod.yml logs api -f
```
**Çıkmak için:** CTRL+C

### ADIM 10: Sunucuda - Frontend Loglarını Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml logs frontend -f
```
**Çıkmak için:** CTRL+C

### ADIM 11: Sunucuda - Veritabanı Migrasyonunun Tamamlandığını Kontrol Et
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "ECom1234" -C
```

SQL promptunda:
```sql
SELECT COUNT(*) as [Ürün Sayısı] FROM ECommerceDb.dbo.Products;
GO
SELECT COUNT(*) as [Kategori Sayısı] FROM ECommerceDb.dbo.Categories;
GO
EXIT
```

### ADIM 12: Tarayıcıda Test Et
```
https://golkoygurme.com.tr
```

---

## ⚡ HIZLI SINGLE-LINE KOMUTLAR (Tümü Bir Seferde)

Eğer güvenilir alandasınız:

```bash
# SSH'da
cd /home/huseyinadm/eticaret && git pull origin main && docker-compose -f docker-compose.prod.yml down && docker-compose -f docker-compose.prod.yml build --no-cache && docker-compose -f docker-compose.prod.yml up -d
```

---

## 🔧 SORUN GIDERMELERİ

### API Çökmüş?
```bash
docker-compose -f docker-compose.prod.yml logs api --tail=100
```

### Frontend Çökmüş?
```bash
docker-compose -f docker-compose.prod.yml logs frontend --tail=100
```

### Veritabanı Bağlantısı Sorunu?
```bash
docker-compose -f docker-compose.prod.yml logs database --tail=50
```

### Container'ları Yeniden Başlat
```bash
docker-compose -f docker-compose.prod.yml restart
```

### Tüm Sistemin Durumunu Kontrol Et
```bash
docker-compose -f docker-compose.prod.yml ps
```

---

## 📊 ÖNEMLİ KONTROL NOKTALAR

✅ Git push başarılı  
✅ SSH bağlantısı kuruluyor  
✅ Git pull başarılı  
✅ Docker build tamamlandı (5-10 dakika)  
✅ Container'lar up durumda  
✅ API healthcheck pass  
✅ Frontend 3000 portunda çalışıyor  
✅ Veritabanı bağlantısı ok  
✅ Tarayıcıda açılıyor (3-5 dakika bekleme gerekebilir)  

---

## 💾 DOSYA YEDEKLEME (Deployment Öncesi)

```bash
# Sunucuda
cd /home/huseyinadm
tar -czf eticaret-backup-$(date +%Y%m%d-%H%M%S).tar.gz eticaret/
```
