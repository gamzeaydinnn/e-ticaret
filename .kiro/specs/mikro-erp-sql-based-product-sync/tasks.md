# Mikro ERP SQL Tabanlı Ürün Senkronizasyonu - Implementasyon Görevleri

## Genel Bakış

Bu görev listesi, Mikro ERP'den ürün çekerken yaşanan stok ve fiyat mapping sorunlarını çözmek için gereken tüm implementasyon adımlarını içermektedir.

## Görevler

### 1. Backend - DTO Model Güncellemeleri

- [ ] 1.1 MikroUrunDto sınıfı oluştur
  - `StokKod`, `UrunAdi`, `Fiyat`, `StokMiktar`, `DepoAdi`, `DepoNo`, `IsWebActive`, `Birim`, `GrupKod` property'lerini ekle
  - JsonPropertyName attribute'ları ile field mapping yap
  - Hesaplanan property'ler ekle (`IsStokta`, `StokDurumu`, `FiyatFormatli`)
  - _Gereksinim: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [ ] 1.2 MikroSqlVeriOkuRequestDto güncelle
  - `SQLSorgu` property'sini kontrol et
  - Parametreli sorgu desteği ekle
  - _Gereksinim: 1.1_

### 2. Backend - SQL Query Builder

- [ ] 2.1 BuildUnifiedProductQuery metodu oluştur
  - Parametreler: `depoNo`, `fiyatListesiNo`, `stokKod`, `grupKod`, `sadeceStoklu`, `sadeceAktif`
  - STOK_SATIS_FIYAT_LISTELERI_YONETIM + fn_Stok_Depo_Dagilim + STOKLAR JOIN sorgusu
  - ROW_NUMBER() ile tekilleştirme
  - Bu yıl içinde hareketi olan ürünleri filtreleme
  - Parametreli sorgu ile SQL injection koruması
  - _Gereksinim: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 2.2 BuildSqlStockQuery metodunu güncelle
  - Mevcut sorguyu yeni birleşik sorgu yapısına uyarla
  - msg_S_0343 alanını stok miktarı olarak kullan
  - _Gereksinim: 1.4, 4.2_

- [ ] 2.3 BuildSqlPriceQuery metodunu güncelle
  - Mevcut sorguyu yeni birleşik sorgu yapısına uyarla
  - msg_S_0002 alanını fiyat olarak kullan
  - _Gereksinim: 1.5, 4.1_

### 3. Backend - Response Parser

- [ ] 3.1 ParseUnifiedProductRows metodu oluştur
  - JSON response'unu parse et
  - Her satır için ParseProductRow çağır
  - Hatalı satırları atla ve logla
  - _Gereksinim: 4.3, 4.4_

- [ ] 3.2 ParseProductRow metodu oluştur
  - JsonElement'ten MikroUrunDto oluştur
  - ReadStringFromRow ile field okuma
  - ParseDecimalFlexible ile decimal dönüşüm
  - ParseIntFlexible ile int dönüşüm
  - ParseBoolFlexible ile bool dönüşüm
  - _Gereksinim: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [ ] 3.3 ReadStringFromRow helper metodu oluştur
  - Birden fazla field ismi dene (fallback mekanizması)
  - Case-insensitive field matching
  - Null/empty value handling
  - _Gereksinim: 4.5, 4.6_

- [ ] 3.4 ParseDecimalFlexible helper metodu oluştur
  - Virgül ve nokta ayraçlarını normalize et
  - Türk kültür ayarlarına uygun parsing
  - Geçersiz değerler için 0 döndür
  - _Gereksinim: 4.6_

- [ ] 3.5 ParseIntFlexible helper metodu oluştur
  - String'den int'e dönüşüm
  - Geçersiz değerler için 0 döndür
  - _Gereksinim: 4.6_

- [ ] 3.6 ParseBoolFlexible helper metodu oluştur
  - 1/0, true/false, "1"/"0" değerlerini handle et
  - Geçersiz değerler için false döndür
  - _Gereksinim: 4.6_

### 4. Backend - MicroService Güncellemeleri

- [ ] 4.1 GetProductsWithSqlAsync metodu oluştur
  - BuildUnifiedProductQuery ile sorgu oluştur
  - SqlVeriOkuV2 endpoint'ine istek gönder
  - ParseUnifiedProductRows ile parse et
  - Cache mekanizması entegre et
  - _Gereksinim: 1.1, 1.2, 1.3_

- [ ] 4.2 GetStockMapWithSqlAsync metodunu güncelle
  - Yeni SQL sorgusu kullan
  - msg_S_0343 field'ini parse et
  - _Gereksinim: 1.4_

- [ ] 4.3 GetPriceMapWithSqlAsync metodunu güncelle
  - Yeni SQL sorgusu kullan
  - msg_S_0002 field'ini parse et
  - _Gereksinim: 1.5_

### 5. Backend - Cache Manager

- [ ] 5.1 Cache key oluşturma metodu ekle
  - Format: `mikro_products_{depoNo}_{fiyatListesiNo}_{grupKod}_{sadeceStoklu}`
  - Null parametreler için default değer kullan
  - _Gereksinim: 8.3_

- [ ] 5.2 Cache get/set metodları ekle
  - IMemoryCache kullan
  - TTL: 5 dakika
  - Cache hit/miss loglama
  - _Gereksinim: 8.3_

- [ ] 5.3 Cache invalidation metodu ekle
  - Manuel sync butonuna basıldığında cache temizle
  - Ürün güncellendiğinde ilgili cache key'leri temizle
  - _Gereksinim: 8.3_

### 6. Backend - Controller Güncellemeleri

- [ ] 6.1 AdminMicroController.GetStokListesi endpoint'ini güncelle
  - Yeni GetProductsWithSqlAsync metodunu kullan
  - Request validation ekle
  - Response formatting düzenle
  - _Gereksinim: 1.1, 1.2, 1.3_

- [ ] 6.2 Hata yönetimi ekle
  - Try-catch blokları
  - Detaylı hata mesajları
  - Fallback mekanizması (cache'den son başarılı sonuç)
  - _Gereksinim: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 6.3 Loglama ekle
  - SQL sorgusu detayları (Debug)
  - Başarılı işlemler (Information)
  - Veri tutarsızlıkları (Warning)
  - Hatalar (Error)
  - _Gereksinim: 7.1, 7.2, 7.3, 7.4, 7.5_

### 7. Frontend - AdminMicro.js Güncellemeleri

- [ ] 7.1 Stok görüntüleme düzelt
  - API'den gelen `stokMiktar` field'ini kullan
  - 0 stok için "Stokta Yok" göster
  - Stok durumuna göre badge rengi ayarla
  - _Gereksinim: 6.1, 6.3_

- [ ] 7.2 Fiyat görüntüleme düzelt
  - API'den gelen `fiyat` field'ini kullan
  - 0 fiyat için uyarı göster
  - Türk Lirası formatında göster (₺XX.XX)
  - _Gereksinim: 6.2, 6.4_

- [ ] 7.3 Web aktiflik göstergesi ekle
  - `isWebActive` field'ine göre görsel gösterge
  - Pasif ürünler için gri badge
  - Aktif ürünler için yeşil badge
  - _Gereksinim: 6.5_

- [ ] 7.4 Hata mesajları göster
  - API hatalarını kullanıcıya bildir
  - Timeout durumunda retry seçeneği sun
  - Cache'den yükleme durumunu göster
  - _Gereksinim: 7.1, 7.2, 7.3, 7.4, 7.5_

### 8. Test - Unit Testler

- [ ] 8.1 SQL Query Builder testleri
  - Parametresiz sorgu testi
  - Depo filtreli sorgu testi
  - Grup kodu filtreli sorgu testi
  - SQL injection koruması testi
  - _Gereksinim: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

- [ ] 8.2 Parser testleri
  - Geçerli JSON parsing testi
  - Eksik field handling testi
  - Null value handling testi
  - Decimal parsing testi (virgül/nokta)
  - Boolean parsing testi (1/0, true/false)
  - _Gereksinim: 4.3, 4.4, 4.5, 4.6_

- [ ] 8.3 DTO Mapping testleri
  - Field name mapping testi
  - Type conversion testi
  - Default value assignment testi
  - _Gereksinim: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

### 9. Test - Integration Testler

- [ ] 9.1 Mikro API integration testi
  - Gerçek API çağrısı
  - Response parsing
  - Error handling
  - _Gereksinim: 1.1, 1.2, 1.3_

- [ ] 9.2 Cache integration testi
  - Cache set/get
  - Cache invalidation
  - TTL expiration
  - _Gereksinim: 8.3_

- [ ] 9.3 End-to-end test
  - Frontend'den backend'e tam akış
  - Ürün listesi görüntüleme
  - Filtreleme ve sayfalama
  - _Gereksinim: 6.1, 6.2, 6.3, 6.4, 6.5_

### 10. Test - Manuel Test Senaryoları

- [ ] 10.1 Stok kontrolü
  - Mikro ERP'de stok değiştir
  - Sistemde sync yap
  - Doğru stok görüntülendiğini kontrol et
  - _Gereksinim: 6.1, 6.3_

- [ ] 10.2 Fiyat kontrolü
  - Mikro ERP'de fiyat değiştir
  - Sistemde sync yap
  - Doğru fiyat görüntülendiğini kontrol et
  - _Gereksinim: 6.2, 6.4_

- [ ] 10.3 Web aktiflik kontrolü
  - Mikro ERP'de sto_webe_gonderilecek_fl değiştir
  - Sistemde sync yap
  - Ürünün görünürlüğünü kontrol et
  - _Gereksinim: 6.5_

### 11. Dokümantasyon

- [ ] 11.1 API dokümantasyonu güncelle
  - Yeni endpoint parametreleri
  - Response formatı
  - Hata kodları
  - Örnek request/response

- [ ] 11.2 Kod yorumları ekle
  - SQL sorgusu açıklamaları
  - Field mapping tablosu
  - Parsing mantığı açıklamaları
  - _Gereksinim: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 11.3 Kullanıcı dokümantasyonu
  - Admin paneli kullanım kılavuzu
  - Sorun giderme rehberi
  - SSS

### 12. Deployment

- [ ] 12.1 Staging ortamda test
  - Tüm testleri çalıştır
  - Performance testleri
  - Load testleri

- [ ] 12.2 Production deployment
  - Database migration
  - Backend deployment
  - Frontend deployment
  - Smoke testler

- [ ] 12.3 Monitoring kurulumu
  - Application Insights / Serilog
  - SQL query performance monitoring
  - API response time tracking
  - Error rate alerting

## Notlar

- Her görev tamamlandığında ilgili checkbox işaretlenmelidir
- Görevler sırayla yapılmalı, bağımlılıklar göz önünde bulundurulmalıdır
- Her görev için unit test yazılmalıdır
- Kod review süreci tamamlanmadan production'a geçilmemelidir
- Rollback planı hazır olmalıdır
