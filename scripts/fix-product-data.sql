-- Ürün verilerini düzeltme scripti
-- 1. Temel Gıda kategorisini ekle (eğer yoksa)
-- 2. Bulgur'u Temel Gıda kategorisine taşı
-- 3. Pınar Süt'ün resmini düzelt

-- SQLite için
-- Temel Gıda kategorisi ekle
INSERT OR IGNORE INTO Categories (Name, Description, ImageUrl, Slug, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES ('Temel Gıda', 'Temel gıda ürünleri', '/images/bulgur.png', 'temel-gida', 7, 1, datetime('now'), datetime('now'));

-- Bulgur'un kategorisini güncelle (Temel Gıda kategorisinin ID'sini bul)
UPDATE Products 
SET CategoryId = (SELECT Id FROM Categories WHERE Slug = 'temel-gida'),
    UpdatedAt = datetime('now')
WHERE Slug = 'bulgur-1-kg';

-- Pınar Süt'ün resmini güncelle
UPDATE Products 
SET ImageUrl = '/images/pinar-nestle-sut.jpg',
    UpdatedAt = datetime('now')
WHERE Slug = 'pinar-sut-1l';

-- Sonuçları kontrol et
SELECT p.Name, p.ImageUrl, c.Name as CategoryName 
FROM Products p 
JOIN Categories c ON p.CategoryId = c.Id 
WHERE p.Slug IN ('bulgur-1-kg', 'pinar-sut-1l');
