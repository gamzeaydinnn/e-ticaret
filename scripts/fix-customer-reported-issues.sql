-- =========================================================================================
-- MÜŞTERİ GERİ BİLDİRİMLERİ DÜZELTME SCRİPTİ (2026-11)
-- 2. Madde (Satıştan kaldırma / silme) HARİÇ:
--  - Kategori Düzeltmeleri
--  - Fiyat Kontrol Bilgileri
--  - Eksik Görsel Güncellemeleri
-- =========================================================================================

-- 1. Kırmızı et altında kalmış Kuru Et ve Pastırma -> Et Şarküteri (CategoryId = 193)
UPDATE Products
SET CategoryId = 193, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%PASTIRMA%' OR Name LIKE '%KURU ET%' OR Name LIKE '%KURUET%')
  AND CategoryId != 193;

-- 2. Alaçatı Damla Sakızlı Kurabiye ve diğer Alaçatı kurabiyeleri -> Fırın & Pastane / Kuru Pasta (CategoryId = 204)
UPDATE Products
SET CategoryId = 204, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%ALACATI%KURABIYE%' OR Name LIKE '%ALAÇATI%KURABİYE%' OR Name LIKE '%DAMLA SAKIZLI KURABİYE%' OR Name LIKE '%DAMLA SAKIZLI KURABIYE%')
  AND CategoryId != 204;

-- 3. Mantı ürünleri -> Dondurulmuş Ürünler / Donuk Hazır Gıda (CategoryId = 241)
UPDATE Products
SET CategoryId = 241, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%MANTI%' OR Name LIKE '%HINGEL%' OR Name LIKE '%HİNGEL%')
  AND Name NOT LIKE '%MANTAR%'
  AND CategoryId != 241;

-- 4. Çiğ Köfte -> Hazır Yemek & Meze (CategoryId = 231)
UPDATE Products
SET CategoryId = 231, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%CIG KOFTE%' OR Name LIKE '%ÇİĞ KÖFTE%' OR Name LIKE '%CIGKOFTE%' OR Name LIKE '%ÇİĞKÖFTE%')
  AND Name NOT LIKE '%BULGUR%' AND Name NOT LIKE '%HARC%' AND Name NOT LIKE '%HARÇ%' AND Name NOT LIKE '%BAHARAT%'
  AND CategoryId != 231;

-- 5. Silivri Aslan Tereyağ -> Tereyağı (CategoryId = 195)
UPDATE Products
SET CategoryId = 195, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%TEREYAG%' OR Name LIKE '%TEREYAĞ%')
  AND (Name LIKE '%SILIVRI%' OR Name LIKE '%SİLİVRİ%' OR Name LIKE '%ARSLAN%' OR Name LIKE '%ASLAN%')
  AND CategoryId != 195;

-- 6. Calippo Dondurma -> Tek Dondurma (CategoryId = 217)
UPDATE Products
SET CategoryId = 217, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE Name LIKE '%CALIPPO%'
  AND CategoryId != 217;

-- 7. Ben Organic Portakal Suyu & İçecekler -> Gazsız İçecek (CategoryId = 185)
UPDATE Products
SET CategoryId = 185, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%BEN ORGANIC%' OR Name LIKE '%BEN ORGANİK%')
  AND CategoryId != 185;

-- 8. Algida Cornetto ve Magnum (Çikolatada kalmış olanlar) -> Tek Dondurma (CategoryId = 217)
UPDATE Products
SET CategoryId = 217, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%CORNETTO%' OR Name LIKE '%CORNET%' OR Name LIKE '%MAGNUM%')
  AND CategoryId NOT IN (216, 217);

-- 9. Milka Choco Snack -> Sütlü Tatlı, Krema & Kaymak (CategoryId = 194)
UPDATE Products
SET CategoryId = 194, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE Name LIKE '%CHOCO SNACK%'
  AND CategoryId != 194;

-- 10. Çikolata altında kalmış İzmir Bombası -> Fırın & Pastane / Tatlı (CategoryId = 206)
UPDATE Products
SET CategoryId = 206, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE (Name LIKE '%BOMBA%' AND (Name LIKE '%IZMIR%' OR Name LIKE '%İZMİR%' OR CategoryId = 245))
  AND Name NOT LIKE '%FASULYE%' AND Name NOT LIKE '%SAMPUAN%' AND Name NOT LIKE '%ŞAMPUAN%'
  AND CategoryId != 206;

-- 11. Arifoğlu Kekik ve Arifoğlu Çubuk Tarçın (Kek ve Krakerde kalanlar) -> Tuz, Baharat & Harç (CategoryId = 214)
UPDATE Products
SET CategoryId = 214, AdminOverrideCategory = 1, UpdatedAt = GETUTCDATE()
WHERE ((Name LIKE '%KEKIK%' OR Name LIKE '%KEKİK%') AND Name NOT LIKE '%ÇAY%' AND Name NOT LIKE '%CAY%')
   OR (Name LIKE '%CUBUK TARCIN%' OR Name LIKE '%ÇUBUK TARÇIN%')
   OR ((Name LIKE '%TARCIN%' OR Name LIKE '%TARÇIN%') AND Name LIKE '%ARIFOGLU%' AND CategoryId != 214);

-- 12. Kellogg's Coco Pops Jumbos için mevcut Coco Pops görselini eşitle
UPDATE Products
SET ImageUrl = '/uploads/products/product_1000395904_20260623072752_21906d20.jpg', UpdatedAt = GETUTCDATE()
WHERE SKU = 'N016703' AND (ImageUrl IS NULL OR ImageUrl = '' OR ImageUrl = '/images/default-product.png');

SELECT p.Id, p.SKU, p.Name, p.CategoryId, c.Name AS CategoryName, p.Price, p.ImageUrl
FROM Products p
LEFT JOIN Categories c ON p.CategoryId = c.Id
WHERE p.Name LIKE '%PASTIRMA%'
   OR p.Name LIKE '%KURU ET%'
   OR p.Name LIKE '%ALACATI%KURABIYE%'
   OR p.Name LIKE '%MANTI%'
   OR p.Name LIKE '%CIG KOFTE%'
   OR p.Name LIKE '%CALIPPO%'
   OR p.Name LIKE '%CHOCO SNACK%'
   OR p.Name LIKE '%BEN ORGANIC%'
   OR p.Name LIKE '%KEKIK%'
   OR p.Name LIKE '%CUBUK TARCIN%';

