#!/bin/bash
# SUNUCU BANNER FIX - Database banner URL'lerini container'daki dosyalarla eşleştir

echo "🔧 Banner URL'lerini güncelliyorum..."

# SQL komutlarını çalıştır
docker exec -i ecommerce-sql-prod /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -d ECommerceDb <<'EOSQL'

-- Mevcut banner'ları göster
PRINT '📋 Mevcut Banner URL''leri:';
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
GO

-- Banner #1 (Slider) güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112111459_a4b5151d.png'
WHERE Id = 1;
GO

-- Banner #2 (Promo) güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_gemini_generated_image_r09nenr09nenr09n_20260112103231_01dc07c7.png'
WHERE Id = 2;
GO

-- Banner #3 (Promo) güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110809_a0760dfa.png'
WHERE Id = 3;
GO

-- Banner #4 (Promo) güncelle
UPDATE Banners 
SET ImageUrl = '/uploads/banners/banner_taze-dogal-indirim-banner_20260112110826_8c6b7b96.png'
WHERE Id = 4;
GO

-- Güncellenmiş banner'ları göster
PRINT '✅ Güncellenmiş Banner URL''leri:';
SELECT Id, Title, ImageUrl FROM Banners ORDER BY DisplayOrder;
GO

EOSQL

echo ""
echo "✅ Banner URL'leri güncellendi!"
echo ""
echo "🧪 Test et:"
echo "   Tarayıcıda: https://golkoygurme.com.tr/"
echo "   Hard refresh: Ctrl+Shift+R (cache temizle)"
echo ""
