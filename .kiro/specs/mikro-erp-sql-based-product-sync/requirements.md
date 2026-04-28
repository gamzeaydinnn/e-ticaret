# Mikro ERP SQL Tabanlı Ürün Senkronizasyonu - Gereksinimler Dökümanı

## Giriş

Mevcut sistemde Mikro ERP'den ürün çekerken stok ve fiyat bilgileri 0 olarak gelmekte veya yanlış mapping yapılmaktadır. Bu sorun, API endpoint'lerinin döndürdüğü field isimleri ile bizim sistemimizin beklediği field isimleri arasındaki uyumsuzluktan kaynaklanmaktadır.

## Terimler Sözlüğü

- **Mikro_ERP**: Şirketin kullandığı ERP sistemi (Mikro yazılım)
- **StokListesiV2**: Mikro ERP'nin ürün listesi API endpoint'i
- **SqlVeriOkuV2**: Mikro ERP'nin SQL sorgusu çalıştırma endpoint'i
- **STOK_SATIS_FIYAT_LISTELERI_YONETIM**: Mikro ERP'deki fiyat listesi tablosu
- **STOKLAR**: Mikro ERP'deki stok kartları tablosu
- **fn_Stok_Depo_Dagilim**: Mikro ERP'deki depo dağılım fonksiyonu
- **STOK_HAREKETLERI**: Mikro ERP'deki stok hareketleri tablosu
- **Mapping**: API'den gelen field isimlerinin sistem modellerine dönüştürülmesi
- **Cache_Tablosu**: Mikro'dan çekilen ürünlerin geçici olarak saklandığı veritabanı tablosu

## Gereksinimler

### Gereksinim 1: SQL Sorgusu ile Ürün Çekme

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, Mikro ERP'den ürünleri doğrudan SQL sorgusu ile çekmek istiyorum, böylece API endpoint'lerindeki mapping sorunlarından etkilenmeyeceğim.

#### Kabul Kriterleri

1. WHEN sistem Mikro ERP'den ürün çektiğinde, THE Sistem SHALL SqlVeriOkuV2 endpoint'ini kullanarak doğrudan SQL sorgusu çalıştırmalı
2. WHEN SQL sorgusu çalıştırıldığında, THE Sistem SHALL STOK_SATIS_FIYAT_LISTELERI_YONETIM ve STOKLAR tablolarını JOIN etmeli
3. WHEN SQL sorgusu sonucu geldiğinde, THE Sistem SHALL msg_S_XXXX formatındaki field isimlerini doğru şekilde parse etmeli
4. WHEN ürün verisi parse edildiğinde, THE Sistem SHALL stok miktarını msg_S_0343 alanından okumalı
5. WHEN ürün verisi parse edildiğinde, THE Sistem SHALL fiyat bilgisini msg_S_0002 alanından okumalı
6. WHEN ürün verisi parse edildiğinde, THE Sistem SHALL sto_webe_gonderilecek_fl alanını kontrol etmeli

### Gereksinim 2: Field Mapping Düzeltmesi

**Kullanıcı Hikayesi:** Geliştirici olarak, Mikro ERP'den gelen field isimlerinin sistem modellerine doğru şekilde map edilmesini istiyorum, böylece stok ve fiyat bilgileri doğru görüntülenecek.

#### Kabul Kriterleri

1. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_0001 alanını StokKod olarak map etmeli
2. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_0002 alanını Fiyat olarak map etmeli
3. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_0343 alanını StokMiktar olarak map etmeli
4. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_0005 alanını UrunAdi olarak map etmeli
5. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_1266 alanını DepoAdi olarak map etmeli
6. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL msg_S_0873 alanını DepoNo olarak map etmeli
7. WHEN Mikro API'den veri geldiğinde, THE Sistem SHALL sto_webe_gonderilecek_fl alanını IsWebActive olarak map etmeli

### Gereksinim 3: Birleşik SQL Sorgusu Oluşturma

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, tek bir SQL sorgusu ile tüm gerekli bilgileri (stok, fiyat, depo) çekmek istiyorum, böylece performans artacak ve veri tutarlılığı sağlanacak.

#### Kabul Kriterleri

1. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL STOK_SATIS_FIYAT_LISTELERI_YONETIM tablosunu ana tablo olarak kullanmalı
2. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL fn_Stok_Depo_Dagilim fonksiyonunu OUTER APPLY ile birleştirmeli
3. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL STOKLAR tablosunu JOIN ederek sto_webe_gonderilecek_fl alanını almalı
4. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL ROW_NUMBER() ile son stok hareketine göre tekilleştirme yapmalı
5. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL sadece bu yıl içinde hareketi olan ürünleri filtrelemeli
6. WHEN SQL sorgusu oluşturulduğunda, THE Sistem SHALL 'GÖLKÖY ŞUBE DEPO' ve boş depo filtrelerini uygulamalı

### Gereksinim 4: Backend Service Güncellemesi

**Kullanıcı Hikayesi:** Geliştirici olarak, MicroService sınıfının SQL sorgusu oluşturma ve parse etme mantığını güncellemek istiyorum, böylece doğru veriler çekilecek.

#### Kabul Kriterleri

1. WHEN BuildSqlPriceQuery metodu çağrıldığında, THE Sistem SHALL güncellenmiş birleşik SQL sorgusunu döndürmeli
2. WHEN BuildSqlStockQuery metodu çağrıldığında, THE Sistem SHALL güncellenmiş birleşik SQL sorgusunu döndürmeli
3. WHEN ParseSqlPriceRows metodu çağrıldığında, THE Sistem SHALL msg_S_XXXX field isimlerini doğru parse etmeli
4. WHEN ParseSqlStockRows metodu çağrıldığında, THE Sistem SHALL msg_S_XXXX field isimlerini doğru parse etmeli
5. WHEN parse işlemi yapılırken, THE Sistem SHALL case-insensitive field name matching kullanmalı
6. WHEN parse işlemi yapılırken, THE Sistem SHALL null/boş değerleri güvenli şekilde handle etmeli

### Gereksinim 5: DTO Model Güncellemesi

**Kullanıcı Hikayesi:** Geliştirici olarak, DTO modellerinin Mikro ERP field isimleriyle uyumlu olmasını istiyorum, böylece mapping sorunları ortadan kalkacak.

#### Kabul Kriterleri

1. WHEN MikroStokSatirDto oluşturulduğında, THE Sistem SHALL msg_S_0001 için StokKod property'si içermeli
2. WHEN MikroStokSatirDto oluşturulduğında, THE Sistem SHALL msg_S_0002 için Fiyat property'si içermeli
3. WHEN MikroStokSatirDto oluşturulduğunda, THE Sistem SHALL msg_S_0343 için StokMiktar property'si içermeli
4. WHEN MikroStokSatirDto oluşturulduğunda, THE Sistem SHALL msg_S_0005 için UrunAdi property'si içermeli
5. WHEN MikroStokSatirDto oluşturulduğunda, THE Sistem SHALL sto_webe_gonderilecek_fl için IsWebActive property'si içermeli
6. WHEN DTO deserialize edildiğinde, THE Sistem SHALL JsonPropertyName attribute'ları ile field mapping yapmalı

### Gereksinim 6: Frontend Görüntüleme Düzeltmesi

**Kullanıcı Hikayesi:** Kullanıcı olarak, admin panelinde ürünlerin stok ve fiyat bilgilerini doğru görmek istiyorum, böylece doğru kararlar alabileceğim.

#### Kabul Kriterleri

1. WHEN ürün listesi görüntülendiğinde, THE Sistem SHALL her ürün için doğru stok miktarını göstermeli
2. WHEN ürün listesi görüntülendiğinde, THE Sistem SHALL her ürün için doğru fiyat bilgisini göstermeli
3. WHEN stok 0 ise, THE Sistem SHALL ürünü "Stokta Yok" olarak işaretlemeli
4. WHEN fiyat 0 ise, THE Sistem SHALL uyarı mesajı göstermeli
5. WHEN ürün web'e gönderilmeyecek olarak işaretliyse, THE Sistem SHALL bunu görsel olarak belirtmeli

### Gereksinim 7: Hata Yönetimi ve Loglama

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, SQL sorgusu hatalarını ve mapping sorunlarını detaylı loglarla görmek istiyorum, böylece sorunları hızlıca çözebileceğim.

#### Kabul Kriterleri

1. WHEN SQL sorgusu başarısız olduğunda, THE Sistem SHALL detaylı hata mesajı loglamalı
2. WHEN parse işlemi başarısız olduğunda, THE Sistem SHALL hangi field'in sorunlu olduğunu loglamalı
3. WHEN mapping hatası oluştuğunda, THE Sistem SHALL kaynak ve hedef field isimlerini loglamalı
4. WHEN veri tutarsızlığı tespit edildiğinde, THE Sistem SHALL uyarı seviyesinde log oluşturmalı
5. WHEN başarılı işlem tamamlandığında, THE Sistem SHALL özet bilgi loglamalı

### Gereksinim 8: Performans Optimizasyonu

**Kullanıcı Hikayesi:** Sistem yöneticisi olarak, ürün çekme işleminin hızlı ve verimli olmasını istiyorum, böylece kullanıcı deneyimi olumsuz etkilenmeyecek.

#### Kabul Kriterleri

1. WHEN toplu ürün çekme yapılırken, THE Sistem SHALL sayfalama kullanmalı
2. WHEN SQL sorgusu çalıştırılırken, THE Sistem SHALL timeout mekanizması uygulamalı
3. WHEN aynı veri tekrar istendiğinde, THE Sistem SHALL cache mekanizması kullanmalı
4. WHEN paralel istek yapılırken, THE Sistem SHALL throttling mekanizması uygulamalı
5. WHEN büyük veri seti çekilirken, THE Sistem SHALL streaming response kullanmalı

## Teknik Kısıtlamalar

1. SQL sorguları Mikro ERP'nin SqlVeriOkuV2 endpoint'i üzerinden çalıştırılmalı
2. Field isimleri case-insensitive olarak parse edilmeli
3. Decimal değerler Türk kültür ayarlarına (virgül ayracı) uygun parse edilmeli
4. Timeout değeri maksimum 30 saniye olmalı
5. Cache süresi maksimum 5 dakika olmalı
6. Sayfa başına maksimum 100 ürün çekilmeli

## Güvenlik Gereksinimleri

1. SQL injection saldırılarına karşı parametreli sorgular kullanılmalı
2. API şifreleri environment variable'larda saklanmalı
3. Hassas veriler loglara yazılmamalı
4. Rate limiting uygulanmalı
5. HTTPS zorunlu olmalı

## Başarı Kriterleri

1. Stok bilgileri %100 doğrulukla görüntülenmeli
2. Fiyat bilgileri %100 doğrulukla görüntülenmeli
3. Ürün çekme işlemi 10 saniyeden kısa sürmeli (100 ürün için)
4. Hata oranı %1'in altında olmalı
5. Sistem 7/24 çalışır durumda olmalı
