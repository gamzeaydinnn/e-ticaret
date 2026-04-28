# Mikro ERP SQL Tabanlı Ürün Senkronizasyonu - Tasarım Dökümanı

## Genel Bakış

Bu tasarım, Mikro ERP'den ürün çekerken yaşanan stok ve fiyat mapping sorunlarını çözmek için SQL tabanlı bir yaklaşım sunmaktadır. Mevcut API endpoint'lerinin döndürdüğü field isimleri ile sistemimizin beklediği field isimleri arasındaki uyumsuzluk, doğrudan SQL sorguları ve geliştirilmiş parsing mantığı ile giderilecektir.

## Mimari

### Mevcut Durum Analizi

**Sorunlar:**

1. `StokListesiV2` API endpoint'inden gelen stok değerleri 0 olarak görünüyor
2. Fiyat bilgileri yanlış field'lerden okunuyor
3. Field isimleri (msg_S_XXXX) ile DTO property'leri arasında mapping hatası var
4. `sto_webe_gonderilecek_fl` alanı kontrol edilmiyor

**Kök Neden:**

- Mikro ERP'nin döndürdüğü JSON response'unda field isimleri `msg_S_0001`, `msg_S_0002` gibi kodlanmış formatta
- Mevcut parsing mantığı bu field isimlerini doğru şekilde map edemiyor
- STOKLAR tablosundaki kritik alanlar (sto_webe_gonderilecek_fl) sorguya dahil edilmemiş

### Hedef Mimari

```
┌─────────────────────────────────────────────────────────────┐
│                     FRONTEND (React)                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  AdminMicro.js                                        │  │
│  │  - Ürün listesi görüntüleme                          │  │
│  │  - Stok/Fiyat bilgilerini doğru gösterme             │  │
│  │  - Filtreleme ve sayfalama                           │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓ HTTP Request
┌─────────────────────────────────────────────────────────────┐
│                  BACKEND (ASP.NET Core)                      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  AdminMicroController                                 │  │
│  │  - /api/admin/micro/stok-listesi endpoint            │  │
│  │  - Request validation                                 │  │
│  │  - Response formatting                                │  │
│  └──────────────────────────────────────────────────────┘  │
│                            ↓                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MicroService (Infrastructure Layer)                  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  SQL Query Builder                              │  │  │
│  │  │  - BuildUnifiedProductQuery()                   │  │  │
│  │  │  - Parametreli sorgu oluşturma                  │  │  │
│  │  │  - Depo/Fiyat listesi filtreleme               │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Response Parser                                │  │  │
│  │  │  - ParseUnifiedProductRows()                    │  │  │
│  │  │  - Field mapping (msg_S_XXXX → DTO)            │  │  │
│  │  │  - Null/empty value handling                    │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  │  ┌────────────────────────────────────────────────┐  │  │
│  │  │  Cache Manager                                  │  │  │
│  │  │  - 5 dakika TTL                                 │  │  │
│  │  │  - Depo/Fiyat bazlı cache key                  │  │  │
│  │  └────────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                            ↓ HTTP Request
┌─────────────────────────────────────────────────────────────┐
│                    MIKRO ERP API                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  SqlVeriOkuV2 Endpoint                                │  │
│  │  - SQL sorgusu çalıştırma                            │  │
│  │  - JSON response döndürme                            │  │
│  └──────────────────────────────────────────────────────┘  │
│                            ↓                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  MIKRO DATABASE                                       │  │
│  │  - STOK_SATIS_FIYAT_LISTELERI_YONETIM               │  │
│  │  - STOKLAR                                            │  │
│  │  - fn_Stok_Depo_Dagilim                             │  │
│  │  - STOK_HAREKETLERI                                  │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Bileşenler ve Arayüzler

### 1. SQL Query Builder

**Sorumluluk:** Mikro ERP veritabanına gönderilecek birleşik SQL sorgusunu oluşturur.

**Metod:**

```csharp
private static string BuildUnifiedProductQuery(
    int? depoNo = null,
    int? fiyatListesiNo = null,
    string stokKod = null,
    string grupKod = null,
    bool? sadeceStoklu = null,
    bool? sadeceAktif = true)
```

**SQL Sorgusu Yapısı:**

```sql
WITH WebAday AS (
    -- Adım 1: Fiyat listesi ve depo dağılımını birleştir
    SELECT
        Y.*,                    -- Fiyat listesi alanları
        D.*,                    -- Depo dağılım alanları
        ROW_NUMBER() OVER (
            PARTITION BY Y.msg_S_0001, Y.msg_S_1266
            ORDER BY (
                SELECT MAX(H.sth_tarih)
                FROM STOK_HAREKETLERI H
                WHERE H.sth_stok_kod = Y.msg_S_0001
            ) DESC
        ) AS rn
    FROM STOK_SATIS_FIYAT_LISTELERI_YONETIM AS Y
    OUTER APPLY (
        SELECT *
        FROM [dbo].[fn_Stok_Depo_Dagilim](Y.msg_S_0001) AS F
        WHERE F.msg_S_0874 = Y.msg_S_1266
    ) AS D
    WHERE Y.msg_S_1266 IN (N'GÖLKÖY ŞUBE DEPO', N'')
      AND EXISTS (
          SELECT 1
          FROM STOK_HAREKETLERI H
          WHERE H.sth_stok_kod = Y.msg_S_0001
            AND H.sth_tarih >= DATEFROMPARTS(YEAR(GETDATE()), 1, 1)
      )
)
SELECT
    W.msg_S_0001 AS stok_kod,           -- Stok kodu
    W.msg_S_0005 AS urun_adi,           -- Ürün adı
    W.msg_S_0002 AS fiyat,              -- Satış fiyatı
    W.msg_S_0343 AS stok_miktar,        -- Kullanılabilir stok
    W.msg_S_1266 AS depo_adi,           -- Depo adı
    W.msg_S_0873 AS depo_no,            -- Depo numarası
    S.sto_webe_gonderilecek_fl AS web_aktif,  -- Web'e gönderilecek mi
    S.sto_birim1_ad AS birim,           -- Birim
    S.sto_grup_kod AS grup_kod          -- Grup kodu
FROM WebAday W
INNER JOIN STOKLAR S ON S.sto_kod = W.msg_S_0001
WHERE W.rn = 1
  AND (@DepoNo IS NULL OR TRY_CONVERT(int, W.msg_S_0873) = @DepoNo)
  AND (@StokKod IS NULL OR W.msg_S_0001 LIKE '%' + @StokKod + '%')
  AND (@GrupKod IS NULL OR S.sto_grup_kod = @GrupKod)
  AND (@SadeceStoklu IS NULL OR
       (@SadeceStoklu = 1 AND TRY_CONVERT(decimal(18,3), W.msg_S_0343) > 0) OR
       (@SadeceStoklu = 0 AND TRY_CONVERT(decimal(18,3), W.msg_S_0343) = 0))
  AND (@SadeceAktif IS NULL OR S.sto_webe_gonderilecek_fl = @SadeceAktif)
ORDER BY (
    SELECT MAX(H.sth_tarih)
    FROM STOK_HAREKETLERI H
    WHERE H.sth_stok_kod = W.msg_S_0001
) DESC
```

**Önemli Noktalar:**

- `ROW_NUMBER()` ile son stok hareketine göre tekilleştirme
- `OUTER APPLY` ile depo dağılım fonksiyonunu birleştirme
- `INNER JOIN STOKLAR` ile web aktiflik kontrolü
- Parametreli sorgu ile SQL injection koruması
- Bu yıl içinde hareketi olan ürünleri filtreleme

### 2. Response Parser

**Sorumluluk:** Mikro ERP'den gelen JSON response'unu parse eder ve DTO'lara dönüştürür.

**Metod:**

```csharp
private List<MikroUrunDto> ParseUnifiedProductRows(string jsonContent)
```

**Field Mapping Tablosu:**

| Mikro Field              | DTO Property | Tip     | Açıklama                    |
| ------------------------ | ------------ | ------- | --------------------------- |
| msg_S_0001               | StokKod      | string  | Stok kodu (primary key)     |
| msg_S_0005               | UrunAdi      | string  | Ürün adı                    |
| msg_S_0002               | Fiyat        | decimal | Satış fiyatı (TL)           |
| msg_S_0343               | StokMiktar   | decimal | Kullanılabilir stok miktarı |
| msg_S_1266               | DepoAdi      | string  | Depo adı                    |
| msg_S_0873               | DepoNo       | int     | Depo numarası               |
| sto_webe_gonderilecek_fl | IsWebActive  | bool    | Web'e gönderilecek mi       |
| sto_birim1_ad            | Birim        | string  | Ölçü birimi                 |
| sto_grup_kod             | GrupKod      | string  | Grup kodu                   |

**Parsing Mantığı:**

```csharp
private MikroUrunDto ParseProductRow(JsonElement row)
{
    return new MikroUrunDto
    {
        StokKod = ReadStringFromRow(row, "stok_kod", "msg_S_0001"),
        UrunAdi = ReadStringFromRow(row, "urun_adi", "msg_S_0005"),
        Fiyat = ParseDecimalFlexible(ReadStringFromRow(row, "fiyat", "msg_S_0002")),
        StokMiktar = ParseDecimalFlexible(ReadStringFromRow(row, "stok_miktar", "msg_S_0343")),
        DepoAdi = ReadStringFromRow(row, "depo_adi", "msg_S_1266"),
        DepoNo = ParseIntFlexible(ReadStringFromRow(row, "depo_no", "msg_S_0873")),
        IsWebActive = ParseBoolFlexible(ReadStringFromRow(row, "web_aktif", "sto_webe_gonderilecek_fl")),
        Birim = ReadStringFromRow(row, "birim", "sto_birim1_ad"),
        GrupKod = ReadStringFromRow(row, "grup_kod", "sto_grup_kod")
    };
}

// Case-insensitive field okuma
private static string ReadStringFromRow(JsonElement row, params string[] fieldNames)
{
    foreach (var fieldName in fieldNames)
    {
        if (TryGetPropertyIgnoreCase(row, fieldName, out var prop))
        {
            return prop.GetString() ?? string.Empty;
        }
    }
    return string.Empty;
}

// Türk kültür ayarlarına uygun decimal parsing
private static decimal ParseDecimalFlexible(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return 0m;

    // Virgül ve nokta ayraçlarını normalize et
    value = value.Replace(',', '.');

    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        return result;

    return 0m;
}
```

### 3. DTO Modelleri

**MikroUrunDto (Yeni):**

```csharp
public class MikroUrunDto
{
    [JsonPropertyName("stok_kod")]
    public string StokKod { get; set; }

    [JsonPropertyName("urun_adi")]
    public string UrunAdi { get; set; }

    [JsonPropertyName("fiyat")]
    public decimal Fiyat { get; set; }

    [JsonPropertyName("stok_miktar")]
    public decimal StokMiktar { get; set; }

    [JsonPropertyName("depo_adi")]
    public string DepoAdi { get; set; }

    [JsonPropertyName("depo_no")]
    public int DepoNo { get; set; }

    [JsonPropertyName("web_aktif")]
    public bool IsWebActive { get; set; }

    [JsonPropertyName("birim")]
    public string Birim { get; set; }

    [JsonPropertyName("grup_kod")]
    public string GrupKod { get; set; }

    // Hesaplanan alanlar
    public bool IsStokta => StokMiktar > 0;
    public string StokDurumu => IsStokta ? "Stokta" : "Stokta Yok";
    public string FiyatFormatli => $"₺{Fiyat:N2}";
}
```

### 4. Cache Manager

**Sorumluluk:** SQL sorgu sonuçlarını cache'ler, gereksiz API çağrılarını önler.

**Cache Stratejisi:**

- **Key Format:** `mikro_products_{depoNo}_{fiyatListesiNo}_{grupKod}_{sadeceStoklu}`
- **TTL:** 5 dakika
- **Invalidation:** Manuel sync butonuna basıldığında veya ürün güncellendiğinde

**Implementasyon:**

```csharp
private readonly IMemoryCache _cache;
private const int CacheDurationMinutes = 5;

public async Task<List<MikroUrunDto>> GetProductsWithCacheAsync(
    int? depoNo = null,
    int? fiyatListesiNo = null,
    string grupKod = null,
    bool? sadeceStoklu = null)
{
    var cacheKey = $"mikro_products_{depoNo}_{fiyatListesiNo}_{grupKod}_{sadeceStoklu}";

    if (_cache.TryGetValue(cacheKey, out List<MikroUrunDto> cachedProducts))
    {
        _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
        return cachedProducts;
    }

    var products = await FetchProductsFromMikroAsync(depoNo, fiyatListesiNo, grupKod, sadeceStoklu);

    _cache.Set(cacheKey, products, TimeSpan.FromMinutes(CacheDurationMinutes));
    _logger.LogInformation("Cache set: {CacheKey}, Count: {Count}", cacheKey, products.Count);

    return products;
}
```

## Veri Modelleri

### Mikro ERP Tablo Yapıları

**STOK_SATIS_FIYAT_LISTELERI_YONETIM:**

- `msg_S_0001`: Stok kodu
- `msg_S_0002`: Satış fiyatı
- `msg_S_0005`: Ürün adı
- `msg_S_1266`: Depo adı
- `msg_S_0873`: Depo numarası

**STOKLAR:**

- `sto_kod`: Stok kodu (primary key)
- `sto_webe_gonderilecek_fl`: Web'e gönderilecek mi (bit)
- `sto_birim1_ad`: Ölçü birimi
- `sto_grup_kod`: Grup kodu

**fn_Stok_Depo_Dagilim:**

- `msg_S_0343`: Kullanılabilir stok miktarı
- `msg_S_0874`: Depo adı (fonksiyon parametresi ile eşleşmeli)

**STOK_HAREKETLERI:**

- `sth_stok_kod`: Stok kodu
- `sth_tarih`: Hareket tarihi

## Hata Yönetimi

### Hata Senaryoları ve Çözümleri

1. **SQL Timeout:**
   - **Sorun:** Sorgu 30 saniyeden uzun sürüyor
   - **Çözüm:** Timeout exception yakalayıp fallback mekanizması devreye girer
   - **Fallback:** Cache'den son başarılı sonuç döndürülür

2. **Parse Hatası:**
   - **Sorun:** JSON response beklenmeyen formatta
   - **Çözüm:** Try-catch ile güvenli parsing, hatalı satırlar atlanır
   - **Log:** Hatalı satır içeriği ve hata mesajı loglanır

3. **Field Bulunamadı:**
   - **Sorun:** Beklenen field JSON'da yok
   - **Çözüm:** Alternatif field isimleri denenir, bulunamazsa default değer kullanılır
   - **Log:** Eksik field uyarısı loglanır

4. **Veri Tutarsızlığı:**
   - **Sorun:** Stok negatif veya fiyat 0
   - **Çözüm:** Validation kuralları uygulanır, geçersiz veriler filtrelenir
   - **Log:** Tutarsız veri uyarısı loglanır

### Loglama Stratejisi

**Log Seviyeleri:**

- **Debug:** SQL sorgusu detayları, parse adımları
- **Information:** Başarılı işlemler, cache hit/miss
- **Warning:** Veri tutarsızlıkları, eksik field'ler
- **Error:** SQL hataları, parse hataları, timeout'lar

**Log Formatı:**

```
[MicroService] {Action} - {Details}
Örnek: [MicroService] SQL Query Executed - DepoNo: 1, Duration: 2.5s, RowCount: 150
```

## Test Stratejisi

### Unit Testler

1. **SQL Query Builder Testleri:**
   - Parametresiz sorgu oluşturma
   - Depo filtreli sorgu oluşturma
   - Grup kodu filtreli sorgu oluşturma
   - SQL injection koruması testi

2. **Parser Testleri:**
   - Geçerli JSON parsing
   - Eksik field handling
   - Null value handling
   - Decimal parsing (virgül/nokta)
   - Boolean parsing (1/0, true/false)

3. **DTO Mapping Testleri:**
   - Field name mapping
   - Type conversion
   - Default value assignment

### Integration Testler

1. **Mikro API Integration:**
   - Gerçek API çağrısı
   - Response parsing
   - Error handling

2. **Cache Integration:**
   - Cache set/get
   - Cache invalidation
   - TTL expiration

3. **End-to-End Test:**
   - Frontend'den backend'e tam akış
   - Ürün listesi görüntüleme
   - Filtreleme ve sayfalama

### Manuel Test Senaryoları

1. **Stok Kontrolü:**
   - Mikro ERP'de stok değiştir
   - Sistemde sync yap
   - Doğru stok görüntülendiğini kontrol et

2. **Fiyat Kontrolü:**
   - Mikro ERP'de fiyat değiştir
   - Sistemde sync yap
   - Doğru fiyat görüntülendiğini kontrol et

3. **Web Aktiflik Kontrolü:**
   - Mikro ERP'de sto_webe_gonderilecek_fl değiştir
   - Sistemde sync yap
   - Ürünün görünürlüğünü kontrol et

## Performans Optimizasyonları

### 1. SQL Sorgu Optimizasyonu

- **Index Kullanımı:** `msg_S_0001`, `sth_tarih`, `sto_kod` alanlarında index olmalı
- **ROW_NUMBER Optimizasyonu:** Partition key'ler doğru seçilmeli
- **EXISTS Kullanımı:** IN yerine EXISTS kullanarak performans artışı

### 2. Sayfalama

- **Sayfa Boyutu:** Maksimum 100 ürün/sayfa
- **Offset-Based:** SQL'de OFFSET-FETCH kullanımı
- **Cursor-Based:** Büyük veri setleri için cursor-based pagination

### 3. Paralel İşleme

- **Batch Processing:** Büyük veri setlerini batch'lere böl
- **Async/Await:** Tüm I/O işlemleri asenkron
- **Throttling:** Saniyede maksimum 5 istek

### 4. Caching

- **Memory Cache:** Sık kullanılan sorgular için
- **Distributed Cache:** Multi-instance senaryolar için Redis
- **Cache Warming:** Uygulama başlangıcında popüler sorguları cache'le

## Güvenlik

### 1. SQL Injection Koruması

- Parametreli sorgular kullan
- User input'ları sanitize et
- Whitelist validation uygula

### 2. API Güvenliği

- HTTPS zorunlu
- API key rotation
- Rate limiting (IP bazlı)
- Request size limiting

### 3. Veri Güvenliği

- Hassas veriler şifrelenmeli
- Loglar sanitize edilmeli
- PII verileri maskelenmeli

## Deployment Stratejisi

### 1. Aşamalı Deployment

**Faz 1: Backend Güncellemesi**

- MicroService.cs güncelleme
- Yeni DTO'lar ekleme
- Unit testler

**Faz 2: Controller Güncellemesi**

- AdminMicroController endpoint güncelleme
- Integration testler

**Faz 3: Frontend Güncellemesi**

- AdminMicro.js güncelleme
- UI testleri

**Faz 4: Production Deployment**

- Staging ortamda test
- Canary deployment
- Full rollout

### 2. Rollback Planı

- Önceki versiyon Docker image'ı hazır tut
- Database migration geri alınabilir olmalı
- Feature flag ile yeni kod devre dışı bırakılabilir

### 3. Monitoring

- Application Insights / Serilog
- SQL query performance monitoring
- API response time tracking
- Error rate alerting

## Sonuç

Bu tasarım, Mikro ERP'den ürün çekerken yaşanan stok ve fiyat mapping sorunlarını kapsamlı bir şekilde çözmektedir. SQL tabanlı yaklaşım, doğrudan veritabanı sorgulama ile veri tutarlılığını garanti ederken, geliştirilmiş parsing mantığı field mapping sorunlarını ortadan kaldırmaktadır.
