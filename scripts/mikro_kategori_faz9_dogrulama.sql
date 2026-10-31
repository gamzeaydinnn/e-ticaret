/* ============================================================================
   FAZ 9 — MİKRO ERP KATEGORİ ENTEGRASYONU DOĞRULAMA SORGULARI (SALT OKUNUR)
   Bu betik yalnızca SELECT sorguları içerir; hiçbir tabloya yazma yapmaz.
   Uygulama çalıştırıldıktan sonra yerel E-Ticaret veritabanında çalıştırarak
   Faz 9.1 - 9.4 maddelerini doğrulayabilirsiniz.
============================================================================ */

-- 9.1A: Ana Kategoriler (Beklenen: 19 Yeni Düzen Ana Kategori 100..2200)
SELECT 
    Id,
    MikroAnaGrupKod,
    Name AS SiteAdi,
    MikroIsim,
    Slug,
    NavbarGroupKey,
    IsActive AS SitedeAktifMi,
    IsMikroSynced,
    SortOrder
FROM Categories
WHERE ParentId IS NULL
  AND MikroAnaGrupKod IS NOT NULL
ORDER BY TRY_CAST(MikroAnaGrupKod AS INT), Id;

-- 9.1B: Alt Kategoriler Özeti (Beklenen: 19 Ana Grup altında toplam 105 Alt Kategori)
SELECT 
    p.MikroAnaGrupKod AS AnaKod,
    p.Name AS AnaKategoriAdi,
    COUNT(c.Id) AS AltKategoriSayisi,
    SUM(CASE WHEN c.IsActive = 1 THEN 1 ELSE 0 END) AS AktifAltKategoriSayisi
FROM Categories p
LEFT JOIN Categories c ON c.ParentId = p.Id
WHERE p.ParentId IS NULL
  AND p.MikroAnaGrupKod IS NOT NULL
GROUP BY p.MikroAnaGrupKod, p.Name
ORDER BY TRY_CAST(p.MikroAnaGrupKod AS INT);

-- 9.2: Ana Kategori Bazında Yerel Ürün Dağılımı (Ana + Alt Kategorilerdeki Aktif Ürün Toplamı)
SELECT 
    p.MikroAnaGrupKod AS AnaKod,
    p.Name AS AnaKategori,
    p.NavbarGroupKey AS BirlesikNavbarGrubu,
    COUNT(prd.Id) AS ToplamAktifUrunSayisi
FROM Categories p
LEFT JOIN Categories sub ON sub.ParentId = p.Id
LEFT JOIN Products prd ON prd.IsActive = 1 AND (prd.CategoryId = p.Id OR prd.CategoryId = sub.Id)
WHERE p.ParentId IS NULL
  AND p.MikroAnaGrupKod IS NOT NULL
GROUP BY p.MikroAnaGrupKod, p.Name, p.NavbarGroupKey
ORDER BY TRY_CAST(p.MikroAnaGrupKod AS INT);

-- 9.3: Birleşik Navbar Grupları ve Altındaki Alt Kategoriler (Atıştırmalık, Dondurulmuş Gıda, Temizlik, Ev & Mutfak)
SELECT 
    p.NavbarGroupKey,
    p.MikroAnaGrupKod,
    c.MikroAltGrupKod,
    c.Name AS AltKategoriAdi,
    c.Slug AS AltKategoriSlug,
    c.IsActive
FROM Categories p
INNER JOIN Categories c ON c.ParentId = p.Id
WHERE p.NavbarGroupKey IN ('atistirmalik', 'dondurma-ve-dondurulmus-gida', 'temizlik', 'ev-ve-mutfak')
ORDER BY p.NavbarGroupKey, TRY_CAST(p.MikroAnaGrupKod AS INT), TRY_CAST(c.MikroAltGrupKod AS INT);

-- 9.4: MikroCategoryMappings Tablosu Doğrulaması (Öncelik 100 = Alt Grup, 50 = Ana Grup)
SELECT 
    Priority,
    COUNT(*) AS EslemeSayisi
FROM MikroCategoryMappings
WHERE IsActive = 1
GROUP BY Priority
ORDER BY Priority DESC;

