-- ============================================================================
-- KARGO FİYATLARI GÜNCELLEME
-- Mevcut veritabanındaki kargo fiyatlarını 40/60 TL olarak günceller
-- ============================================================================

-- Motosiklet fiyatını 40 TL olarak güncelle
UPDATE ShippingSettings 
SET Price = 40.00, 
    UpdatedAt = GETUTCDATE()
WHERE VehicleType = 'motorcycle';

-- Araç fiyatını 60 TL olarak güncelle
UPDATE ShippingSettings 
SET Price = 60.00, 
    UpdatedAt = GETUTCDATE()
WHERE VehicleType = 'car';

-- Sonucu kontrol et
SELECT Id, VehicleType, DisplayName, Price, IsActive FROM ShippingSettings;
