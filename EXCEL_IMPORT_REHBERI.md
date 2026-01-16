# 📊 EXCEL İLE TOPLU ÜRÜN EKLEME REHBERİ

## 📝 Gerekli Sütunlar (A-G)

| Sütun | Alan Adı     | Gerekli?     | Açıklama            | Örnek                      |
| ----- | ------------ | ------------ | ------------------- | -------------------------- |
| **A** | Name         | ✅ ZORUNLU   | Ürün adı            | Kaşar Peyniri 500gr        |
| **B** | Description  | ⚪ Opsiyonel | Ürün açıklaması     | Taze kaşar peyniri         |
| **C** | Price        | ✅ ZORUNLU   | Normal fiyat (₺)    | 85.00                      |
| **D** | Stock        | ⚪ Opsiyonel | Stok miktarı        | 30                         |
| **E** | CategoryId   | ✅ ZORUNLU   | Kategori ID         | 4                          |
| **F** | ImageUrl     | ⚪ Opsiyonel | Resim URL/yolu      | /uploads/products/urun.jpg |
| **G** | SpecialPrice | ⚪ Opsiyonel | İndirimli fiyat (₺) | 79.90                      |

---

## 🏷️ Kategori ID Listesi

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

## 📋 Örnek Excel İçeriği

**İlk satır başlık olmalı:**

```
Name                    | Description              | Price  | Stock | CategoryId | ImageUrl | SpecialPrice
Kaşar Peyniri 500gr    | Taze kaşar peyniri       | 85.00  | 30    | 4          |          | 79.90
Zeytin Siyah 500gr     | Gemlik zeytini           | 45.00  | 50    | 9          |          |
Domates Salçası 700gr  | Ev tipi domates salçası  | 32.50  | 40    | 9          |          | 29.90
```

---

## ✅ Önemli Notlar

1. **İlk satır başlık satırıdır** - atlayın
2. **Name ve Price zorunludur** - boş bırakılamaz
3. **CategoryId geçerli olmalı** (1-9 arası)
4. **Türkçe karakter kullanabilirsiniz** (ş, ı, ğ, ü, ö, ç)
5. **Fiyatlar ondalık olabilir** (32.50, 85.00)
6. **SpecialPrice boş bırakılırsa** indirim olmaz
7. **ImageUrl boş bırakılabilir** - varsayılan resim kullanılır
8. **CSV veya XLSX formatında** kaydedebilirsiniz

---

## 🚀 Nasıl Yüklenir?

1. **Admin Panel** → **Ürün Yönetimi**
2. **"Excel'den İçe Aktar"** butonuna tıklayın
3. Hazırladığınız dosyayı seçin
4. Yükleme tamamlanınca başarı mesajı görürsünüz

---

## 📁 Hazır Örnek Dosyalar

- `ORNEK_URUN_IMPORT.xlsx.csv` - 3 ürün örneği (bu dizinde)

Dosyayı kopyalayıp düzenleyebilirsiniz!
