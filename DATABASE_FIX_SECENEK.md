# SUNUCU BANNER FIX - Kolay Yöntem

## Seçenek 1: Sunucuda SSH ile Komut Çalıştır (KOLAY)

```bash
# 1. Sunucuya SSH ile bağlan
ssh huseyinadm@31.186.24.78

# 2. SQL Server container'ın PATH'ini kontrol et
docker exec ecommerce-sql-prod find / -name sqlcmd 2>/dev/null | head -5

# 3. Doğru path'i bulduktan sonra, SQL komutlarını çalıştır
# VEYA daha basit: T-SQL dosyasını container içine kopyala ve çalıştır
```

## Seçenek 2: .sql Dosyasını Container'a Kopyala (ÖNERİLEN)

```bash
# Sunucuda çalıştır:

# 1. Temp SQL dosyası oluştur
cat > /tmp/fix-banners.sql << 'EOF'
UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png' WHERE Id = 1;
UPDATE Banners SET ImageUrl = '/uploads/banners/banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png' WHERE Id = 2;
UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png' WHERE Id = 3;
UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png' WHERE Id = 4;

SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
GO
EOF

# 2. Container'a kopyala
docker cp /tmp/fix-banners.sql ecommerce-sql-prod:/tmp/

# 3. sqlcmd bul ve çalıştır
docker exec ecommerce-sql-prod bash -c "find / -name sqlcmd -type f 2>/dev/null | head -1 | xargs -I {} {} -S localhost -U sa -P 'ECom1234' -d ECommerceDb -i /tmp/fix-banners.sql"
```

## Seçenek 3: PowerShell'den Remote Bağlantı (DETAYLI)

```powershell
# Bilgisayarında PowerShell'de çalıştır:

$serverIP = "31.186.24.78"
$sqlPassword = "ECom1234"

# SQL Server'a bağlan (eğer SQL Server Management Studio yüklüyse)
# Veya invoke-sqlcmd kullan:

$params = @{
    ServerInstance = "31.186.24.78,1435"  # SQL Server port
    Database = "ECommerceDb"
    Username = "sa"
    Password = $sqlPassword
    QueryTimeout = 30
}

$updateQueries = @(
    "UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png' WHERE Id = 1;",
    "UPDATE Banners SET ImageUrl = '/uploads/banners/banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png' WHERE Id = 2;",
    "UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png' WHERE Id = 3;",
    "UPDATE Banners SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png' WHERE Id = 4;",
    "SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;"
)

foreach ($query in $updateQueries) {
    Write-Host "Çalıştırılıyor: $query"
    try {
        Invoke-Sqlcmd @params -Query $query
    } catch {
        Write-Error "Hata: $_"
    }
}
```

## Seçenek 4: Admin Panel Arayüzü Kullan (EN KOLAY)

Admin panel'de (`https://golkoygurme.com.tr/admin/posters`):
1. Her poster için "Düzenle" butonuna tıkla
2. Doğru görsel dosyasını seç ve yükle
3. Kaydet

Bu otomatik olarak database'i güncelleyecektir!

---

## HEMEN DENE - Sunucu Komut Satırında:

```bash
ssh huseyinadm@31.186.24.78

# İlk olarak database'deki mevcut durum gör
docker exec ecommerce-sql-prod bash -c "
find / -name sqlcmd -type f 2>/dev/null | head -1
"
```

Bunu çalıştırıp sqlcmd yolunu al, sonra bana bildir!
