# 🎉 HAZIR ZIP DOSYASI OLUŞTURULDU!

## 📦 Oluşturulan Dosyalar

### 1. ZIP Dosyası (Hazır Kullanım)
- **Dosya:** `ORNEK_URUN_IMPORT.zip` (3.32 KB)
- **Konum:** `c:\Users\GAMZE\Desktop\eticaret\`
- **İçerik:** 6 ürün + 6 görsel + README

### 2. Kaynak Klasör (Düzenlenebilir)
- **Klasör:** `ornek_zip_import\`
- **Konum:** `c:\Users\GAMZE\Desktop\eticaret\`
- **İçerik:**
  - `data.csv` - 6 örnek ürün bilgileri
  - `kasar-peyniri.jpg` - Kaşar peyniri görseli
  - `zeytin-siyah.jpg` - Zeytin görseli
  - `domates-salcasi.jpg` - Domates salçası görseli
  - `dana-kusbasi.jpg` - Dana kuşbaşı görseli
  - `sut.jpg` - Süt görseli
  - `elma.jpg` - Elma görseli
  - `README.txt` - Detaylı kullanım talimatları

---

## 🚀 NASIL KULLANILIR?

### Hızlı Başlangıç (3 Adım)

1. **Admin Paneli Açın**
   ```
   http://localhost:3000/admin/products (veya kendi sunucu adresiniz)
   ```

2. **Import Butonuna Tıklayın**
   - "Excel'den İçe Aktar" butonuna tıklayın
   - **"ZIP (Önerilen)"** seçeneğini seçin

3. **ZIP Dosyasını Yükleyin**
   - `ORNEK_URUN_IMPORT.zip` dosyasını seçin
   - "ZIP Yükle" butonuna tıklayın
   - ✅ 6 ürün ve 6 görsel otomatik olarak eklenecek!

---

## 📝 ZIP İçeriği Detayları

### CSV Dosyası (data.csv)
```csv
Name,Description,Price,Stock,CategoryId,ImageUrl,SpecialPrice
"Kaşar Peyniri 500gr","Taze kaşar peyniri",85.00,30,4,"kasar-peyniri.jpg",79.90
"Zeytin Siyah 500gr","Gemlik zeytini",45.00,50,9,"zeytin-siyah.jpg",
"Domates Salçası 700gr","Ev tipi domates salçası",32.50,40,9,"domates-salcasi.jpg",29.90
"Dana Kuşbaşı 500gr","Taze kesim dana eti",289.90,25,3,"dana-kusbasi.jpg",259.90
"Tam Yağlı Süt 1L","Günlük taze süt",25.50,100,4,"sut.jpg",
"Elma Golden 1kg","Taze golden elma",42.00,80,5,"elma.jpg",39.90
```

### Görseller
- SVG formatında renkli ve açıklayıcı örnekler
- Her ürün için eşleşen görsel adı
- Gerçek görsellerle değiştirebilirsiniz

---

## 🔧 Kendi ZIP'inizi Hazırlamak

### 1. Klasörü Düzenleyin
```powershell
cd c:\Users\GAMZE\Desktop\eticaret\ornek_zip_import
```

### 2. Görselleri Değiştirin
- Mevcut .jpg dosyalarını silin
- Kendi görsellerinizi aynı isimlerle ekleyin
- Veya yeni görseller ekleyip CSV'yi güncelleyin

### 3. CSV'yi Düzenleyin
- `data.csv` dosyasını Excel veya Not Defteri ile açın
- Ürünlerinizi ekleyin/düzenleyin
- `ImageUrl` sütununa sadece dosya adı yazın (örn: `urun.jpg`)

### 4. Yeni ZIP Oluşturun
```powershell
Compress-Archive -Path "c:\Users\GAMZE\Desktop\eticaret\ornek_zip_import\*" -DestinationPath "c:\Users\GAMZE\Desktop\eticaret\ORNEK_URUN_IMPORT.zip" -Force
```

---

## ⚠️ ÖNEMLI NOTLAR

### CSV Kuralları
- ✅ İlk satır başlık olmalı (silinmez)
- ✅ Name, Price, CategoryId zorunlu
- ✅ ImageUrl sütununda **sadece dosya adı** (örn: `urun.jpg`)
- ❌ Tam yol yazmayın (örn: `C:\resimler\urun.jpg`)

### Görsel Kuralları
- ✅ Desteklenen formatlar: .jpg, .jpeg, .png, .gif, .webp
- ✅ Maksimum görsel boyutu: 10 MB
- ✅ Dosya adı = CSV'deki ImageUrl değeri
- ✅ Görseller ZIP'in ana dizininde olmalı

### Kategori ID'leri
```
1  = Genel
3  = Et & Tavuk
4  = Süt Ürünleri
5  = Meyve & Sebze
6  = İçecek
7  = Atıştırmalık
8  = Temizlik
9  = Temel Gıda
```

---

## 🎯 Beklenen Sonuç

Yükleme tamamlandığında:
- ✅ 6 ürün eklenir
- ✅ 6 görsel yüklenir
- ✅ Her ürün kendi görseli ile eşleşir
- ✅ Kategorilerine göre sıralanır
- ✅ İndirimli fiyatlar otomatik uygulanır

---

## 🆘 Sorun Giderme

### "Görsel bulunamadı" Hatası
- CSV'deki dosya adı ile ZIP'teki dosya adı aynı mı?
- Dosya uzantısı doğru yazılmış mı? (.jpg, .png)

### "CSV dosyası boş" Hatası
- İlk satır başlık içeriyor mu?
- En az 1 ürün satırı var mı?

### "Kategori bulunamadı" Hatası
- CategoryId değeri 1-9 arası mı?
- Yukarıdaki kategori listesine bakın

---

## 📞 Destek

Sorularınız için `EXCEL_IMPORT_REHBERI.md` dosyasına bakın.

**Hazırlayan:** GitHub Copilot
**Tarih:** 17 Ocak 2026
