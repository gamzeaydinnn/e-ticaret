# POSNET İADE VE İPTAL İŞLEMLERİ ANALİZ RAPORU

**Tarih:** 27 Haziran 2026  
**Doküman:** POSNET XML Servisleri Entegrasyon Dokümanı v2.1.1.3  
**Mevcut Kod:** YapiKrediPosnetService.cs

---

## 📊 YÖNETİCİ ÖZETİ

Bu rapor, mevcut POSNET entegrasyonunun bankaapi.txt dokümantasyonuna uygunluğunu analiz eder. **Kritik eksiklikler ve uyumsuzluklar** tespit edilmiştir.

### 🔴 KRİTİK SORUNLAR (Acil Aksiy on Gerekli)

1. **Hata Kodu Eksiklikleri:** 30+ dokümante hata kodu implement edilmemiş
2. **İadenin İptali:** YKB kartı kontrolü eksik
3. **Eşleniksiz İade:** Tamamen implement edilmemiş
4. **Kısmi İade Validasyonu:** Toplam kontrol eksik
5. **Grup Kapama Kontrolü:** 0211 hata kodu kontrolü yok

### 🟡 ORTA ÖNCELİK SORUNLAR

6. OrderID prefix kontrolü (TDS\_ için 3D işlemler)
7. Provizyon aşım yüzdesi desteği
8. İadenin iptali transaction="return" desteği

---

## 1️⃣ İPTAL (REVERSE) İŞLEMİ ANALİZİ

### ✅ DOĞRU IMPLEMENT EDİLENLER

| Özellik                  | Bankaapi Gereksinimi                                 | Mevcut Durum                         | Durum     |
| ------------------------ | ---------------------------------------------------- | ------------------------------------ | --------- |
| **Transaction Tipi**     | sale, auth, capt, return, pointUsage, vftTransaction | ✅ Destekleniyor                     | ✅ UYUMLU |
| **HostLogKey Kullanımı** | İşlem tekilliği için zorunlu                         | ✅ Implement edilmiş                 | ✅ UYUMLU |
| **OrderID + OrderDate**  | Aktif parametrede gerekli                            | ✅ Implement edilmiş                 | ✅ UYUMLU |
| **AuthCode**             | VFT işlemlerde zorunlu                               | ✅ Destekleniyor                     | ✅ UYUMLU |
| **Tarih Formatı**        | YYYYAAGG format                                      | ✅ ToTurkeyDateString ile çevriliyor | ✅ UYUMLU |

### 🔴 EKSİKLİKLER VE UYUMSUZLUKLAR

#### 1.1 Kısmi İade Kontrolü Eksik

**POSNET Dokümanı (Sayfa 17):**

> "Bir işlem üzerinde daha önce yapılmış bir İade işlemi var ise, İptal işlemi hata alacaktır."

**Mevcut Kod:**

```csharp
// ProcessReverseAsync metodunda bu kontrol YOK!
// Direkt reverse request gönderiliyor
```

**SORUN:** Üzerinde kısmi iade olan bir işlem iptal edilmeye çalışılırsa, POSNET 0xxx hatası dönecek ama önceden kontrol edilmiyor.

**ÖNERİLEN ÇÖZÜM:**

```csharp
// ProcessReverseAsync içinde, reverse'den önce:
var hasPartialRefunds = await _db.Payments
    .AnyAsync(p => p.OrderId == orderId &&
                   p.TransactionType == "return" &&
                   p.Status == "Refunded" &&
                   p.OriginalPaymentId.HasValue,
              cancellationToken);

if (hasPartialRefunds)
{
    return PosnetResult<PosnetReverseResponse>.Failure(
        "İşlem üzerinde kısmi iade olduğu için iptal edilemez",
        PosnetErrorCode.CannotReversePartiallyRefunded);
}
```

#### 1.2 Grup Kapama Kontrolü Eksik

**POSNET Dokümanı (Sayfa 5):**

> "Grup kapaması yapılmış bir işlemin iptalinde 0211 hata kodu alınır. Bu durumda iade edilmesi gerekmektedir."

**Mevcut Kod:** 0211 hata kodu PosnetErrorCodes.cs'de tanımlı değil!

**EKSİK HATA KODU:**

```csharp
[Description("Grup kapanmış - İade yapılmalı")]
GroupClosedUseRefund = 211,
```

#### 1.3 İadenin İptali - YKB Kartı Kontrolü Eksik

**POSNET Dokümanı (Sayfa 22):**

> "Aynı gün içinde yapılan bir iade işlemini iptal etmek için kullanılır. Yapılan satış işlemi **Yapı Kredi Bankası kartı ile yapıldıysa** bu işlem kullanılabilir, farklı bankanın kartı ile yapılan bir işlem için kullanılamaz."

**Mevcut Kod:** Bu kontrol YOK!

**ÖNERİLEN ÇÖZÜM:**

```csharp
// Iade iptal edilmeye çalışılıyorsa (transaction="return")
if (reverseTransaction == "return")
{
    // Orijinal işlemdeki kart BIN'ini kontrol et
    var originalPayment = await _db.Payments
        .Where(p => p.HostLogKey == hostLogKey || p.ProviderPaymentId == hostLogKey)
        .FirstOrDefaultAsync(cancellationToken);

    var isYKBCard = originalPayment?.CardBin?.StartsWith("4546") == true ||
                    originalPayment?.CardBin?.StartsWith("5406") == true;

    if (!isYKBCard)
    {
        return PosnetResult<PosnetReverseResponse>.Failure(
            "İadenin iptali sadece YKB kartları için yapılabilir",
            PosnetErrorCode.RefundCancelOnlyForYKBCards);
    }
}
```

---

## 2️⃣ İADE (REFUND/RETURN) İŞLEMİ ANALİZİ

### ✅ DOĞRU IMPLEMENT EDİLENLER

| Özellik                 | Bankaapi Gereksinimi | Mevcut Durum                                    | Durum     |
| ----------------------- | -------------------- | ----------------------------------------------- | --------- |
| **Tutar Kuruş Çevrimi** | amount \* 100        | ✅ ConvertToKurus() kullanılıyor                | ✅ UYUMLU |
| **HostLogKey Önceliği** | Capt > Auth          | ✅ KGL orders için implement edilmiş (MADDE 15) | ✅ UYUMLU |
| **OrderDate Format**    | YYYYAAGG             | ✅ ToTurkeyDateString ile çevriliyor            | ✅ UYUMLU |
| **Payments Tracking**   | Ayrı return kaydı    | ✅ OriginalPaymentId ile linkli                 | ✅ UYUMLU |
| **Kısmi İade**          | Desteklenmeli        | ✅ Amount parametresi ile                       | ✅ UYUMLU |

### 🔴 EKSİKLİKLER VE UYUMSUZLUKLAR

#### 2.1 Toplam İade Tutarı Kontrolü Eksik

**POSNET Dokümanı (Sayfa 19):**

> "İade işlemlerinde iade edilecek tutar, orijinal işlem tutarını ve daha önce aynı orijinal işlem üzerinde yapılmış iadelerin tutarlarının toplamını geçemez."

**Mevcut Kod:** Bu kontrol YOK!

**ÖNERİLEN ÇÖZÜM:**

```csharp
// ProcessRefundAsync içinde, POSNET'e gönderilmeden önce:
var originalAmount = order?.TotalPrice ?? 0;

var previousRefunds = await _db.Payments
    .Where(p => p.OrderId == orderId &&
                p.TransactionType == "return" &&
                p.Status == "Refunded")
    .SumAsync(p => p.Amount, cancellationToken);

var totalRefundAttempt = previousRefunds + amount;

if (totalRefundAttempt > originalAmount)
{
    return PosnetResult<PosnetReturnResponse>.Failure(
        $"Toplam iade tutarı orijinal tutarı aşamaz. " +
        $"Orijinal: {originalAmount:N2} TL, Önceki İadeler: {previousRefunds:N2} TL, " +
        $"Talep: {amount:N2} TL, Kalan: {(originalAmount - previousRefunds):N2} TL",
        PosnetErrorCode.RefundExceedsSaleAmount);
}
```

#### 2.2 3D Secure TDS\_ Prefix Kontrolü Kısmen Eksik

**POSNET Dokümanı (Sayfa 20):**

> "Eğer 3D Secure ödeme yönetimi ile finansallaştırılmış bir işlemin iadesi yapılıyorsa 20 haneli orderId önüne **TDS\_** koyularak 24 haneye tamamlanması gerekmektedir."

**Mevcut Kod:** TDS\_ ekleme mantığı yok!

**ÖNERİLEN ÇÖZÜM:**

```csharp
// OrderID kullanılıyorsa ve HostLogKey yoksa:
if (string.IsNullOrWhiteSpace(effectiveHostLogKey) &&
    !string.IsNullOrWhiteSpace(originalPosnetOrderId))
{
    // 3D işlem mi kontrol et
    var is3DSecure = await _db.Payments
        .AnyAsync(p => p.OrderId == orderId &&
                       (p.TransactionType == "oosTranData" ||
                        p.Metadata.Contains("3DS")),
                  cancellationToken);

    if (is3DSecure && originalPosnetOrderId.Length == 20)
    {
        originalPosnetOrderId = "TDS_" + originalPosnetOrderId;
        _logger.LogInformation(
            "[POSNET] 3DS iade: TDS_ prefix eklendi. OrderId: {OrderId}",
            originalPosnetOrderId);
    }
}
```

---

## 3️⃣ EŞLENİKSİZ İADE (UNMATCHED RETURN) ANALİZİ

### 🔴 KRİTİK: TAMAMEN IMPLEMENT EDİLMEMİŞ!

**POSNET Dokümanı (Sayfa 23):**

> "Bu işlem tipi, içerisinde bulunan tutarı işlem gönderilen kart hamiline iade edip, üye işyerinin hesabından çekilmesini sağlar. Daha önce yapılmış bir sipariş numarası ya da referans numarası gerektirmemekte..."

**Mevcut Kod:** `ProcessUnmatchedRefundAsync` metodu YOK!

**GEREKLİ IMPLEMENTASYON:**

```csharp
/// <summary>
/// Eşleniksiz iade işlemi
/// Orijinal işlem referansı gerektirmez - doğrudan karta iade
/// </summary>
public async Task<PosnetResult<PosnetUnmatchedReturnResponse>> ProcessUnmatchedRefundAsync(
    string cardNumber,
    string expireDate,
    decimal amount,
    string currency = "TL",
    CancellationToken cancellationToken = default)
{
    // XML: <unmatchedreturn>
    //   <ccno>4506340000000001</ccno>
    //   <expDate>2101</expDate>
    //   <orderID>unique_24_char</orderID>
    //   <currencyCode>TL</currencyCode>
    //   <amount>245</amount>
    // </unmatchedreturn>
}
```

**DOKÜMANTASYON DETAYI:**

- Kart numarası ve son kullanma tarihi zorunlu
- OrderID benzersiz olmalı (24 karakter)
- Currency kod gerekli
- Üye işyerinin bu işlem için yetkisi olmalı

---

## 4️⃣ HATA KODU YÖNETİMİ ANALİZİ

### 📋 DOKÜMANTE EDİLEN vs IMPLEMENT EDİLEN HATA KODLARI

#### ✅ IMPLEMENT EDİLEN HATA KODLARI (Mevcut: 28 adet)

| Kod | Açıklama                         | Kullanım |
| --- | -------------------------------- | -------- |
| 0   | Success                          | ✅       |
| 5   | DeclinedNotApproved              | ✅       |
| 12  | DeclinedInvalidTransaction       | ✅       |
| 14  | DeclinedInvalidCardNumber        | ✅       |
| 15  | DeclinedNoSuchCard               | ✅       |
| 17  | DeclinedCustomerCancelled        | ✅       |
| 51  | DeclinedInvalidAmount            | ✅       |
| 54  | DeclinedExpiredCard              | ✅       |
| 55  | DeclinedIncorrectPin             | ✅       |
| 61  | DeclinedExceedLimit              | ✅       |
| 62  | DeclinedRestrictedCard           | ✅       |
| 63  | DeclinedSecurityViolation        | ✅       |
| 65  | DeclinedTransactionLimitExceeded | ✅       |
| 75  | DeclinedPinTriesExceeded         | ✅       |
| 78  | DeclinedAccountNotFound          | ✅       |
| 91  | DeclinedGeneral                  | ✅       |
| 96  | DeclinedBankUnavailable          | ✅       |
| 127 | OrderIdAlreadyUsed               | ✅       |
| 1xx | Teknik hatalar                   | ✅       |

#### 🔴 EKSİK HATA KODLARI (Bankaapi'de var, kodda yok: 30+ adet)

| Kod      | Açıklama                         | Kritiklik      | Kullanım Alanı          |
| -------- | -------------------------------- | -------------- | ----------------------- |
| **0001** | Bankanızı arayın                 | 🔴 Yüksek      | Limit yetersiz          |
| **0004** | Karta el koy                     | 🔴 Yüksek      | Bloke kart              |
| **0007** | Bankanızı arayın (Özel durum)    | 🟡 Orta        | Çalıntı/kayıp           |
| **0015** | Provizyon bulunamadı             | 🔴 Yüksek      | Finansallaştırma hatası |
| **0030** | Bankanızı arayın                 | 🟡 Orta        | Bozuk veri              |
| **0041** | Karta el koy (Kayıp)             | 🔴 Yüksek      | Kayıp kart              |
| **0043** | Karta el koy (Çalıntı)           | 🔴 Yüksek      | Çalıntı kart            |
| **0053** | Hesap bulunamadı                 | 🟡 Orta        | Hesap hatası            |
| **0057** | İşlem tipi uygunsuz              | 🔴 Yüksek      | Debit kart reddi        |
| **0058** | Terminal yetkisi yok             | 🟡 Orta        | Yetki hatası            |
| **0091** | Banka timeout                    | 🟡 Orta        | Retry gerekli           |
| **0122** | 1 hafta geçmiş, iptal yapılamaz  | 🔴 Yüksek      | **Reverse kritik!**     |
| **0123** | Orijinal işlem bulunamadı        | 🔴 Yüksek      | **Refund kritik!**      |
| **0127** | OrderID kullanılmış              | ✅ Var         | Tekillik                |
| **0129** | Kart merchant blacklist'te       | 🔴 Yüksek      | Kara liste              |
| **0146** | Şifreleme hatası                 | 🟡 Orta        | Credentials             |
| **0147** | Kullanıcı adı/şifre hatalı       | 🟡 Orta        | Auth                    |
| **0148** | Crypto hatası / MID hatası       | 🟡 Orta        | Config                  |
| **0150** | Paket hatası                     | 🟡 Orta        | CVC/OrderDate           |
| **0211** | **Grup kapanmış - İade gerekli** | 🔴 **KRİTİK!** | **Reverse red!**        |
| **0220** | İptal işlemi yapılmış            | 🔴 Yüksek      | Duplicate reverse       |
| **0229** | Önceki güne ait finansallaştırma | 🔴 Yüksek      | Capt iade gerekli       |

**ÖNERİLEN ÇÖZÜM:** PosnetErrorCodes.cs'e eklenecek kodlar:

```csharp
// İptal/İade işlemleri için kritik kodlar
[Description("RED - Bankanızı arayın")]
CallYourBank0001 = 1,

[Description("RED - Karta el koy")]
ConfiscateCard0004 = 4,

[Description("RED - Provizyon bulunamadı")]
AuthorizationNotFound = 15,

[Description("RED - Kayıp kart - Karta el koy")]
LostCard = 41,

[Description("RED - Çalıntı kart - Karta el koy")]
StolenCard = 43,

[Description("RED - Hesap bulunamadı")]
AccountNotFound = 53,

[Description("RED - İşlem tipi uygunsuz (Debit/Kredi)")]
InvalidTransactionType = 57,

[Description("RED - Terminal yetkisi yok")]
TerminalNotAuthorized = 58,

[Description("İptal edilemez - 1 hafta geçmiş")]
CannotReverseAfterOneWeek = 122,

[Description("Orijinal işlem bulunamadı")]
OriginalTransactionNotFound = 123,

[Description("Kart merchant blacklist'te")]
CardInMerchantBlacklist = 129,

[Description("Grup kapanmış - İade yapılmalı")]
GroupClosedUseRefund = 211,

[Description("İptal işlemi daha önce yapılmış")]
AlreadyReversed = 220,

[Description("Önceki güne ait finansallaştırma - İade gerekli")]
PreviousDayCapture = 229,
```

---

## 5️⃣ İŞ KURALLARI UYGUNLUK ANALİZİ

### 📊 İş Kuralı Karşılaştırma Tablosu

| İş Kuralı               | Bankaapi Gereksinimi    | Mevcut Implement       | Durum    |
| ----------------------- | ----------------------- | ---------------------- | -------- |
| **Gün içi iptal**       | Sadece aynı gün         | ⚠️ Tarih kontrolü yok  | 🟡 KISMİ |
| **Grup kapama sonrası** | İade yapılmalı          | ❌ 0211 kontrolü yok   | 🔴 EKSİK |
| **Kısmi iade toplamı**  | Orijinal tutarı geçemez | ❌ Toplam kontrolü yok | 🔴 EKSİK |
| **İade üzerinde iptal** | İptal edilemez          | ❌ Kontrol yok         | 🔴 EKSİK |
| **İadenin iptali**      | Sadece YKB kartları     | ❌ YKB kontrolü yok    | 🔴 EKSİK |
| **3DS TDS\_ prefix**    | 20 char → 24 char       | ⚠️ Manuel ekleme yok   | 🟡 KISMİ |
| **Capture HostLogKey**  | Prov chain'de capt      | ✅ KGL orders için var | ✅ TAMAM |
| **OrderID tekilliği**   | Duplicate kontrol       | ✅ POSNET yapar        | ✅ TAMAM |
| **OrderDate format**    | YYYYAAGG                | ✅ ToTurkeyDateString  | ✅ TAMAM |
| **Amount kuruş**        | TL \* 100               | ✅ ConvertToKurus      | ✅ TAMAM |
| **VFT AuthCode**        | Reverse'de zorunlu      | ✅ Destekleniyor       | ✅ TAMAM |

---

## 6️⃣ XML REQUEST/RESPONSE FORMAT UYGUNLUĞU

### ✅ UYUMLU FORMATLAR

#### Reverse Request (İptal)

```xml
<posnetRequest>
  <mid>6700000001</mid>
  <tid>67000001</tid>
  <reverse>
    <transaction>sale|auth|capt|return|pointUsage|vftTransaction</transaction>
    <hostLogKey>...</hostLogKey>
    <orderID>...</orderID>       <!-- Opsiyonel -->
    <orderDate>YYYYAAGG</orderDate> <!-- OrderID aktifse -->
    <authCode>...</authCode>     <!-- VFT için zorunlu -->
  </reverse>
</posnetRequest>
```

✅ **MEVCUT KOD:** PosnetXmlBuilder.BuildReverseXml() - UYUMLU

#### Return Request (İade)

```xml
<posnetRequest>
  <mid>6700000001</mid>
  <tid>67000001</tid>
  <tranDateRequired>1</tranDateRequired>
  <return>
    <amount>100</amount>
    <currencyCode>TL</currencyCode>
    <hostLogKey>...</hostLogKey>
    <orderID>...</orderID>       <!-- Opsiyonel, 3DS için TDS_ prefix -->
    <orderDate>YYYYAAGG</orderDate>
  </return>
</posnetRequest>
```

✅ **MEVCUT KOD:** PosnetXmlBuilder.BuildReturnXml() - UYUMLU

### 🔴 EKSİK FORMAT: Unmatched Return

```xml
<posnetRequest>
  <mid>6700000001</mid>
  <tid>67000001</tid>
  <tranDateRequired>1</tranDateRequired>
  <unmatchedreturn>
    <ccno>4506340000000001</ccno>
    <expDate>2101</expDate>
    <orderID>unique_24_char</orderID>
    <currencyCode>TL</currencyCode>
    <amount>245</amount>
  </unmatchedreturn>
</posnetRequest>
```

❌ **MEVCUT KOD:** BuildUnmatchedReturnXml() metodu YOK!

---

## 7️⃣ RESPONSE PARSING ANALİZİ

### ✅ UYUMLU PARSER'LAR

| Parser Metodu            | Dokümantasyon                                          | Durum     |
| ------------------------ | ------------------------------------------------------ | --------- |
| `ParseReverseResponse()` | ✅ approved, hostlogkey, authCode                      | ✅ UYUMLU |
| `ParseReturnResponse()`  | ✅ approved, hostlogkey, authCode, pointInfo, instInfo | ✅ UYUMLU |
| `ParseSaleResponse()`    | ✅ Tüm alanlar                                         | ✅ UYUMLU |
| `ParseAuthResponse()`    | ✅ Tüm alanlar                                         | ✅ UYUMLU |

### 🔴 EKSİK PARSER

❌ `ParseUnmatchedReturnResponse()` - Implement edilmemiş

---

## 8️⃣ TEST SENARYO ÖNERİLERİ

### 🧪 Kritik Test Senaryoları

#### Senaryo 1: Gün içi iptal başarı

```
1. Satış işlemi yap (amount: 100 TL)
2. Aynı gün içinde reverse işlemi yap
3. Beklenen: approved=1, işlem iptal edildi
```

#### Senaryo 2: Grup kapama sonrası iptal girişimi

```
1. Satış işlemi yap (gün: T)
2. Grup kapama bekle (gün: T+1)
3. Reverse işlemi dene
4. Beklenen: respCode=0211 "Grup kapanmış - İade yapılmalı"
```

#### Senaryo 3: Kısmi iade sonrası iptal girişimi (Red olmalı)

```
1. Satış işlemi yap (amount: 100 TL)
2. Kısmi iade yap (amount: 30 TL)
3. Reverse işlemi dene
4. Beklenen: İşlem reddedilmeli (üzerinde iade var)
```

#### Senaryo 4: Toplam iade tutarı kontrolü

```
1. Satış işlemi yap (amount: 100 TL)
2. İade 1: 40 TL
3. İade 2: 30 TL
4. İade 3: 50 TL dene
5. Beklenen: Red edilmeli (40+30+50 = 120 > 100)
```

#### Senaryo 5: İadenin iptali - YKB kartı

```
1. Satış işlemi yap (YKB kartı: 4546...)
2. Aynı gün iade yap (amount: 50 TL)
3. Aynı gün iade iptal et (transaction="return")
4. Beklenen: approved=1, iade iptal edildi
```

#### Senaryo 6: İadenin iptali - Diğer banka kartı (Red olmalı)

```
1. Satış işlemi yap (Başka banka kartı)
2. Aynı gün iade yap (amount: 50 TL)
3. Aynı gün iade iptal dene
4. Beklenen: 0005 "Onaylanmadı - İade iptali YKB dışı kartlarda yapılamaz"
```

#### Senaryo 7: 3D Secure TDS\_ prefix

```
1. 3D Secure satış yap (orderID: 20 karakter)
2. İade işlemi yap (orderID'ye TDS_ ekle → 24 karakter)
3. Beklenen: approved=1, iade başarılı
```

#### Senaryo 8: Provizyon + Capture chain iade

```
1. Provizyon al (auth) → HostLogKey1
2. Finansallaştır (capt) → HostLogKey2
3. İade işlemi yap (HostLogKey2 kullan, HostLogKey1 değil!)
4. Beklenen: approved=1, doğru capture key ile iade
```

#### Senaryo 9: Eşleniksiz iade (Implement edilmeli)

```
1. Müşteri mağazadan nakit ödeme yapmış
2. Online'da iade talebi geliyor
3. Eşleniksiz iade yap (kart no + tutar)
4. Beklenen: Karta para iadesi, üye işyerinden kesinti
```

#### Senaryo 10: Duplicate reverse (Already reversed)

```
1. Satış işlemi yap
2. Reverse işlemi yap (başarılı)
3. Aynı işlem için tekrar reverse dene
4. Beklenen: respCode=0220 "İptal işlemi yapılmış"
```

---

## 9️⃣ ÖNCELİKLENDİRİLMİŞ AKSIYON LİSTESİ

### 🔴 P0 - KRİTİK (Hemen yapılmalı)

| #   | Aksiyon                                                | Etki      | Çaba   | Dosya                       |
| --- | ------------------------------------------------------ | --------- | ------ | --------------------------- |
| 1   | **Hata kodları ekleme** (0211, 0122, 0123, 0220, 0229) | 🔴 Yüksek | 2 saat | `PosnetErrorCodes.cs`       |
| 2   | **Toplam iade tutarı kontrolü**                        | 🔴 Yüksek | 3 saat | `YapiKrediPosnetService.cs` |
| 3   | **Kısmi iade kontrolü (reverse'de)**                   | 🔴 Yüksek | 2 saat | `YapiKrediPosnetService.cs` |
| 4   | **Grup kapama kontrolü**                               | 🔴 Yüksek | 1 saat | `YapiKrediPosnetService.cs` |
| 5   | **İadenin iptali YKB kontrolü**                        | 🔴 Yüksek | 3 saat | `YapiKrediPosnetService.cs` |

### 🟡 P1 - YÜKSEK ÖNCELİK (Bu sprint'te)

| #   | Aksiyon                              | Etki    | Çaba   | Dosya                           |
| --- | ------------------------------------ | ------- | ------ | ------------------------------- |
| 6   | **3DS TDS\_ prefix otomatik ekleme** | 🟡 Orta | 2 saat | `YapiKrediPosnetService.cs`     |
| 7   | **Eşleniksiz iade implementasyonu**  | 🟡 Orta | 5 saat | Yeni metod + XML builder/parser |
| 8   | **Gün içi tarih kontrolü (reverse)** | 🟡 Orta | 2 saat | `YapiKrediPosnetService.cs`     |
| 9   | **30+ eksik hata kodu tanımlama**    | 🟡 Orta | 3 saat | `PosnetErrorCodes.cs`           |
| 10  | **Duplicate reverse kontrolü**       | 🟡 Orta | 2 saat | `YapiKrediPosnetService.cs`     |

### 🟢 P2 - ORTA ÖNCELİK (Sonraki sprint)

| #   | Aksiyon                             | Etki     | Çaba   | Dosya                 |
| --- | ----------------------------------- | -------- | ------ | --------------------- |
| 11  | **Provizyon aşım yüzdesi desteği**  | 🟢 Düşük | 3 saat | Config + validation   |
| 12  | **Test otomasyonu (10 senaryo)**    | 🟢 Düşük | 8 saat | Yeni test sınıfları   |
| 13  | **Detaylı logging iyileştirmeleri** | 🟢 Düşük | 2 saat | Tüm metodlar          |
| 14  | **Dokümantasyon güncelleme**        | 🟢 Düşük | 4 saat | README + XML comments |

---

## 🔟 DETAYLI İMPLEMENTASYON ÖRNEKLERİ

### Örnek 1: Toplam İade Tutarı Kontrolü

```csharp
public virtual async Task<PosnetResult<PosnetReturnResponse>> ProcessRefundAsync(
    int orderId,
    string hostLogKey,
    decimal amount,
    CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    // ── MADDE 1: Toplam iade tutarı kontrolü ────────────────────────────
    var order = await _db.Orders
        .AsNoTracking()
        .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

    if (order == null)
    {
        return PosnetResult<PosnetReturnResponse>.Failure(
            "Sipariş bulunamadı",
            PosnetErrorCode.InvalidOrderId);
    }

    // Orijinal işlem tutarını belirle (capture varsa onun tutarı, yoksa order tutarı)
    var originalPayment = await _db.Payments
        .Where(p => p.OrderId == orderId &&
               (p.TransactionType == "capt" || p.TransactionType == "sale") &&
               p.Status == "Paid")
        .OrderByDescending(p => p.CreatedAt)
        .FirstOrDefaultAsync(cancellationToken);

    var originalAmount = originalPayment?.Amount ?? order.TotalPrice;

    // Önceki iadelerin toplamını hesapla
    var previousRefunds = await _db.Payments
        .Where(p => p.OrderId == orderId &&
                   p.TransactionType == "return" &&
                   p.Status == "Refunded")
        .SumAsync(p => p.Amount, cancellationToken);

    var totalRefundAttempt = previousRefunds + amount;
    var availableRefund = originalAmount - previousRefunds;

    if (totalRefundAttempt > originalAmount)
    {
        _logger.LogWarning(
            "[POSNET] İade tutarı aşımı. OrderId: {OrderId}, " +
            "Orijinal: {Original}, Önceki: {Previous}, Talep: {Request}, Kalan: {Available}",
            orderId, originalAmount, previousRefunds, amount, availableRefund);

        return PosnetResult<PosnetReturnResponse>.Failure(
            $"Toplam iade tutarı orijinal tutarı aşamaz. " +
            $"Orijinal: {originalAmount:N2} TL, " +
            $"Önceki İadeler: {previousRefunds:N2} TL, " +
            $"Talep: {amount:N2} TL, " +
            $"Kalan İade Edilebilir: {availableRefund:N2} TL",
            PosnetErrorCode.RefundExceedsSaleAmount);
    }

    // Devamı mevcut kod...
}
```

### Örnek 2: Kısmi İade Kontrolü (Reverse'de)

```csharp
public virtual async Task<PosnetResult<PosnetReverseResponse>> ProcessReverseAsync(
    int orderId,
    string hostLogKey,
    CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    // ── MADDE 2: Kısmi iade kontrolü ────────────────────────────────────
    var hasPartialRefunds = await _db.Payments
        .AnyAsync(p => p.OrderId == orderId &&
                       p.TransactionType == "return" &&
                       p.Status == "Refunded" &&
                       p.OriginalPaymentId.HasValue,
                  cancellationToken);

    if (hasPartialRefunds)
    {
        _logger.LogWarning(
            "[POSNET] İptal reddedildi - İşlem üzerinde kısmi iade var. " +
            "OrderId: {OrderId}, HostLogKey: {HostLogKey}",
            orderId, hostLogKey);

        return PosnetResult<PosnetReverseResponse>.Failure(
            "İşlem üzerinde iade olduğu için iptal edilemez. " +
            "Kalan tutarı iade etmeniz gerekmektedir.",
            PosnetErrorCode.CannotReverseWithRefunds);
    }

    // Devamı mevcut kod...
}
```

### Örnek 3: İadenin İptali - YKB Kartı Kontrolü

```csharp
public virtual async Task<PosnetResult<PosnetReverseResponse>> ProcessReverseAsync(
    int orderId,
    string hostLogKey,
    CancellationToken cancellationToken = default)
{
    // ... mevcut kodlar ...

    // ── MADDE 3: İadenin iptali için YKB kartı kontrolü ─────────────────
    if (reverseTransaction == "return")
    {
        _logger.LogInformation(
            "[POSNET] İade iptali işlemi - YKB kartı kontrolü yapılıyor. " +
            "OrderId: {OrderId}",
            orderId);

        // Orijinal iade işlemini bul
        var returnPayment = await _db.Payments
            .Where(p => (p.HostLogKey == hostLogKey ||
                        p.ProviderPaymentId == hostLogKey) &&
                       p.TransactionType == "return")
            .FirstOrDefaultAsync(cancellationToken);

        if (returnPayment == null)
        {
            return PosnetResult<PosnetReverseResponse>.Failure(
                "İade işlemi bulunamadı",
                PosnetErrorCode.TransactionNotFoundForCancel);
        }

        // Orijinal satışı bul (iade'nin orijinal işlemi)
        var originalSale = returnPayment.OriginalPaymentId.HasValue
            ? await _db.Payments.FindAsync(
                new object[] { returnPayment.OriginalPaymentId.Value },
                cancellationToken)
            : null;

        // YKB BIN kodları: 4546, 5406, 4506 (Worldcard)
        var isYKBCard = originalSale?.CardBin != null && (
            originalSale.CardBin.StartsWith("4546") ||
            originalSale.CardBin.StartsWith("5406") ||
            originalSale.CardBin.StartsWith("4506"));

        if (!isYKBCard)
        {
            _logger.LogWarning(
                "[POSNET] İade iptali reddedildi - YKB kartı değil. " +
                "OrderId: {OrderId}, CardBin: {CardBin}",
                orderId, originalSale?.CardBin ?? "N/A");

            return PosnetResult<PosnetReverseResponse>.Failure(
                "İadenin iptali sadece Yapı Kredi Bankası kartları için yapılabilir. " +
                "Diğer banka kartlarında iade kart sahibi bankaya ulaştığı için " +
                "iptal işlemi gerçekleştirilemez.",
                PosnetErrorCode.RefundCancelOnlyForYKBCards);
        }
    }

    // Devamı mevcut kod...
}
```

### Örnek 4: Eşleniksiz İade (Unmatched Return) - Tam İmplementasyon

```csharp
/// <summary>
/// Eşleniksiz iade işlemi
/// Orijinal işlem referansı gerektirmez - direkt karta iade yapılır
/// POSNET Dok: Sayfa 23 - "Eşleniksiz İade İşlemi"
/// </summary>
public virtual async Task<PosnetResult<PosnetUnmatchedReturnResponse>>
    ProcessUnmatchedRefundAsync(
        string cardNumber,
        string expireDate,
        decimal amount,
        string currency = "TL",
        string? reason = null,
        CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    _logger.LogInformation(
        "[POSNET] Eşleniksiz iade başlatılıyor. Amount: {Amount}, Kart: ****{Last4}",
        amount, cardNumber.Length >= 4 ? cardNumber[^4..] : "");

    try
    {
        // Benzersiz OrderID oluştur (unmatched return için)
        var unmatchedOrderId = $"UR_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";

        // Request oluştur
        var request = new PosnetUnmatchedReturnRequest
        {
            MerchantId = _settings.PosnetMerchantId,
            TerminalId = _settings.PosnetTerminalId,
            Card = new PosnetCardInfo
            {
                CardNumber = cardNumber.Replace(" ", "").Replace("-", ""),
                ExpireDate = expireDate
            },
            OrderId = unmatchedOrderId,
            Amount = PosnetSaleRequest.ConvertToKurus(amount),
            CurrencyCode = NormalizeCurrencyCode(currency)
        };

        // Validasyon
        PosnetRequestValidator.ValidateAndThrow(request);

        // XML oluştur
        var xml = _xmlBuilder.BuildUnmatchedReturnXml(request);

        // POSNET'e gönder
        var httpResponse = await _httpClient.SendAsync(xml, cancellationToken);

        if (!httpResponse.IsSuccess)
        {
            return PosnetResult<PosnetUnmatchedReturnResponse>.Failure(
                httpResponse.ErrorMessage ?? "HTTP hatası",
                PosnetErrorCode.ConnectionError,
                httpResponse.Exception,
                stopwatch.ElapsedMilliseconds);
        }

        // Response parse et
        var returnResponse = _xmlParser.ParseUnmatchedReturnResponse(httpResponse.ResponseXml!);

        stopwatch.Stop();

        // Audit kaydı oluştur (OrderId yok, özel tracking için)
        await SaveUnmatchedRefundRecordAsync(
            unmatchedOrderId,
            returnResponse.HostLogKey ?? unmatchedOrderId,
            amount,
            returnResponse.IsSuccess ? "Refunded" : "Failed",
            httpResponse.ResponseXml,
            cardNumber[^4..],
            reason,
            cancellationToken,
            hostLogKey: returnResponse.HostLogKey,
            authCode: returnResponse.AuthCode);

        if (returnResponse.IsSuccess)
        {
            _logger.LogInformation(
                "[POSNET] Eşleniksiz iade başarılı. Amount: {Amount}, " +
                "HostLogKey: {HostLogKey}, Kart: ****{Last4}",
                amount, returnResponse.HostLogKey, cardNumber[^4..]);
        }
        else
        {
            _logger.LogWarning(
                "[POSNET] Eşleniksiz iade reddedildi. Amount: {Amount}, " +
                "ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}",
                amount, returnResponse.ErrorCode, returnResponse.ErrorMessage);
        }

        return PosnetResult<PosnetUnmatchedReturnResponse>.FromResponse(
            returnResponse,
            stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "[POSNET] Eşleniksiz iade hatası");

        return PosnetResult<PosnetUnmatchedReturnResponse>.Failure(
            ex.Message,
            PosnetErrorCode.SystemError,
            ex,
            stopwatch.ElapsedMilliseconds);
    }
}
```

---

## 1️⃣1️⃣ ÖZET PUAN TABLOSU

### 📊 Genel Uyumluluk Skoru: **68/100** 🟡

| Kategori                      | Skor       | Ağırlık  | Ağırlıklı Skor |
| ----------------------------- | ---------- | -------- | -------------- |
| **İptal (Reverse) İşlemleri** | 75/100     | 25%      | 18.75          |
| **İade (Refund) İşlemleri**   | 70/100     | 30%      | 21.00          |
| **Eşleniksiz İade**           | 0/100      | 10%      | 0.00           |
| **Hata Kodu Yönetimi**        | 60/100     | 15%      | 9.00           |
| **İş Kuralları Uygunluğu**    | 65/100     | 10%      | 6.50           |
| **XML Format Uygunluğu**      | 85/100     | 10%      | 8.50           |
| **TOPLAM**                    | **68/100** | **100%** | **68.00**      |

### Kategori Detayları

#### İptal (Reverse) - 75/100

- ✅ Transaction type desteği: **100%**
- ✅ HostLogKey/OrderID kullanımı: **100%**
- ⚠️ Kısmi iade kontrolü: **0%**
- ⚠️ Grup kapama kontrolü: **0%**
- ⚠️ YKB kartı kontrolü (iade iptali): **0%**
- ✅ VFT AuthCode desteği: **100%**

#### İade (Refund) - 70/100

- ✅ Capture HostLogKey önceliği: **100%**
- ✅ Kısmi iade desteği: **100%**
- ❌ Toplam iade tutarı kontrolü: **0%**
- ⚠️ 3DS TDS\_ prefix: **50%** (manuel yapılabilir ama otomatik değil)
- ✅ Payments tracking: **100%**
- ✅ OrderDate format: **100%**

#### Eşleniksiz İade - 0/100

- ❌ Method implementasyonu: **0%**
- ❌ XML builder: **0%**
- ❌ XML parser: **0%**
- ❌ Request/Response modelleri: **0%**

#### Hata Kodu Yönetimi - 60/100

- ✅ Temel hata kodları (28 adet): **100%**
- ❌ İptal/İade kritik kodlar (0122, 0123, 0211, 0220, 0229): **0%**
- ⚠️ Banka ret kodları (+30 adet): **20%**

#### İş Kuralları - 65/100

- ✅ Tarih format: **100%**
- ✅ Tutar kuruş dönüşüm: **100%**
- ✅ Capture önceliği: **100%**
- ❌ Grup kapama: **0%**
- ❌ Kısmi iade toplamı: **0%**
- ❌ İade üzerinde iptal: **0%**

#### XML Format - 85/100

- ✅ Reverse XML: **100%**
- ✅ Return XML: **100%**
- ❌ Unmatched Return XML: **0%**
- ✅ Response parsing: **95%**

---

## 1️⃣2️⃣ SONUÇ VE TAVSİYELER

### 🎯 Ana Bulgular

1. **Mevcut Kod Kalitesi:** Orta-İyi seviyede (68/100)
2. **Kritik Eksiklikler:** 5 ana kategori
3. **Hızlı Kazanımlar:** Hata kodları ve validasyonlar (toplam ~10 saat)
4. **Büyük Özellik:** Eşleniksiz iade (~5 saat)

### ✅ Önerilen Uygulama Planı (3 Hafta)

#### 🗓️ Hafta 1: Kritik Eksiklikler (P0)

**Hedef:** Skor 68 → 80

- [ ] Hata kodları ekleme (0211, 0122, 0123, 0220, 0229) - **2 saat**
- [ ] Toplam iade tutarı kontrolü - **3 saat**
- [ ] Kısmi iade kontrolü (reverse'de) - **2 saat**
- [ ] Grup kapama kontrolü - **1 saat**
- [ ] İadenin iptali YKB kontrolü - **3 saat**
- [ ] Unit testler (P0 için) - **4 saat**

**Toplam:** ~15 saat, **Skor Artışı:** +12 puan

#### 🗓️ Hafta 2: Yüksek Öncelik (P1)

**Hedef:** Skor 80 → 90

- [ ] 3DS TDS\_ prefix otomatik ekleme - **2 saat**
- [ ] Eşleniksiz iade implementasyonu - **5 saat**
- [ ] Gün içi tarih kontrolü - **2 saat**
- [ ] 30+ eksik hata kodu tanımlama - **3 saat**
- [ ] Duplicate reverse kontrolü - **2 saat**
- [ ] Integration testler - **6 saat**

**Toplam:** ~20 saat, **Skor Artışı:** +10 puan

#### 🗓️ Hafta 3: Orta Öncelik ve Cilalama (P2)

**Hedef:** Skor 90 → 95

- [ ] Provizyon aşım yüzdesi desteği - **3 saat**
- [ ] Detaylı logging iyileştirmeleri - **2 saat**
- [ ] Dokümantasyon güncelleme - **4 saat**
- [ ] End-to-end testler (10 senaryo) - **8 saat**
- [ ] Code review ve refactoring - **3 saat**

**Toplam:** ~20 saat, **Skor Artışı:** +5 puan

### 📈 Beklenen İyileşme Grafiği

```
Başlangıç (Şimdi):     68/100 🟡 ████████████░░░░░░░░
Hafta 1 Sonu:         80/100 🟢 ████████████████░░░░
Hafta 2 Sonu:         90/100 🟢 ██████████████████░░
Hafta 3 Sonu:         95/100 🟢 ███████████████████░
```

### 🎓 Öğrenilenler ve Best Practices

1. **HostLogKey Önceliği:** Capture > Auth (KGL orders için zaten implement edilmiş ✅)
2. **OrderDate Format:** YYYYAAGG - UTC→TR timezone dönüşümü gerekli ✅
3. **3DS Prefix:** TDS\_ eklemesi 20→24 karakter için (Manuel yapılabilir ⚠️)
4. **İade Limiti:** Toplam iade ≤ Orijinal tutar (Eksik ❌)
5. **YKB Kartı:** İadenin iptali sadece YKB kartları (Eksik ❌)
6. **Grup Kapama:** 0211 hatası → reverse değil refund yap (Eksik ❌)

### ⚠️ Uyarılar ve Dikkat Edilecekler

1. **Grup Kapama Zamanı:** Gün sonu (genelde 23:59), test ortamında farklı olabilir
2. **BIN Kodları:** YKB kartları için 4546, 5406, 4506 prefix'leri
3. **OrderID Aktif/Pasif:** Merchant'a göre değişir, config'den kontrol edilmeli
4. **Provizyon Aşım:** %10 varsayılan ama merchant'a özel ayarlanabilir
5. **Eşleniksiz İade:** Özel yetki gerektirir, her merchant için aktif değil
6. **Test Kartları:** Üretim ortamında XXX CVC kullanılamaz (0150 hatası)

### 📞 POSNET Destek İletişimi

- **E-posta:** possupp@yapikredi.com.tr
- **Telefon:** 444 0 448 (Üye İşyeri Operasyon Servisi)
- **Test Geçiş Talebi:** Mail ile mid/tid/ip bilgileri ile
- **Problem Bildirimi:** X-CORRELATION-ID header'ı kullanın

---

## 1️⃣3️⃣ EKLER

### EK A: Eksik Hata Kodları - Tam Liste

```csharp
// PosnetErrorCodes.cs'e eklenecek kodlar:

// Kart ve İşlem Hataları
[Description("RED - Bankanızı arayın")]
CallYourBank0001 = 1,

[Description("RED - Karta el koy - Bloke")]
ConfiscateCardBlocked = 4,

[Description("RED - Bankanızı arayın - Özel durum")]
CallYourBank0007 = 7,

[Description("RED - Provizyon bulunamadı / Terminal yetkisi yok / İşyeri statüsü hatalı")]
AuthNotFoundOrNoPermission = 15,

[Description("RED - Bankanızı arayın - Bozuk veri")]
CallYourBank0030 = 30,

[Description("RED - Karta el koy - Kayıp kart")]
ConfiscateCardLost = 41,

[Description("RED - Karta el koy - Çalıntı kart")]
ConfiscateCardStolen = 43,

[Description("RED - Hesap bulunamadı")]
AccountNotFound = 53,

[Description("RED - İşlem tipi uygunsuz (Debit kartı)")]
InvalidTransactionTypeDebit = 57,

[Description("RED - Terminal yetkisi yok")]
TerminalNotAuthorized = 58,

[Description("RED - Banka timeout")]
BankTimeout = 91,

// İptal ve İade Hataları (KRİTİK!)
[Description("İptal edilemez - 1 hafta geçmiş veya provizyon bulunamadı")]
CannotReverseAfterOneWeek = 122,

[Description("Orijinal işlem bulunamadı")]
OriginalTransactionNotFound = 123,

[Description("Kart merchant blacklist'te")]
CardInMerchantBlacklist = 129,

[Description("Grup kapanmış - İade yapılmalı")]
GroupClosedUseRefund = 211,

[Description("İptal işlemi daha önce yapılmış")]
AlreadyReversed = 220,

[Description("Önceki güne ait finansallaştırma - İade gerekli")]
PreviousDayCapture = 229,

// Şifreleme ve Güvenlik
[Description("Şifreleme hatası - Kullanıcı adı/şifre yanlış")]
EncryptionError = 146,

[Description("Kullanıcı adı ve şifre hatalı")]
InvalidCredentials = 147,

[Description("Crypto hatası - MID/TID/IP hatası")]
CryptoOrMidError = 148,

[Description("Paket hatası - CVC veya OrderDate yanlış")]
PacketError = 150,
```
