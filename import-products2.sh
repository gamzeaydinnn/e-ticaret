#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== URUN IMPORT ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; DECLARE @diger INT; SET @diger = (SELECT TOP 1 Id FROM Categories WHERE Slug='diger' OR Name='Diger'); IF @diger IS NULL SET @diger = (SELECT TOP 1 Id FROM Categories WHERE ParentId IS NULL); INSERT INTO Products (Name, Description, CategoryId, Slug, Price, StockQuantity, ImageUrl, SKU, Currency, UnitWeightGrams, IsActive, CreatedAt, UpdatedAt, IsWeightBased, MaxOrderWeight, MinOrderWeight, PricePerUnit, WeightTolerancePercent, WeightUnit) SELECT ISNULL(NULLIF(c.StokAd,''), c.StokKod), '', @diger, LOWER(REPLACE(REPLACE(REPLACE(REPLACE(c.StokKod,' ','-'),'.',''),'/',''),'\\','')), ISNULL(c.SatisFiyati,0), ISNULL(c.DepoMiktari,0), '', c.StokKod, 'TRY', 0, 1, GETUTCDATE(), GETUTCDATE(), 0, 0, 0, ISNULL(c.SatisFiyati,0), 0, 0 FROM MikroProductCache c WHERE c.Aktif = 1 AND NOT EXISTS (SELECT 1 FROM Products p WHERE p.SKU = c.StokKod); SELECT 'Eklenen urun: ' + CAST(@@ROWCOUNT AS VARCHAR) as Sonuc;"

echo "=== LocalProductId GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; UPDATE c SET c.LocalProductId = p.Id FROM MikroProductCache c INNER JOIN Products p ON p.SKU = c.StokKod WHERE c.LocalProductId IS NULL; SELECT 'LocalProductId guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) as Sonuc;"

echo "=== KATEGORİ MAPPING GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; UPDATE p SET p.CategoryId = ISNULL((SELECT TOP 1 cm.CategoryId FROM MikroCategoryMappings cm INNER JOIN MikroProductCache c ON c.StokKod = p.SKU WHERE cm.AnagrupKod = c.AnagrupKod AND cm.AltgrupKod = c.GrupKod), p.CategoryId) FROM Products p WHERE p.CategoryId = (SELECT TOP 1 Id FROM Categories WHERE Slug='diger' OR Name='Diger'); SELECT 'Kategori guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR) as Sonuc;"

echo "=== SONUC ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT c.Name as Kategori, COUNT(p.Id) as UrunSayisi FROM Categories c LEFT JOIN Products p ON p.CategoryId=c.Id WHERE c.ParentId IS NOT NULL OR c.Name NOT IN ('100','1000','12','1200','1300','1400','1500','1600','1900','2000','21','2200','400','500','600','700','800','900') GROUP BY c.Name HAVING COUNT(p.Id) > 0 ORDER BY COUNT(p.Id) DESC;"
