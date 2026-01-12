# SUNUCU BANNER GÖRSEL SORUNU ÇÖZÜMÜ

## Sorun
- Container'da 5 dosya var
- Database'de farklı dosya adları kayıtlı
- Frontend database'den okuduğu dosya adını istiyor → 404 hatası

## Container'daki Dosyalar (MEVCUT)
```
banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png
banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png
banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png
banner_taze-dogal-indirim-banner_20260112111442_799269fc.png
banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png
```

## Çözüm - Sunucuda Çalıştır

### 1. Database'e Bağlan
```bash
docker exec -it ecommerce-sql-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -d ECommerceDb
```

### 2. Banner URL'lerini Güncelle
```sql
-- Mevcut durumu gör
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
GO

-- Slider Banner #1 güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png'
WHERE Id = 1;
GO

-- Promo Banner #2 güncelle  
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png'
WHERE Id = 2;
GO

-- Promo Banner #3 güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png'
WHERE Id = 3;
GO

-- Promo Banner #4 güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png'
WHERE Id = 4;
GO

-- Kontrol et
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
GO

-- Çıkış
EXIT
```

### 3. Tarayıcıyı Yenile
- `https://golkoygurme.com.tr/` adresini yenile
- Ctrl+Shift+R ile hard refresh yap (cache temizle)
- Görseller artık görünmeli!

## Kalıcı Çözüm

**Sorunu Yaratan Neden:**
- Dockerfile'da `COPY --from=build /src/ECommerce.API/uploads ./uploads` satırı var
- Bu satır BUILD sırasında LOCAL uploads klasörünü kopyalıyor
- Ama local uploads klasörü BOŞ!

**Kalıcı Çözüm Seçenekleri:**

### Seçenek 1: Dockerfile'dan Uploads Kopyalamayı Kaldır
```dockerfile
# Bu satırı YORUMLa veya SİL:
# COPY --from=build /src/ECommerce.API/uploads ./uploads
```

Bu durumda:
- Her container başladığında uploads klasörü boş olur
- BannerSeeder varsayılan görselleri kopyalamaya çalışır (ama frontend/public/images'dan alamaz)
- Admin panel üzerinden yeni posterler yüklenmelidir

### Seçenek 2: Volume Kullan (ÖNERİLEN)
`docker-compose.prod.yml` dosyasında:
```yaml
services:
  api:
    volumes:
      - banner-uploads:/app/uploads/banners
      - product-uploads:/app/uploads/products
      - category-uploads:/app/uploads/categories

volumes:
  banner-uploads:
  product-uploads:
  category-uploads:
```

Bu durumda:
- Uploads kalıcı hale gelir
- Container yeniden başlatıldığında dosyalar kaybolmaz
- Backup kolaylaşır

### Seçenek 3: Host Klasörü Mount Et
```yaml
services:
  api:
    volumes:
      - ./uploads:/app/uploads
```

Bu durumda:
- Dosyalar sunucunun disk'inde saklanır
- Container dışından erişilebilir
- Backup script'leri kolaylaşır

## Anlık Test

Sunucuda test et:
```bash
# 1. Database'de doğru dosya adları var mı?
docker exec -it ecommerce-sql-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -d ECommerceDb \
  -Q "SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;"

# 2. Dosya container'da mevcut mu?
docker exec ecommerce-api-prod ls -la /app/uploads/banners/

# 3. Nginx üzerinden erişilebiliyor mu?
curl -I http://127.0.0.1/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png

# 4. API üzerinden banner listesi doğru mu?
curl -s http://127.0.0.1:5000/api/banners/slider | jq '.[].imageUrl'
```
