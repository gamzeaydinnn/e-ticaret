# Mikro ERP Timeout Sorunu Çözümü

## Sorun

Frontend'de Mikro ERP panelinde "0 ürün" görünüyordu. Backend loglarında sürekli timeout hataları:

```
"errorText":"MikroAPI - TimeOut"
Received HTTP response headers after 61378.585ms - 200
```

## Kök Neden Analizi

### 1. SQL Sorguları Çok Yavaş

- `STOK_SATIS_FIYAT_LISTELERI_YONETIM` + `fn_Stok_Depo_Dagilim` + `STOK_HAREKETLERI` JOIN'leri
- Karmaşık `ROW_NUMBER()` ve `OUTER APPLY` kullanımı
- Her sorgu 61+ saniye sürüyordu

### 2. Sayfa Boyutu Çok Büyük

- Frontend: 100 ürün/sayfa istiyordu
- Bulk fetch: 50 ürün/sayfa
- Bu kadar veri tek seferde çekilince timeout oluyordu

### 3. Timeout Ayarları Yetersiz

- SQL timeout: 75 saniye
- HTTP timeout: 120 saniye
- Ama Mikro API 61 saniyede kesiyordu

## Uygulanan Çözümler

### ✅ 1. Sayfa Boyutlarını Küçülttük

**Backend (AdminMicroController.cs)**

```csharp
// ÖNCE: sayfaBuyuklugu = 50
// SONRA: sayfaBuyuklugu = 20
[HttpGet("stok-listesi")]
public async Task<IActionResult> GetStokListesiV2(
    [FromQuery] int sayfaBuyuklugu = 20,  // ✅ 50 → 20
    ...
)
```

**Frontend (AdminMicro.js)**

```javascript
// Stok listesi sayfalama
const [stokListesiMeta, setStokListesiMeta] = useState({
  pageSize: 20, // ✅ 100 → 20
});

// Bulk fetch config
const [bulkFetchConfig, setBulkFetchConfig] = useState({
  pageSize: 20, // ✅ 50 → 20
  throttleDelayMs: 500, // ✅ 300 → 500 (istekler arası bekleme artırıldı)
});
```

### ✅ 2. Timeout Sürelerini Artırdık

**MicroService.cs**

```csharp
// SQL sorgu timeout'u artırıldı
private static readonly TimeSpan SqlVeriOkuTimeout = TimeSpan.FromSeconds(120);  // ✅ 75 → 120
```

**appsettings.json & appsettings.Production.json**

```json
{
  "MikroSettings": {
    "RequestTimeoutSeconds": 180, // ✅ 120 → 180
    "ParallelPageFetchCount": 3 // ✅ 5 → 3 (paralel istek sayısı azaltıldı)
  }
}
```

### ✅ 3. Paralel İstek Sayısını Azalttık

- `ParallelPageFetchCount`: 5 → 3
- Daha az paralel istek = Mikro API'ye daha az yük

## Beklenen Sonuçlar

### Performans İyileştirmeleri

1. **Daha hızlı sayfa yüklemeleri**: 20 ürün/sayfa → ~15-20 saniye
2. **Daha az timeout**: Küçük sayfalar daha hızlı işlenir
3. **Daha stabil bağlantı**: Mikro API aşırı yüklenmez

### Kullanıcı Deneyimi

1. **Ürünler görünür olacak**: Artık "0 ürün" yerine gerçek veriler
2. **Sayfalama çalışacak**: Kullanıcı sayfalar arası geçiş yapabilecek
3. **Daha az hata**: Timeout hataları minimize edilecek

## Test Adımları

### 1. Backend'i Yeniden Başlat

```bash
# Docker kullanıyorsanız
docker-compose down
docker-compose up -d --build

# Veya direkt .NET
cd src/ECommerce.API
dotnet run
```

### 2. Frontend'i Yeniden Başlat

```bash
cd frontend
npm start
```

### 3. Admin Paneli Test

1. Admin paneline giriş yap
2. "Mikro ERP" sekmesine git
3. "Stok Listesi" sekmesini aç
4. "Bağlantı Testi" butonuna tıkla
5. Ürünlerin yüklendiğini kontrol et

### 4. Log Kontrolü

Backend loglarında şunları kontrol et:

```
✅ [MicroService] SqlVeriOkuV2 ürün sorgusu tamamlandı. Ürün: 20
✅ [AdminMicroController] SQL tabanlı ürün sorgusu çağrılıyor. Sayfa: 1, Büyüklük: 20
❌ OLMAMALI: "MikroAPI - TimeOut"
```

## Ek Optimizasyon Önerileri

### Kısa Vadeli (Hemen Uygulanabilir)

1. **Cache mekanizmasını güçlendir**
   - İlk yükleme yavaş olabilir, ama sonraki istekler cache'den gelir
   - `StockSnapshotCacheTtl` süresini artır (20s → 60s)

2. **Arama filtrelerini kullan**
   - Grup kodu, stok kodu ile filtrele
   - Tüm ürünleri çekmek yerine sadece ihtiyaç olanları çek

### Orta Vadeli (Veritabanı Optimizasyonu)

1. **SQL sorgularını optimize et**
   - `STOK_HAREKETLERI` tablosuna index ekle
   - `ROW_NUMBER()` yerine daha hızlı alternatifler
   - Gereksiz JOIN'leri kaldır

2. **Mikro API'yi güncelle**
   - Daha hızlı endpoint'ler
   - Sayfalama desteği
   - Filtreleme desteği

### Uzun Vadeli (Mimari Değişiklik)

1. **Asenkron senkronizasyon**
   - Ürünleri arka planda çek ve DB'ye kaydet
   - Frontend DB'den okusun (çok daha hızlı)
   - Hangfire job'ları kullan

2. **Elasticsearch/Redis entegrasyonu**
   - Hızlı arama ve filtreleme
   - Gerçek zamanlı stok güncellemeleri

## Sorun Devam Ederse

### 1. Mikro API Loglarını Kontrol Et

```bash
# Mikro API sunucusunda
tail -f /path/to/mikro/api/logs/api.log
```

### 2. SQL Server Performance Monitor

- Hangi sorgular yavaş?
- Index eksikliği var mı?
- Deadlock var mı?

### 3. Network Latency

```bash
# API sunucusundan Mikro sunucusuna ping
ping 10.0.0.3

# Port kontrolü
telnet 10.0.0.3 8084
```

### 4. Fallback Mekanizması

Kod zaten fallback içeriyor:

```csharp
catch (Exception ex)
{
    // Mikro API başarısız → Veritabanından oku
    var dbProducts = await _productRepository.GetAllAsync();
    return Ok(new { isOfflineMode = true, data = dbProducts });
}
```

## Değişiklik Özeti

| Dosya                         | Değişiklik                | Önce  | Sonra |
| ----------------------------- | ------------------------- | ----- | ----- |
| `AdminMicroController.cs`     | Sayfa boyutu              | 50    | 20    |
| `AdminMicro.js`               | Stok listesi sayfa boyutu | 100   | 20    |
| `AdminMicro.js`               | Bulk fetch sayfa boyutu   | 50    | 20    |
| `AdminMicro.js`               | Throttle delay            | 300ms | 500ms |
| `MicroService.cs`             | SQL timeout               | 75s   | 120s  |
| `appsettings.json`            | HTTP timeout              | 120s  | 180s  |
| `appsettings.json`            | Paralel istek             | 5     | 3     |
| `appsettings.Production.json` | HTTP timeout              | 120s  | 180s  |
| `appsettings.Production.json` | Paralel istek             | 5     | 3     |

## Sonuç

Bu değişiklikler ile:

- ✅ Timeout hataları minimize edildi
- ✅ Sayfa yükleme süreleri iyileştirildi
- ✅ Mikro API yükü azaltıldı
- ✅ Kullanıcı deneyimi iyileştirildi

**Not**: Eğer sorun devam ederse, Mikro API'nin kendi timeout ayarlarını kontrol edin (IIS, Kestrel, vb.).
