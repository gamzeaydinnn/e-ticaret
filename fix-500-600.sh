#!/bin/bash
cd /home/huseyinadm/eticaret

docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
INSERT INTO MikroCategoryMappings (MikroAnagrupKod, MikroAltgrupKod, MikroMarkaKod, CategoryId, BrandId, Priority, MikroGrupAciklama, Notes, IsActive, CreatedAt)
VALUES
  ('500', NULL, NULL, 4, NULL, 1, 'Sut ve Yogurt Urunleri', 'Manuel mapping', 1, GETUTCDATE()),
  ('600', NULL, NULL, 9, NULL, 1, 'Ekmek ve Firinci Urunler', 'Manuel mapping', 1, GETUTCDATE());
UPDATE p SET p.CategoryId = m.CategoryId
FROM Products p
INNER JOIN MikroProductCache c ON c.StokKod = p.SKU
INNER JOIN MikroCategoryMappings m ON m.MikroAnagrupKod = c.AnagrupKod
WHERE m.IsActive = 1 AND m.MikroAnagrupKod IN ('500','600');
SELECT 'Guncellendi: ' + CAST(@@ROWCOUNT AS VARCHAR) AS Sonuc;
SELECT cat.Name AS Kategori, COUNT(p.Id) AS UrunSayisi
FROM Products p
LEFT JOIN Categories cat ON cat.Id = p.CategoryId
GROUP BY cat.Name
ORDER BY UrunSayisi DESC;"
