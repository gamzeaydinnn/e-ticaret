namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// Birleşik SQL sorgusu ile Mikro ERP'den çekilen ürün verisi DTO'su.
    /// 
    /// NEDEN: StokListesiV2 + ayrı fiyat/stok SQL sorguları yerine
    /// TEK birleşik SQL ile tüm veri (fiyat + stok + ürün bilgisi + barkod) çekilir.
    /// Bu yaklaşım Mikro'ya yapılan istek sayısını %80+ azaltır ve timeout sorununu kökten çözer.
    /// 
    /// SQL KAYNAKLARI:
    /// - STOK_SATIS_FIYAT_LISTELERI_YONETIM (fiyat yönetim tablosu)
    /// - fn_Stok_Depo_Dagilim (depo bazlı stok dağılımı)
    /// - STOKLAR (ana stok kartı)
    /// - STOK_SATIS_FIYAT_LISTELERI (fiyat listeleri)
    /// - BARKOD_TANIMLARI (barkod tablosu)
    /// - STOK_HAREKETLERI (hareket geçmişi — yıl içi aktif ürün filtresi)
    /// 
    /// FİLTRE: Sadece sto_webe_gonderilecek_fl = 1 olan ürünler gelir (~500-1200 ürün).
    /// </summary>
    public class MikroUnifiedProductDto
    {
        /// <summary>
        /// Stok kodu (SKU) — Birincil tanımlayıcı.
        /// SQL alias: stokkod
        /// </summary>
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı.
        /// SQL alias: stokad (STOKLAR.sto_isim)
        /// </summary>
        public string StokAd { get; set; } = string.Empty;

        /// <summary>
        /// Satış fiyatı (TL).
        /// SQL alias: fiyat (STOK_SATIS_FIYAT_LISTELERI.sfiyat_fiyati)
        /// </summary>
        public decimal Fiyat { get; set; }

        /// <summary>
        /// Depo bazlı stok miktarı.
        /// SQL alias: stok_miktar (fn_Stok_Depo_Dagilim.msg_S_0343)
        /// </summary>
        public decimal StokMiktar { get; set; }

        /// <summary>
        /// Depo numarası.
        /// SQL alias: depo_no (fn_Stok_Depo_Dagilim.msg_S_0873)
        /// </summary>
        public int? DepoNo { get; set; }

        /// <summary>
        /// Ürün barkodu.
        /// SQL alias: barkod (BARKOD_TANIMLARI.bar_kodu)
        /// </summary>
        public string Barkod { get; set; } = string.Empty;

        /// <summary>
        /// Alt grup kodu (sub-category).
        /// SQL alias: grup_kod (STOKLAR.sto_altgrup_kod)
        /// </summary>
        public string GrupKod { get; set; } = string.Empty;

        /// <summary>
        /// Ana grup kodu (main category).
        /// SQL alias: anagrup_kod (STOKLAR.sto_anagrup_kod)
        /// </summary>
        public string AnagrupKod { get; set; } = string.Empty;

        /// <summary>
        /// Ölçü birimi (ADET, KG, LT vb.).
        /// SQL alias: birim (STOKLAR.sto_birim1_ad)
        /// </summary>
        public string Birim { get; set; } = string.Empty;

        /// <summary>
        /// KDV oranı (%).
        /// SQL alias: kdv_orani (STOKLAR.sto_perakende_vergi)
        /// </summary>
        public decimal KdvOrani { get; set; }

        /// <summary>
        /// Web'e gönderilecek flag (her zaman true — SQL WHERE filtresi bunu garantiler).
        /// SQL alias: webe_gonderilecek_fl
        /// </summary>
        public bool WebeGonderilecekFl { get; set; } = true;

        /// <summary>
        /// Yıl içindeki en son stok hareketi tarihi — sıralama ve delta sync için.
        /// SQL alias: son_hareket_tarihi (MAX(STOK_HAREKETLERI.sth_tarih))
        /// </summary>
        public DateTime? SonHareketTarihi { get; set; }
    }
}
