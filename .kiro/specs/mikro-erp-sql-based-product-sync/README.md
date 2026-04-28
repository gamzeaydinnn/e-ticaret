# Mikro ERP SQL Tabanlı Ürün Senkronizasyonu

## 📌 Proje Özeti

Bu proje, Mikro ERP'den ürün çekerken yaşanan **stok ve fiyat bilgilerinin 0 olarak gelmesi** veya **yanlış görüntülenmesi** sorununu çözmek için geliştirilmiştir.

## 🔴 Sorun

### Mevcut Durum

- Mikro ERP API'den ürünler çekiliyor ✅
- **Stok miktarları 0 olarak görünüyor** ❌
- **Fiyat bilgileri yanlış field'lerden okunuyor** ❌
- Field mapping hatası var (msg_S_XXXX formatı) ❌
- `sto_webe_gonderilecek_fl` alanı kontrol edilmiyor ❌

### Örnek Senaryo

```
Mikro ERP'de:
- Stok Kodu: ABC123
- Stok Miktarı: 50 adet
- Fiyat: 125.50 TL

Sistemde Görünen:
- Stok Kodu: ABC123
- Stok Miktarı: 0 adet ❌
- Fiyat: 0.00 TL ❌
```

## 💡 Çözüm

### Yaklaşım

1. **SQL Tabanlı Veri Çekme:** API endpoint yerine doğrudan SQL sorgusu
2. **Birleşik Sorgu:** Tüm gerekli tabloları JOIN et
3. **Geliştirilmiş Parsing:** Field isimlerini doğru map et
4. **Cache Mekanizması:** Performans optimizasyonu

### Teknik Detaylar

**SQL Sorgusu:**

```sql
-- Fiyat listesi + Depo dağılım + Stok kartları
STOK_SATIS_FIYAT_LISTELERI_YONETIM (msg_S_0002 → Fiyat)
+ fn_Stok_Depo_Dagilim (msg_S_0343 → Stok Miktarı)
+ STOKLAR (sto_webe_gonderilecek_fl → Web Aktiflik)
+ STOK_HAREKETLERI (Son hareket tarihi)
```

**Field Mapping:**
| Mikro Field | Sistem Field | Açıklama |
|-------------|--------------|----------|
| msg_S_0001 | StokKod | Stok kodu |
| msg_S_0002 | Fiyat | Satış fiyatı |
| msg_S_0343 | StokMiktar | Kullanılabilir stok |
| msg_S_0005 | UrunAdi | Ürün adı |
| sto_webe_gonderilecek_fl | IsWebActive | Web'e gönderilecek mi |

## 📁 Dökümanlar

### 1. [requirements.md](./requirements.md)

Detaylı gereksinim analizi ve kabul kriterleri

**İçerik:**

- 8 ana gereksinim
- EARS formatında kabul kriterleri
- Teknik kısıtlamalar
- Güvenlik gereksinimleri

### 2. [design.md](./design.md)

Teknik tasarım ve mimari

**İçerik:**

- Mimari diyagramlar
- SQL sorgu yapısı
- Field mapping tablosu
- Parser mantığı
- Cache stratejisi
- Hata yönetimi

### 3. [tasks.md](./tasks.md)

İmplementasyon görev listesi

**İçerik:**

- 12 ana görev grubu
- 60+ alt görev
- Gereksinim referansları
- Checkbox'lar ile ilerleme takibi

### 4. [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

Detaylı implementasyon planı

**İçerik:**

- 10 fazlı implementasyon
- Zaman tahminleri (27 saat)
- Risk analizi
- Başarı kriterleri
- Rollback planı

## 🚀 Hızlı Başlangıç

### Önkoşullar

- Mikro ERP API erişimi
- SqlVeriOkuV2 endpoint'i aktif
- .NET 6.0+
- React 18+

### Implementasyon Sırası

1. **Backend - DTO Modelleri** (2 saat)

   ```bash
   # MikroUrunDto.cs oluştur
   cd src/ECommerce.Infrastructure/DTOs
   ```

2. **Backend - SQL Query Builder** (3 saat)

   ```bash
   # MicroService.cs güncelle
   cd src/ECommerce.Infrastructure/Services/MicroServices
   ```

3. **Backend - Response Parser** (4 saat)

   ```bash
   # Parse metodları ekle
   # MicroService.cs
   ```

4. **Backend - Controller** (2 saat)

   ```bash
   # AdminMicroController.cs güncelle
   cd src/ECommerce.API/Controllers/Admin
   ```

5. **Frontend - UI** (3 saat)

   ```bash
   # AdminMicro.js güncelle
   cd frontend/src/pages/Admin
   ```

6. **Test** (7 saat)

   ```bash
   # Unit + Integration testler
   cd tests
   ```

7. **Deployment** (2 saat)
   ```bash
   # Staging → Production
   ```

## 📊 İlerleme Takibi

### Faz Durumu

- [ ] Faz 1: DTO Güncellemeleri
- [ ] Faz 2: SQL Query Builder
- [ ] Faz 3: Response Parser
- [ ] Faz 4: Service Metodları
- [ ] Faz 5: Controller Güncellemeleri
- [ ] Faz 6: Frontend Güncellemeleri
- [ ] Faz 7: Unit Testler
- [ ] Faz 8: Integration Testler
- [ ] Faz 9: Dokümantasyon
- [ ] Faz 10: Deployment

### Başarı Metrikleri

- [ ] Stok bilgileri %100 doğru
- [ ] Fiyat bilgileri %100 doğru
- [ ] API response time < 2 saniye
- [ ] Unit test coverage > %80
- [ ] Hata oranı < %1

## 🎯 Beklenen Sonuçlar

### Önce (Mevcut Durum)

```
Ürün Listesi:
┌─────────┬──────────┬────────┐
│ Ürün    │ Stok     │ Fiyat  │
├─────────┼──────────┼────────┤
│ ABC123  │ 0 ❌     │ 0.00 ❌│
│ DEF456  │ 0 ❌     │ 0.00 ❌│
│ GHI789  │ 0 ❌     │ 0.00 ❌│
└─────────┴──────────┴────────┘
```

### Sonra (Hedef Durum)

```
Ürün Listesi:
┌─────────┬──────────┬────────────┐
│ Ürün    │ Stok     │ Fiyat      │
├─────────┼──────────┼────────────┤
│ ABC123  │ 50 ✅    │ 125.50 ✅  │
│ DEF456  │ 23 ✅    │ 89.90 ✅   │
│ GHI789  │ 0 ✅     │ 45.00 ✅   │
└─────────┴──────────┴────────────┘
```

## 🔧 Teknik Detaylar

### Kullanılan Teknolojiler

- **Backend:** ASP.NET Core 6.0, Entity Framework Core
- **Frontend:** React 18, Bootstrap 5
- **Database:** SQL Server (Mikro ERP)
- **Cache:** IMemoryCache
- **Logging:** Serilog / Application Insights

### Mimari Kararlar

1. **SQL Tabanlı Yaklaşım:** API endpoint sınırlamalarını aşmak için
2. **Birleşik Sorgu:** Performans optimizasyonu için tek sorgu
3. **Cache Mekanizması:** 5 dakika TTL ile gereksiz API çağrılarını önleme
4. **Fallback Mekanizması:** API hatalarında cache'den son başarılı sonuç

### Güvenlik

- ✅ Parametreli SQL sorguları (SQL injection koruması)
- ✅ HTTPS zorunlu
- ✅ API key rotation
- ✅ Rate limiting
- ✅ Input validation

## 📞 Destek

### Sorun Giderme

1. **Stok hala 0 görünüyor:**
   - Cache'i temizle (Ctrl+F5)
   - Mikro ERP'de `msg_S_0343` alanını kontrol et
   - Backend loglarını incele

2. **Fiyat hala 0 görünüyor:**
   - Fiyat listesi numarasını kontrol et
   - Mikro ERP'de `msg_S_0002` alanını kontrol et
   - Backend loglarını incele

3. **API timeout:**
   - SQL sorgusu performansını kontrol et
   - Index'leri kontrol et
   - Timeout değerini artır (appsettings.json)

### Loglama

```bash
# Backend logları
tail -f logs/microservice-{date}.log

# Frontend console
F12 → Console → Filter: "MicroService"
```

## 📚 Referanslar

- [Mikro ERP API Dokümantasyonu](https://mikro.com/api-docs)
- [EARS Requirements Pattern](https://www.incose.org/ears)
- [SQL Server Best Practices](https://docs.microsoft.com/sql)
- [React Best Practices](https://react.dev/learn)

## 👥 Ekip

- **Backend Developer:** SQL sorguları, parser, service metodları
- **Frontend Developer:** UI güncellemeleri, hata yönetimi
- **QA Engineer:** Test senaryoları, manuel testler
- **DevOps Engineer:** Deployment, monitoring

## 📅 Zaman Çizelgesi

| Gün | Faz                              | Durum       |
| --- | -------------------------------- | ----------- |
| 1   | Backend (Faz 1-4)                | ⏳ Bekliyor |
| 2   | Frontend + Testler (Faz 5-7)     | ⏳ Bekliyor |
| 3   | Integration Test + Dok (Faz 8-9) | ⏳ Bekliyor |
| 4   | Deployment (Faz 10)              | ⏳ Bekliyor |

**Tahmini Tamamlanma:** 4 iş günü

---

## ✅ Sonraki Adımlar

1. [ ] Ekip toplantısı yap
2. [ ] Görev atamaları yap
3. [ ] Development branch oluştur
4. [ ] İlk commit: DTO modelleri
5. [ ] Daily standup'lar başlat

---

**Son Güncelleme:** 2026-04-12  
**Versiyon:** 1.0  
**Durum:** 📋 Planlama Tamamlandı
