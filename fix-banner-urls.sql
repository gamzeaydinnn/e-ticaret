-- Sunucuda çalıştır:
-- Database'deki banner ImageUrl'leri güncelle

-- 1. Mevcut banner'ları listele
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;

-- 2. Container'daki dosya adlarıyla eşleştir
-- Slider Banner #1 (Id=1)
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png'
WHERE Id = 1;

-- Promo Banner #2 (Id=2)
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png'
WHERE Id = 2;

-- Promo Banner #3 (Id=3)
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png'
WHERE Id = 3;

-- Promo Banner #4 (Id=4)
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png'
WHERE Id = 4;

-- 3. Kontrol et
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
