# Mikro ERP SQL Tabanlı Ürün Senkronizasyonu - İmplementasyon Planı

## 🎯 Proje Özeti

Mikro ERP'den ürün çekerken stok ve fiyat bilgilerinin 0 olarak gelmesi veya yanlış görüntülenmesi sorununu çözmek için SQL tabanlı bir yaklaşım geliştirilecektir.

## 🔍 Sorun Analizi

### Mevcut Durum

- ✅ Mikro ERP API'den ürünler çekiliyor
- ❌ Stok miktarları 0 olarak görünüyor
- ❌ Fiyat bilgileri yanlış field'lerden okunuyor
- ❌ Field mapping hatası var (msg_S_XXXX formatı)
- ❌ `sto_webe_gonderilecek_fl` alanı kontrol edilmiyor

### Kök Neden

1. **Field İsimleri Uyumsuzluğu:** Mikro ERP `msg_S_0001`, `msg_S_0002` gibi kodlanmış field isimleri kullanıyor
2. **Eksik JOIN:** STOKLAR tablosu sorguya dahil edilmemiş
3. **Yanlış Field Mapping:** Parser doğru field'leri okumuyor
4. **API Endpoint Sınırlamaları:** StokListesiV2 endpoint'i bazı bilgileri eksik döndürüyor

## 💡 Çözüm Yaklaşımı

### Stratejik Kararlar

1. **SQL Tabanlı Yaklaşım:**
   - ✅ Doğrudan veritabanı sorgusu ile veri çekme
   - ✅ Tüm gerekli tabloları JOIN etme
   - ✅ Field isimlerini kontrol altına alma

2. **Birleşik Sorgu:**

   ```sql
   STOK_SATIS_FIYAT_LISTELERI_YONETIM (Fiyat)
   + fn_Stok_Depo_Dagilim (Stok Miktarı)
   + STOKLAR (Web Aktiflik, Grup Kodu)
   + STOK_HAREKETLERI (Son Hareket Tarihi)
   ```

3. **Geliştirilmiş Parsing:**
   - Case-insensitive field matching
   - Fallback mekanizması (alternatif field isimleri)
   - Türk kültür ayarlarına uygun decimal parsing
   - Null/empty value handling

## 📋 İmplementasyon Adımları

### Faz 1: Backend - DTO ve Model Güncellemeleri (2 saat)

**Görevler:**

1. `MikroUrunDto` sınıfı oluştur
2. Field mapping attribute'ları ekle
3. Hesaplanan property'ler ekle

**Dosyalar:**

- `src/ECommerce.Infrastructure/DTOs/MikroUrunDto.cs` (YENİ)

**Çıktı:**

- Mikro ERP field'leri ile uyumlu DTO modeli

---

### Faz 2: Backend - SQL Query Builder (3 saat)

**Görevler:**

1. `BuildUnifiedProductQuery` metodu oluştur
2. Parametreli sorgu desteği ekle
3. JOIN mantığını kur
4. ROW_NUMBER ile tekilleştirme

**Dosyalar:**

- `src/ECommerce.Infrastructure/Services/MicroServices/MicroService.cs`

**SQL Sorgusu Yapısı:**

```sql
WITH WebAday AS (
    SELECT Y.*, D.*, ROW_NUMBER() OVER (...) AS rn
    FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM Y
    OUTER APPLY fn_Stok_Depo_Dagilim(Y.msg_S_0001) D
    WHERE ...
)
SELECT W.*, S.sto_webe_gonderilecek_fl
FROM WebAday W
INNER JOIN STOKLAR S ON S.sto_kod = W.msg_S_0001
WHERE W.rn = 1
```

**Çıktı:**

- Tüm gerekli bilgileri içeren SQL sorgusu

---

### Faz 3: Backend - Response Parser (4 saat)

**Görevler:**

1. `ParseUnifiedProductRows` metodu oluştur
2. `ParseProductRow` metodu oluştur
3. Helper metodlar: `ReadStringFromRow`, `ParseDecimalFlexible`, `ParseIntFlexible`, `ParseBoolFlexible`
4. Hata yönetimi ekle

**Dosyalar:**

- `src/ECommerce.Infrastructure/Services/MicroServices/MicroService.cs`

**Field Mapping Tablosu:**
| Mikro Field | DTO Property | Tip |
|-------------|--------------|-----|
| msg_S_0001 | StokKod | string |
| msg_S_0002 | Fiyat | decimal |
| msg_S_0343 | StokMiktar | decimal |
| msg_S_0005 | UrunAdi | string |
| sto_webe_gonderilecek_fl | IsWebActive | bool |

**Çıktı:**

- JSON response'unu DTO'lara dönüştüren parser

---

### Faz 4: Backend - Service Metodları (2 saat)

**Görevler:**

1. `GetProductsWithSqlAsync` metodu oluştur
2. Cache mekanizması entegre et
3. Hata yönetimi ve loglama ekle

**Dosyalar:**

- `src/ECommerce.Infrastructure/Services/MicroServices/MicroService.cs`

**Çıktı:**

- SQL sorgusu çalıştıran ve parse eden servis metodu

---

### Faz 5: Backend - Controller Güncellemeleri (2 saat)

**Görevler:**

1. `AdminMicroController.GetStokListesi` endpoint'ini güncelle
2. Request validation ekle
3. Response formatting düzenle
4. Hata yönetimi ekle

**Dosyalar:**

- `src/ECommerce.API/Controllers/Admin/AdminMicroController.cs`

**Çıktı:**

- Güncellenmiş API endpoint

---

### Faz 6: Frontend - UI Güncellemeleri (3 saat)

**Görevler:**

1. Stok görüntüleme düzelt
2. Fiyat görüntüleme düzelt
3. Web aktiflik göstergesi ekle
4. Hata mesajları göster

**Dosyalar:**

- `frontend/src/pages/Admin/AdminMicro.js`

**Değişiklikler:**

```javascript
// Stok görüntüleme
<span className={`badge ${
  product.stokMiktar > 10 ? "bg-success" :
  product.stokMiktar > 0 ? "bg-warning" : "bg-danger"
}`}>
  {product.stokMiktar || 0}
</span>

// Fiyat görüntüleme
<span className="fw-bold" style={{ color: "#f57c00" }}>
  ₺{product.fiyat?.toFixed(2) || "0.00"}
</span>

// Web aktiflik
{!product.isWebActive && (
  <span className="badge bg-secondary">Web'e Gönderilmeyecek</span>
)}
```

**Çıktı:**

- Doğru stok ve fiyat bilgilerini gösteren UI

---

### Faz 7: Test - Unit Testler (4 saat)

**Görevler:**

1. SQL Query Builder testleri
2. Parser testleri
3. DTO Mapping testleri

**Dosyalar:**

- `tests/ECommerce.Infrastructure.Tests/Services/MicroServiceTests.cs` (YENİ)

**Test Senaryoları:**

- ✅ Parametresiz sorgu oluşturma
- ✅ Depo filtreli sorgu oluşturma
- ✅ Geçerli JSON parsing
- ✅ Eksik field handling
- ✅ Null value handling
- ✅ Decimal parsing (virgül/nokta)
- ✅ SQL injection koruması

**Çıktı:**

- %80+ code coverage

---

### Faz 8: Test - Integration ve Manuel Testler (3 saat)

**Görevler:**

1. Mikro API integration testi
2. Cache integration testi
3. End-to-end test
4. Manuel test senaryoları

**Test Senaryoları:**

- ✅ Mikro ERP'de stok değiştir → Sistemde sync → Doğru görüntüleme
- ✅ Mikro ERP'de fiyat değiştir → Sistemde sync → Doğru görüntüleme
- ✅ Web aktiflik değiştir → Sistemde sync → Görünürlük kontrolü

**Çıktı:**

- Tüm senaryolar başarılı

---

### Faz 9: Dokümantasyon (2 saat)

**Görevler:**

1. API dokümantasyonu güncelle
2. Kod yorumları ekle
3. Kullanıcı dokümantasyonu

**Dosyalar:**

- `docs/API_DOCUMENTATION.md`
- `docs/USER_GUIDE.md`
- `docs/TROUBLESHOOTING.md`

**Çıktı:**

- Kapsamlı dokümantasyon

---

### Faz 10: Deployment (2 saat)

**Görevler:**

1. Staging ortamda test
2. Production deployment
3. Monitoring kurulumu

**Adımlar:**

1. Database migration (gerekirse)
2. Backend deployment
3. Frontend deployment
4. Smoke testler
5. Monitoring dashboard kontrolü

**Çıktı:**

- Production'da çalışan sistem

---

## 📊 Zaman Tahmini

| Faz                              | Süre   | Kümülatif |
| -------------------------------- | ------ | --------- |
| Faz 1: DTO Güncellemeleri        | 2 saat | 2 saat    |
| Faz 2: SQL Query Builder         | 3 saat | 5 saat    |
| Faz 3: Response Parser           | 4 saat | 9 saat    |
| Faz 4: Service Metodları         | 2 saat | 11 saat   |
| Faz 5: Controller Güncellemeleri | 2 saat | 13 saat   |
| Faz 6: Frontend Güncellemeleri   | 3 saat | 16 saat   |
| Faz 7: Unit Testler              | 4 saat | 20 saat   |
| Faz 8: Integration Testler       | 3 saat | 23 saat   |
| Faz 9: Dokümantasyon             | 2 saat | 25 saat   |
| Faz 10: Deployment               | 2 saat | 27 saat   |

**Toplam Tahmini Süre:** 27 saat (≈ 3.5 iş günü)

---

## 🎯 Başarı Kriterleri

### Fonksiyonel Kriterler

- ✅ Stok bilgileri %100 doğrulukla görüntüleniyor
- ✅ Fiyat bilgileri %100 doğrulukla görüntüleniyor
- ✅ Web aktiflik durumu doğru gösteriliyor
- ✅ Filtreleme ve sayfalama çalışıyor

### Performans Kriterleri

- ✅ Ürün çekme işlemi < 10 saniye (100 ürün için)
- ✅ API response time < 2 saniye
- ✅ Cache hit rate > %70

### Kalite Kriterleri

- ✅ Unit test coverage > %80
- ✅ Hata oranı < %1
- ✅ Kod review tamamlandı
- ✅ Dokümantasyon güncel

---

## 🚨 Riskler ve Azaltma Stratejileri

### Risk 1: Mikro ERP API Değişiklikleri

**Olasılık:** Orta  
**Etki:** Yüksek  
**Azaltma:**

- Field isimleri için fallback mekanizması
- Kapsamlı hata yönetimi
- Monitoring ve alerting

### Risk 2: Performans Sorunları

**Olasılık:** Düşük  
**Etki:** Orta  
**Azaltma:**

- SQL sorgu optimizasyonu
- Cache mekanizması
- Sayfalama
- Load testing

### Risk 3: Veri Tutarsızlığı

**Olasılık:** Orta  
**Etki:** Yüksek  
**Azaltma:**

- Validation kuralları
- Detaylı loglama
- Manuel test senaryoları
- Rollback planı

---

## 📝 Notlar

### Önemli Noktalar

1. **SQL Injection Koruması:** Tüm sorgular parametreli olmalı
2. **Null Handling:** Tüm field'ler için null check yapılmalı
3. **Decimal Parsing:** Türk kültür ayarlarına dikkat edilmeli
4. **Cache Invalidation:** Ürün güncellendiğinde cache temizlenmeli
5. **Loglama:** Tüm kritik işlemler loglanmalı

### Bağımlılıklar

- Mikro ERP API erişimi
- SqlVeriOkuV2 endpoint'i aktif olmalı
- Veritabanı tabloları mevcut olmalı
- IMemoryCache servisi yapılandırılmış olmalı

### Rollback Planı

1. Önceki Docker image'ı hazır tut
2. Database migration geri alınabilir olmalı
3. Feature flag ile yeni kod devre dışı bırakılabilir
4. Monitoring ile hızlı tespit

---

## 🎉 Sonuç

Bu plan, Mikro ERP'den ürün çekerken yaşanan stok ve fiyat mapping sorunlarını kapsamlı bir şekilde çözmektedir. SQL tabanlı yaklaşım, doğrudan veritabanı sorgulama ile veri tutarlılığını garanti ederken, geliştirilmiş parsing mantığı field mapping sorunlarını ortadan kaldırmaktadır.

**Tahmini Tamamlanma Süresi:** 3.5 iş günü  
**Öncelik:** Yüksek  
**Karmaşıklık:** Orta  
**Risk Seviyesi:** Düşük
