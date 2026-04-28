-- ============================================================================
-- WEB FİYAT LİSTESİ HAZIRLAMA (STOK_SATIS_FIYAT_LISTELERI)
-- ============================================================================
-- AMAÇ: Kaynak liste (1) fiyatlarını hedef listeye (11) kopyalar.
--        Web uygulaması SELECT sorgularında liste 11'den okur.
--
-- ÇALIŞTIRMA SIRASI:
--   1) DELETE  → Hedef liste (11) tamamen temizlenir
--   2) INSERT  → Web-aktif stoklar hedef listeye eklenir (fiyat=0 placeholder)
--   3) UPDATE  → Fiyatlar kaynak listeden (1) Depo 1 MAX ile güncellenir
--
-- ÖNEMLİ: Bu script Mikro ERP veritabanına karşı çalıştırılır.
--          Transaction ile atomik işlem yapılır — hata olursa ROLLBACK.
-- ============================================================================

SET NOCOUNT ON;

-- Parametre atamaları
DECLARE @HedefListeNo  INT = 11;  -- Web Listesi (hedef — sıfırdan doldurulur)
DECLARE @KaynakListeNo INT = 1;   -- Mikro Fiyat Listesi (kaynak — orijinal fiyatlar)
DECLARE @HedefDepoNo   INT = 0;   -- İşlem Yapılacak Depo No

BEGIN TRY
    BEGIN TRANSACTION;

    -- ─────────────────────────────────────────────────────────────────────
    -- ADIM 1: Hedef listeyi tamamen temizle — sıfırdan doldurulacak
    -- ─────────────────────────────────────────────────────────────────────
    DELETE FROM STOK_SATIS_FIYAT_LISTELERI
    WHERE sfiyat_listesirano = @HedefListeNo;

    PRINT CONCAT('ADIM 1 TAMAM — Silinen kayıt: ', @@ROWCOUNT);

    -- ─────────────────────────────────────────────────────────────────────
    -- ADIM 2: Web-aktif stokları hedef listeye ekle
    -- DELETE sonrası liste boş olduğu için tüm web-aktif stoklar eklenir
    -- ─────────────────────────────────────────────────────────────────────
    INSERT INTO STOK_SATIS_FIYAT_LISTELERI (
        sfiyat_DBCno, sfiyat_SpecRECno, sfiyat_iptal, sfiyat_fileid,
        sfiyat_hidden, sfiyat_kilitli, sfiyat_degisti, sfiyat_checksum,
        sfiyat_create_user, sfiyat_create_date,
        sfiyat_lastup_user, sfiyat_lastup_date,
        sfiyat_special1, sfiyat_special2, sfiyat_special3,
        sfiyat_stokkod, sfiyat_listesirano, sfiyat_deposirano,
        sfiyat_odemeplan, sfiyat_birim_pntr, sfiyat_fiyati,
        sfiyat_doviz, sfiyat_iskontokod, sfiyat_deg_nedeni,
        sfiyat_primyuzdesi, sfiyat_kampanyakod, sfiyat_doviz_kuru
    )
    SELECT
        0,               -- sfiyat_DBCno
        0,               -- sfiyat_SpecRECno
        0,               -- sfiyat_iptal
        228,             -- sfiyat_fileid
        0,               -- sfiyat_hidden
        0,               -- sfiyat_kilitli
        0,               -- sfiyat_degisti
        0,               -- sfiyat_checksum
        1,               -- sfiyat_create_user
        GETDATE(),       -- sfiyat_create_date
        1,               -- sfiyat_lastup_user
        GETDATE(),       -- sfiyat_lastup_date
        '',              -- sfiyat_special1
        '',              -- sfiyat_special2
        '',              -- sfiyat_special3
        s.sto_kod,       -- sfiyat_stokkod
        @HedefListeNo,   -- sfiyat_listesirano
        @HedefDepoNo,    -- sfiyat_deposirano
        0,               -- sfiyat_odemeplan
        1,               -- sfiyat_birim_pntr
        0.0,             -- sfiyat_fiyati (başlangıç — UPDATE ile güncellenecek)
        0,               -- sfiyat_doviz
        '',              -- sfiyat_iskontokod
        '',              -- sfiyat_deg_nedeni
        0,               -- sfiyat_primyuzdesi
        '',              -- sfiyat_kampanyakod
        0                -- sfiyat_doviz_kuru
    FROM STOKLAR s
    WHERE s.sto_webe_gonderilecek_fl = 1;

    PRINT CONCAT('ADIM 2 TAMAM — Eklenen kayıt: ', @@ROWCOUNT);

    -- ─────────────────────────────────────────────────────────────────────
    -- ADIM 3: Hedef listedeki fiyatları KAYNAK listeden (1) MAX ile güncelle
    -- Bir stok kodunun birden fazla satırı olabilir → MAX en yüksek fiyatı alır
    -- Örnek: M000058 liste 1'de 149.9, 149.9, 79.5 → MAX = 149.9
    -- ─────────────────────────────────────────────────────────────────────
    UPDATE f_hedef
    SET
        f_hedef.sfiyat_fiyati      = ISNULL(f_kaynak.MaxFiyat, 0),
        f_hedef.sfiyat_lastup_date = GETDATE()
    FROM STOK_SATIS_FIYAT_LISTELERI f_hedef
    INNER JOIN (
        SELECT sfiyat_stokkod, MAX(sfiyat_fiyati) AS MaxFiyat
        FROM STOK_SATIS_FIYAT_LISTELERI
        WHERE sfiyat_listesirano = @KaynakListeNo
          AND sfiyat_deposirano  = 1   -- Yalnızca Depo 1 (ana perakende deposu)
        GROUP BY sfiyat_stokkod
    ) f_kaynak ON f_hedef.sfiyat_stokkod = f_kaynak.sfiyat_stokkod
    INNER JOIN STOKLAR s ON s.sto_kod = f_hedef.sfiyat_stokkod
    WHERE f_hedef.sfiyat_listesirano = @HedefListeNo
      AND f_hedef.sfiyat_deposirano  = @HedefDepoNo
      AND s.sto_webe_gonderilecek_fl = 1;

    PRINT CONCAT('ADIM 3 TAMAM — Güncellenen kayıt: ', @@ROWCOUNT);

    COMMIT TRANSACTION;
    PRINT '========== İŞLEM BAŞARILI — COMMIT YAPILDI ==========';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '========== HATA OLUŞTU — ROLLBACK YAPILDI ==========';
    PRINT CONCAT('Hata No  : ', ERROR_NUMBER());
    PRINT CONCAT('Hata Mesaj: ', ERROR_MESSAGE());
    PRINT CONCAT('Satır    : ', ERROR_LINE());
END CATCH;
