#!/bin/bash
cd /home/huseyinadm/eticaret

echo "=== ADMIN KULLANICILARI ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT TOP 3 Email, Role FROM Users WHERE Role IN ('Admin','SuperAdmin') ORDER BY Id;"

echo "=== CACHE -> PRODUCTS IMPORT (dogrudan SQL) ==="
# Cache'deki tum urunleri Products tablosuna ekle (yoksa), LocalProductId guncelle
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; DECLARE @diger INT; SET @diger = (SELECT TOP 1 Id FROM Categories WHERE Slug='diger' OR Name='Diger'); IF @diger IS NULL SET @diger = (SELECT TOP 1 Id FROM Categories WHERE ParentId IS NULL); INSERT INTO Products (Name, SKU, Price, StockQuantity, CategoryId, IsActive, CreatedAt, UpdatedAt, Description, Slug) SELECT c.StokAd, c.StokKod, ISNULL(c.SatisFiyati, 0), ISNULL(c.DepoMiktari, 0), @diger, 1, GETUTCDATE(), GETUTCDATE(), ISNULL(c.StokAdi2, ''), LOWER(REPLACE(REPLACE(c.StokKod, ' ', '-'), '.', '')) FROM MikroProductCache c WHERE c.Aktif = 1 AND NOT EXISTS (SELECT 1 FROM Products p WHERE p.SKU = c.StokKod); SELECT 'Eklenen urun: ' + CAST(@@ROWCOUNT AS VARCHAR);"

echo "=== LocalProductId GUNCELLE ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SET QUOTED_IDENTIFIER ON; UPDATE c SET c.LocalProductId = p.Id FROM MikroProductCache c INNER JOIN Products p ON p.SKU = c.StokKod WHERE c.LocalProductId IS NULL; SELECT 'LocalProductId guncellenen: ' + CAST(@@ROWCOUNT AS VARCHAR);"

echo "=== SONUC ==="
docker exec ecommerce-sql-prod /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'ECom1234' -C -d ECommerceDb \
  -Q "SELECT 'Toplam Products' as Tip, CAST(COUNT(*) AS VARCHAR) as Sayi FROM Products UNION ALL SELECT 'Cache dolu LocalProductId', CAST(COUNT(*) AS VARCHAR) FROM MikroProductCache WHERE LocalProductId IS NOT NULL;"
