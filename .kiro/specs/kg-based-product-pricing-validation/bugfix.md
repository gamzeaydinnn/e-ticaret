# Bugfix Requirements Document

## Introduction

KG bazlı ürünlerde (manav/kasap/şarküteri) fiyatlandırma ve kullanıcı arayüzü tutarsızlığı bulunmaktadır. Ürünün Mikro ERP'deki birim fiyatı (PricePerUnit) backend tarafında doğru bir şekilde hesaplanıp 3D Secure'e gönderilmesine rağmen, frontend tarafında kullanıcıya kg bazlı ürün olduğu yeterince net gösterilmemektedir. ProductCard bileşeninde "₺/kg" etiketi eksiktir ve kullanıcı ürünün kg bazlı olduğunu anlayamamaktadır.

Bu durum şu sorunlara yol açmaktadır:

- Kullanıcı ürün kartında fiyatın kg başına mı yoksa adet başına mı olduğunu anlayamıyor
- Sepette ondalıklı kg miktarı girebilmesine rağmen ürün kartında bu bilgi yok
- Kullanıcı deneyimi tutarsız ve kafa karıştırıcı

**Etkilenen Bileşenler:**

- Frontend: ProductCard.js (fiyat gösterimi)
- Frontend: weightBasedProduct.js (kg ürün tespiti - çalışıyor ✅)
- Backend: WeightBasedProductRules.cs (kg ürün tespiti - çalışıyor ✅)
- Backend: MikroStokMapper.cs (PricePerUnit mapping - çalışıyor ✅)

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN bir ürün kg bazlı ise (örn: "MOR SOĞAN KG", PricePerUnit: 120.95 TL/kg) THEN ProductCard bileşeninde fiyat "120.95 TL" olarak gösteriliyor ve birim bilgisi (₺/kg) gösterilmiyor

1.2 WHEN kullanıcı ProductCard'da kg bazlı bir ürünün fiyatını görüyor THEN kullanıcı fiyatın kg başına mı yoksa adet başına mı olduğunu ayırt edemiyor

1.3 WHEN kullanıcı sepete kg bazlı ürün ekliyor THEN ürün kartında kg bilgisi olmadığı için kullanıcı ondalıklı miktar (1.5 kg) girebileceğini bilmiyor

### Expected Behavior (Correct)

2.1 WHEN bir ürün kg bazlı ise (PricePerUnit > 0 ve isStrictVariableWeightProduct = true) THEN ProductCard bileşeninde fiyat "120.95 ₺/kg" formatında gösterilmelidir

2.2 WHEN kullanıcı ProductCard'da kg bazlı bir ürünün fiyatını görüyor THEN kullanıcı fiyatın kg başına olduğunu açıkça görebilmeli ve anlayabilmeli

2.3 WHEN ProductCard bileşeninde kg bazlı ürün gösteriliyor THEN ürün kartında görsel bir kg ikonu veya badge gösterilmelidir

2.4 WHEN kullanıcı kg bazlı ürünü sepete ekliyor THEN sepette ondalıklı kg miktarı (0.25, 0.5, 1.5 kg gibi) girebilmeli ve bu minimum 0.25 kg olarak sınırlanmalıdır

### Unchanged Behavior (Regression Prevention)

3.1 WHEN ürün adet bazlı ise (WeightUnit = Piece, PricePerUnit = 0) THEN ProductCard'da fiyat "120.95 TL" formatında gösterilmeye devam etmelidir

3.2 WHEN ürün gram/litre bazlı ise (WeightUnit = Gram/Liter) THEN ProductCard'da fiyat normal formatta gösterilmeye devam etmelidir

3.3 WHEN backend'de PricePerUnit hesaplaması yapılıyor THEN MikroStokMapper.MapWeightProperties metodundaki mevcut mantık değiştirilmemelidir

3.4 WHEN sepette kg bazlı ürün miktarı güncellenirken THEN normalizeBackendQuantity fonksiyonundaki 0.25 minimum değer ve 2 basamak yuvarlatma kuralları korunmalıdır

3.5 WHEN sipariş oluşturulurken PreAuthAmount hesaplanıyor THEN OrderManager.CheckoutAsync metodundaki %20 tolerans marjı (finalPrice \* 1.20) korunmalıdır

3.6 WHEN 3D Secure ödeme başlatılıyor THEN PaymentsController.PosnetInitiate3DSecure metodundaki PreAuthAmount kullanımı ve "Auth" işlem tipi seçimi korunmalıdır

3.7 WHEN tartı sonrası fiyat güncellemesi yapılıyor THEN Capture işlemindeki PreAuthAmount ≤ FinalPrice kontrolü korunmalıdır

3.8 WHEN frontend'de isStrictVariableWeightProduct kontrolü yapılıyor THEN weightBasedProduct.js'deki mevcut algılama mantığı (FIXED_WEIGHT_PATTERN, STANDALONE_KG_PATTERN) değiştirilmemelidir

3.9 WHEN ProductCard bileşeninde kampanya badge'leri gösteriliyor THEN mevcut kampanya gösterimi etkilenmemelidir

3.10 WHEN ProductCard bileşeninde favori ve sepete ekle butonları çalışıyor THEN bu butonların işlevselliği değiştirilmemelidir
