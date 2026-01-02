USE ECommerceDb;
GO

-- Kategorileri ekle
SET IDENTITY_INSERT Categories ON;

INSERT INTO Categories (Id, Name, Description, ImageUrl, Slug, SortOrder, IsActive, CreatedAt, UpdatedAt)
VALUES 
(1, N'Et ve Et Ürünleri', N'Taze et ve şarküteri ürünleri', '/images/dana-kusbasi.jpg', 'et-ve-et-urunleri', 1, 1, GETDATE(), GETDATE()),
(2, N'Süt ve Süt Ürünleri', N'Süt, peynir, yoğurt ve türevleri', '/images/ozel-fiyat-koy-sutu.png', 'sut-ve-sut-urunleri', 2, 1, GETDATE(), GETDATE()),
(3, N'Meyve ve Sebze', N'Taze meyve ve sebzeler', '/images/domates.webp', 'meyve-ve-sebze', 3, 1, GETDATE(), GETDATE()),
(4, N'İçecekler', N'Soğuk ve sıcak içecekler', '/images/coca-cola.jpg', 'icecekler', 4, 1, GETDATE(), GETDATE()),
(5, N'Atıştırmalık', N'Cipsi, kraker ve atıştırmalıklar', '/images/tahil-cipsi.jpg', 'atistirmalik', 5, 1, GETDATE(), GETDATE()),
(6, N'Temizlik', N'Ev temizlik ürünleri', '/images/yeşil-cif-krem.jpg', 'temizlik', 6, 1, GETDATE(), GETDATE());

SET IDENTITY_INSERT Categories OFF;
GO

-- Ürünleri ekle
SET IDENTITY_INSERT Products ON;

INSERT INTO Products (Id, Name, Description, CategoryId, Price, StockQuantity, ImageUrl, Slug, SKU, Currency, IsActive, CreatedAt, UpdatedAt)
VALUES 
(1, N'Dana Kuşbaşı', N'Taze dana eti kuşbaşı', 1, 89.90, 25, '/images/dana-kusbasi.jpg', 'dana-kusbasi', 'ET-001', 'TRY', 1, GETDATE(), GETDATE()),
(2, N'Kuzu İncik', N'Taze kuzu incik eti', 1, 95.50, 15, '/images/kuzu-incik.webp', 'kuzu-incik', 'ET-002', 'TRY', 1, GETDATE(), GETDATE()),
(3, N'Sucuk 250gr', N'Geleneksel sucuk', 1, 24.90, 30, '/images/sucuk.jpg', 'sucuk-250gr', 'ET-003', 'TRY', 1, GETDATE(), GETDATE()),
(4, N'Pınar Süt 1L', N'Taze tam yağlı süt', 2, 12.50, 50, '/images/ozel-fiyat-koy-sutu.png', 'pinar-sut-1l', 'SUT-001', 'TRY', 1, GETDATE(), GETDATE()),
(5, N'Şek Kaşar Peyniri 200gr', N'Eski kaşar peynir', 2, 35.90, 20, '/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg', 'sek-kasar-peyniri-200gr', 'SUT-002', 'TRY', 1, GETDATE(), GETDATE()),
(6, N'Domates Kg', N'Taze domates', 3, 8.75, 100, '/images/domates.webp', 'domates-kg', 'SEB-001', 'TRY', 1, GETDATE(), GETDATE()),
(7, N'Salatalık Kg', N'Taze salatalık', 3, 6.50, 80, '/images/salatalik.jpg', 'salatalik-kg', 'SEB-002', 'TRY', 1, GETDATE(), GETDATE()),
(8, N'Bulgur 1 Kg', N'Pilavlık bulgur', 3, 15.90, 40, '/images/bulgur.png', 'bulgur-1-kg', 'BAK-001', 'TRY', 1, GETDATE(), GETDATE()),
(9, N'Coca Cola 330ml', N'Coca Cola teneke kutu', 4, 5.50, 75, '/images/coca-cola.jpg', 'coca-cola-330ml', 'ICE-001', 'TRY', 1, GETDATE(), GETDATE()),
(10, N'Lipton Ice Tea 330ml', N'Şeftali aromalı ice tea', 4, 4.75, 60, '/images/lipton-ice-tea.jpg', 'lipton-ice-tea-330ml', 'ICE-002', 'TRY', 1, GETDATE(), GETDATE()),
(11, N'Nescafe 200gr', N'Klasik nescafe', 4, 45.90, 25, '/images/nescafe.jpg', 'nescafe-200gr', 'ICE-003', 'TRY', 1, GETDATE(), GETDATE()),
(12, N'Tahıl Cipsi 150gr', N'Çıtır tahıl cipsi', 5, 12.90, 35, '/images/tahil-cipsi.jpg', 'tahil-cipsi-150gr', 'ATI-001', 'TRY', 1, GETDATE(), GETDATE()),
(13, N'Cif Krem Temizleyici', N'Mutfak temizleyici', 6, 15.90, 5, '/images/yeşil-cif-krem.jpg', 'cif-krem-temizleyici', 'TEM-001', 'TRY', 0, GETDATE(), GETDATE());

SET IDENTITY_INSERT Products OFF;
GO

PRINT 'Seed data başarıyla eklendi!';
PRINT 'Toplam 6 kategori ve 13 ürün oluşturuldu.';
GO
