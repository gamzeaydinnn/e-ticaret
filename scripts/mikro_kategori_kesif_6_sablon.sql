-- Mikro grup tabloları: şablon alanların tutarlılık kontrolü (SADECE OKUMA)

-- 1) ANA grup: sistem alanlarının tüm satırlardaki kombinasyonları
SELECT san_DBCno, san_SpecRECno, san_iptal, san_fileid, san_hidden, san_kilitli, san_degisti, san_checksum,
       san_create_user, san_lastup_user,
       CASE WHEN san_special1 IS NULL THEN 'NULL' WHEN san_special1 = '' THEN 'BOS' ELSE 'DOLU' END AS sp1,
       CASE WHEN san_special2 IS NULL THEN 'NULL' WHEN san_special2 = '' THEN 'BOS' ELSE 'DOLU' END AS sp2,
       CASE WHEN san_special3 IS NULL THEN 'NULL' WHEN san_special3 = '' THEN 'BOS' ELSE 'DOLU' END AS sp3,
       COUNT(*) AS adet
FROM STOK_ANA_GRUPLARI
GROUP BY san_DBCno, san_SpecRECno, san_iptal, san_fileid, san_hidden, san_kilitli, san_degisti, san_checksum,
         san_create_user, san_lastup_user,
         CASE WHEN san_special1 IS NULL THEN 'NULL' WHEN san_special1 = '' THEN 'BOS' ELSE 'DOLU' END,
         CASE WHEN san_special2 IS NULL THEN 'NULL' WHEN san_special2 = '' THEN 'BOS' ELSE 'DOLU' END,
         CASE WHEN san_special3 IS NULL THEN 'NULL' WHEN san_special3 = '' THEN 'BOS' ELSE 'DOLU' END
ORDER BY adet DESC;
GO

-- 2) ALT grup: aynı kontrol
SELECT sta_DBCno, sta_SpecRECno, sta_iptal, sta_fileid, sta_hidden, sta_kilitli, sta_degisti, sta_checksum,
       sta_create_user, sta_lastup_user,
       CASE WHEN sta_special1 IS NULL THEN 'NULL' WHEN sta_special1 = '' THEN 'BOS' ELSE 'DOLU' END AS sp1,
       CASE WHEN sta_special2 IS NULL THEN 'NULL' WHEN sta_special2 = '' THEN 'BOS' ELSE 'DOLU' END AS sp2,
       CASE WHEN sta_special3 IS NULL THEN 'NULL' WHEN sta_special3 = '' THEN 'BOS' ELSE 'DOLU' END AS sp3,
       COUNT(*) AS adet
FROM STOK_ALT_GRUPLARI
GROUP BY sta_DBCno, sta_SpecRECno, sta_iptal, sta_fileid, sta_hidden, sta_kilitli, sta_degisti, sta_checksum,
         sta_create_user, sta_lastup_user,
         CASE WHEN sta_special1 IS NULL THEN 'NULL' WHEN sta_special1 = '' THEN 'BOS' ELSE 'DOLU' END,
         CASE WHEN sta_special2 IS NULL THEN 'NULL' WHEN sta_special2 = '' THEN 'BOS' ELSE 'DOLU' END,
         CASE WHEN sta_special3 IS NULL THEN 'NULL' WHEN sta_special3 = '' THEN 'BOS' ELSE 'DOLU' END
ORDER BY adet DESC;
GO

-- 3) Kod tekrarı var mı? (tabloda kod için benzersizlik kısıtı yok)
SELECT san_kod, COUNT(*) AS adet FROM STOK_ANA_GRUPLARI GROUP BY san_kod HAVING COUNT(*) > 1;
SELECT sta_kod, sta_ana_grup_kod, COUNT(*) AS adet FROM STOK_ALT_GRUPLARI GROUP BY sta_kod, sta_ana_grup_kod HAVING COUNT(*) > 1;
GO

-- 4) Mikro kullanıcıları (create_user numaraları kimlere ait?)
SELECT TOP 20 * FROM MIKRO_KULLANICILARI;  -- tablo adı farklıysa hata verir, atlayın
GO

-- 5) Bu veritabanına bağlanan uygulama kullanıcısının yetkileri (yazma yetkisi var mı?)
SELECT dp.name AS kullanici, p.permission_name, p.state_desc, OBJECT_NAME(p.major_id) AS nesne
FROM sys.database_permissions p
JOIN sys.database_principals dp ON dp.principal_id = p.grantee_principal_id
WHERE OBJECT_NAME(p.major_id) IN ('STOK_ANA_GRUPLARI', 'STOK_ALT_GRUPLARI');
GO

