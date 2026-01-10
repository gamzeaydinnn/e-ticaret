# 🛠️ SUNUCU CONTAINER HATASI ÇÖZÜMÜ

## ❌ HATA

```
Error response from daemon: Conflict. The container name "/ecommerce-sql-prod"
is already in use by container "2f0d94418128..."
```

## 🔍 SEBEP

Önceki container'lar siliştirilmemiş, yeni container başlatmaya çalışıyor.

---

## ✅ ÇÖZÜM (HIZLI)

### SEÇENEK 1: Script Kullanarak (Önerilen)

```bash
# Sunucuya bağlan
ssh huseyinadm@31.186.24.78

# Script'i indirip çalıştır
curl -s https://raw.githubusercontent.com/gamzeaydinnn/e-ticaret/main/SUNUCU_CONTAINER_TEMIZLE.sh | bash
```

### SEÇENEK 2: Manuel Komutlar

```bash
ssh huseyinadm@31.186.24.78
cd ~/eticaret

# 1. Tüm container'ları durdur
docker stop $(docker ps -q)
sleep 5

# 2. Eski container'ları sil
docker rm ecommerce-sql-prod
docker rm ecommerce-api-prod
docker rm ecommerce-frontend-prod

# 3. Dangling image'ları temizle
docker image prune -f

# 4. Docker temizliği
docker system prune -af
docker volume prune -f

# 5. Fresh build başlat
docker-compose -f docker-compose.prod.yml build --no-cache --force-rm

# 6. Servisleri başlat
docker-compose -f docker-compose.prod.yml up -d

# 7. Durumu kontrol et
sleep 30
docker-compose -f docker-compose.prod.yml ps
```

---

## 📊 ADIM ADIM NE YAPILIYOR

| Adım | Komut                             | Açıklama                                                  |
| ---- | --------------------------------- | --------------------------------------------------------- |
| 1    | `docker stop`                     | Çalışan konteynerler durduruluyor                         |
| 2    | `docker rm`                       | Eski container'lar kaldırılıyor                           |
| 3    | `docker image prune`              | Kullanılmayan image'lar siliniyor                         |
| 4    | `docker system prune`             | Docker tarafından oluşturulan gereksiz dosyalar siliniyor |
| 5    | `docker-compose build --no-cache` | Yeni image'lar oluşturuluyor                              |
| 6    | `docker-compose up -d`            | Yeni container'lar başlatılıyor                           |

---

## ✅ BAŞARILI OLUP OLMADIĞINI KONTROL ET

```bash
# 1. Container'lar çalışıyor mu?
docker-compose -f docker-compose.prod.yml ps

# Görmek istediğin:
# NAME                    STATUS
# ecommerce-sql-prod      Up (healthy)
# ecommerce-api-prod      Up
# ecommerce-frontend-prod Up
```

```bash
# 2. Migration log'larını izle
docker-compose -f docker-compose.prod.yml logs api

# Görmek istediğin:
# [INFO] ✅ Database schema oluşturuldu
# [INFO] ✅ IdentitySeeder tamamlandı
# [INFO] ✅ ProductSeeder tamamlandı
```

```bash
# 3. API çalışıyor mu?
curl http://localhost:5000/api/categories

# Görmek istediğin (JSON):
# [{"id":1,"name":"Elektronik",...}]
```

---

## 🎯 SONUÇ

Temizlikten sonra:

- ✅ Yeni, temiz container'lar oluşturulacak
- ✅ Migration otomatik çalışacak
- ✅ Veritabanı yeniden kurulacak
- ✅ Kategoriler görünecek

**Tahmini Süre**: ~5-10 dakika

---

## ⚠️ EĞER HALA SORUN VARSA

```bash
# Tüm image'ları listele
docker images

# Tüm container'ları listele (durdurulmuş olanlar dahil)
docker ps -a

# Belirli bir container'ı forca sil
docker rm -f ecommerce-sql-prod

# Belirli bir image'ı sil
docker rmi -f ecommerce-api
```

---

## 💡 İPUCU

Sunucuda birden fazla deploy yaptıysanız ve container adı çakışıyorsa:

```bash
# Container adını değiştirme (alternative)
docker rename ecommerce-sql-prod ecommerce-sql-prod-old
docker rename ecommerce-api-prod ecommerce-api-prod-old
docker rename ecommerce-frontend-prod ecommerce-frontend-prod-old

# Eski olanları sil
docker rm ecommerce-sql-prod-old
docker rm ecommerce-api-prod-old
docker rm ecommerce-frontend-prod-old
```

**Ama önerilen yöntem**: Basitçe `docker rm` ile silmek!
