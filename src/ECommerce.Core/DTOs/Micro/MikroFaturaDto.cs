using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 FaturaKaydetV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: E-ticaret siparişleri için Mikro'da fatura kesilmesi gerekiyor.
    /// Bu DTO, siparişten faturaya dönüşüm için kullanılır.
    /// 
    /// AKIŞ:
    /// 1. Sipariş onaylanır
    /// 2. CariKaydetV2 ile müşteri kontrol/oluştur
    /// 3. SiparisKaydetV2 ile sipariş kaydı (opsiyonel)
    /// 4. FaturaKaydetV2 ile fatura kesilir → Stok düşer, cari borç oluşur
    /// 
    /// ÖNEMLİ: FaturaKaydetV2 çağrıldığında stok otomatik düşer!
    /// </summary>
    public class MikroFaturaKaydetRequestDto
    {
        /// <summary>
        /// Evrak listesi.
        /// NEDEN: Toplu fatura kesimi için birden fazla evrak gönderilebilir.
        /// E-ticaret'te genelde tek sipariş = tek fatura.
        /// </summary>
        [JsonPropertyName("evraklar")]
        public List<MikroFaturaEvrakDto> Evraklar { get; set; } = new();
    }

    /// <summary>
    /// Fatura evrak bilgileri (header).
    /// </summary>
    public class MikroFaturaEvrakDto
    {
        // ==================== EVRAK BİLGİLERİ ====================

        /// <summary>
        /// Evrak seri numarası.
        /// NEDEN: Her satış kanalı için farklı seri kullanılabilir.
        /// Örnek: "E" = E-ticaret, "M" = Mağaza
        /// </summary>
        [JsonPropertyName("cha_evrakno_seri")]
        public string ChaEvraknoSeri { get; set; } = "E";

        /// <summary>
        /// Evrak tarihi (dd.MM.yyyy formatı).
        /// </summary>
        [JsonPropertyName("cha_tarihi")]
        public string ChaTarihi { get; set; } = DateTime.Now.ToString("dd.MM.yyyy");

        /// <summary>
        /// Cari (müşteri) kodu.
        /// </summary>
        [JsonPropertyName("cha_kod")]
        public string ChaKod { get; set; } = string.Empty;

        /// <summary>
        /// Evrak tipi.
        /// 0 = Satış, 1 = Alış
        /// </summary>
        [JsonPropertyName("cha_tip")]
        public int ChaTip { get; set; } = 0; // Satış

        /// <summary>
        /// Evrak cinsi.
        /// 7 = Satış Faturası, 8 = Perakende Satış Faturası
        /// </summary>
        [JsonPropertyName("cha_cinsi")]
        public int ChaCinsi { get; set; } = 8; // Perakende Satış Faturası

        /// <summary>
        /// Normal / İade.
        /// 0 = Normal, 1 = İade
        /// </summary>
        [JsonPropertyName("cha_normal_Iade")]
        public int ChaNormalIade { get; set; } = 0; // Normal

        /// <summary>
        /// Cari cinsi.
        /// 0 = Normal Cari
        /// </summary>
        [JsonPropertyName("cha_cari_cins")]
        public int ChaCariCins { get; set; } = 0;

        /// <summary>
        /// Döviz cinsi.
        /// 0 = TL, 1 = USD, 2 = EUR
        /// </summary>
        [JsonPropertyName("cha_d_cins")]
        public int ChaDCins { get; set; } = 0; // TL

        /// <summary>
        /// Döviz kuru.
        /// </summary>
        [JsonPropertyName("cha_d_kur")]
        public decimal ChaDKur { get; set; } = 1;

        /// <summary>
        /// Şube numarası.
        /// </summary>
        [JsonPropertyName("cha_subeno")]
        public int ChaSubeno { get; set; } = 0;

        /// <summary>
        /// Ara toplam (KDV hariç).
        /// </summary>
        [JsonPropertyName("cha_aratoplam")]
        public decimal ChaAratoplam { get; set; }

        /// <summary>
        /// Evrak açıklaması.
        /// NEDEN: E-ticaret sipariş numarası referans olarak yazılır.
        /// </summary>
        [JsonPropertyName("cha_aciklama")]
        public string ChaAciklama { get; set; } = string.Empty;

        /// <summary>
        /// Vade gün sayısı.
        /// 0 = Peşin
        /// </summary>
        [JsonPropertyName("cha_vade")]
        public int ChaVade { get; set; } = 0; // Peşin (e-ticaret için)

        /// <summary>
        /// Fatura iskontosu (%).
        /// </summary>
        [JsonPropertyName("cha_ft_iskonto1")]
        public decimal ChaFtIskonto1 { get; set; } = 0;

        /// <summary>
        /// İskonto/Masraf tipi.
        /// </summary>
        [JsonPropertyName("cha_isk_mas1")]
        public string ChaIskMas1 { get; set; } = "0";

        /// <summary>
        /// Evrak tip kodu.
        /// 63 = Standart Satış Faturası
        /// </summary>
        [JsonPropertyName("cha_evrak_tip")]
        public int ChaEvrakTip { get; set; } = 63;

        /// <summary>
        /// Vergi pointer.
        /// </summary>
        [JsonPropertyName("cha_vergipntr")]
        public decimal ChaVergipntr { get; set; } = 0;

        /// <summary>
        /// Proje kodu.
        /// </summary>
        [JsonPropertyName("cha_projekodu")]
        public string ChaProjekodu { get; set; } = string.Empty;

        /// <summary>
        /// Satıcı kodu.
        /// </summary>
        [JsonPropertyName("cha_satici_kodu")]
        public string ChaSaticiKodu { get; set; } = string.Empty;

        /// <summary>
        /// Sorumluluk merkezi kodu.
        /// </summary>
        [JsonPropertyName("cha_srmrkkodu")]
        public string ChaSrmrkkodu { get; set; } = string.Empty;

        /// <summary>
        /// Miktar (evrak bazında toplam).
        /// </summary>
        [JsonPropertyName("cha_miktari")]
        public string ChaMiktari { get; set; } = "1";

        // ==================== E-ARŞİV BİLGİLERİ ====================
        // E-fatura/e-arşiv zorunlulukları için

        /// <summary>
        /// E-Arşiv - Müşteri VKN/TCKN.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_Vkn")]
        public string ChaEArsivVkn { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Müşteri adı.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_unvani_ad")]
        public string ChaEArsivUnvaniAd { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Müşteri soyadı.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_unvani_soyad")]
        public string ChaEArsivUnvaniSoyad { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - E-posta adresi.
        /// NEDEN: E-arşiv fatura müşteriye otomatik gönderilir.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_mail")]
        public string ChaEArsivMail { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Telefon (ülke kodu).
        /// </summary>
        [JsonPropertyName("cha_EArsiv_tel_ulke_kod")]
        public string ChaEArsivTelUlkeKod { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Telefon (alan kodu).
        /// </summary>
        [JsonPropertyName("cha_EArsiv_tel_bolge_kod")]
        public string ChaEArsivTelBolgeKod { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Telefon numarası.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_tel_no")]
        public string ChaEArsivTelNo { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - İl.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_Il")]
        public string ChaEArsivIl { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Ülke.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_ulke")]
        public string ChaEArsivUlke { get; set; } = string.Empty;

        /// <summary>
        /// E-Arşiv - Vergi dairesi.
        /// </summary>
        [JsonPropertyName("cha_EArsiv_daire_adi")]
        public string ChaEArsivDaireAdi { get; set; } = string.Empty;

        /// <summary>
        /// KDV istisna kodu.
        /// </summary>
        [JsonPropertyName("kdv_istisna_kodu")]
        public string KdvIstinaKodu { get; set; } = string.Empty;

        /// <summary>
        /// Kasa hizmet kodu.
        /// </summary>
        [JsonPropertyName("cha_kasa_hizkod")]
        public string ChaKasaHizkod { get; set; } = string.Empty;

        /// <summary>
        /// Kasa hizmet.
        /// </summary>
        [JsonPropertyName("cha_kasa_hizmet")]
        public int ChaKasaHizmet { get; set; } = 0;

        // ==================== FATURA DETAY (SATIRLAR) ====================

        /// <summary>
        /// Fatura satırları (stok hareketleri).
        /// NEDEN: Her sipariş kalemi bir satır olarak girer.
        /// </summary>
        [JsonPropertyName("detay")]
        public List<MikroFaturaSatirDto> Detay { get; set; } = new();

        /// <summary>
        /// E-Belge detayları (ödeme bilgisi vb.).
        /// </summary>
        [JsonPropertyName("ebelge_detay")]
        public List<MikroFaturaEbelgeDetayDto>? EbelgeDetay { get; set; }

        /// <summary>
        /// Ödeme bilgileri.
        /// </summary>
        [JsonPropertyName("odemeler")]
        public List<MikroFaturaOdemeDto>? Odemeler { get; set; }

        /// <summary>
        /// Kullanıcı tanımlı alanlar.
        /// NEDEN: E-ticaret referansları (order id, payment ref vb.) için.
        /// </summary>
        [JsonPropertyName("user_tablo")]
        public List<MikroFaturaUserTabloDto>? UserTablo { get; set; }
    }

    /// <summary>
    /// Fatura satır (kalem) bilgileri.
    /// </summary>
    public class MikroFaturaSatirDto
    {
        /// <summary>
        /// Stok kodu.
        /// </summary>
        [JsonPropertyName("sth_stok_kod")]
        public string SthStokKod { get; set; } = string.Empty;

        /// <summary>
        /// Miktar.
        /// </summary>
        [JsonPropertyName("sth_miktar")]
        public decimal SthMiktar { get; set; }

        /// <summary>
        /// Tutar (KDV hariç).
        /// </summary>
        [JsonPropertyName("sth_tutar")]
        public decimal SthTutar { get; set; }

        /// <summary>
        /// KDV tutarı.
        /// </summary>
        [JsonPropertyName("sth_vergi")]
        public decimal SthVergi { get; set; }

        /// <summary>
        /// Evrak seri.
        /// </summary>
        [JsonPropertyName("sth_evrakno_seri")]
        public string SthEvraknoSeri { get; set; } = "E";

        /// <summary>
        /// Evrak tipi.
        /// 4 = Satış Faturası
        /// </summary>
        [JsonPropertyName("sth_evraktip")]
        public int SthEvraktip { get; set; } = 4; // Satış Faturası

        /// <summary>
        /// Hareket tipi.
        /// 0 = Alış, 1 = Satış
        /// </summary>
        [JsonPropertyName("sth_tip")]
        public int SthTip { get; set; } = 1; // Satış

        /// <summary>
        /// Hareket cinsi.
        /// 0 = Normal
        /// </summary>
        [JsonPropertyName("sth_cins")]
        public int SthCins { get; set; } = 0;

        /// <summary>
        /// Normal / İade.
        /// 0 = Normal
        /// </summary>
        [JsonPropertyName("sth_normal_iade")]
        public int SthNormalIade { get; set; } = 0;

        /// <summary>
        /// Çıkış depo numarası.
        /// NEDEN: Satışta stok bu depodan düşer.
        /// </summary>
        [JsonPropertyName("sth_cikis_depo_no")]
        public int SthCikisDepoNo { get; set; } = 1;

        /// <summary>
        /// Giriş depo numarası.
        /// NEDEN: İade/transfer durumunda kullanılır.
        /// </summary>
        [JsonPropertyName("sth_giris_depo_no")]
        public int SthGirisDepoNo { get; set; } = 1;

        /// <summary>
        /// Birim pointer.
        /// 1 = Ana birim
        /// </summary>
        [JsonPropertyName("sth_birim_pntr")]
        public int SthBirimPntr { get; set; } = 1;

        /// <summary>
        /// Cari cinsi.
        /// </summary>
        [JsonPropertyName("sth_cari_cinsi")]
        public int SthCariCinsi { get; set; } = 0;

        /// <summary>
        /// Cari kodu.
        /// </summary>
        [JsonPropertyName("sth_cari_kodu")]
        public string SthCariKodu { get; set; } = string.Empty;

        /// <summary>
        /// Tarih (dd.MM.yyyy).
        /// </summary>
        [JsonPropertyName("sth_tarih")]
        public string SthTarih { get; set; } = DateTime.Now.ToString("dd.MM.yyyy");

        /// <summary>
        /// Şube numarası.
        /// </summary>
        [JsonPropertyName("sth_subeno")]
        public int SthSubeno { get; set; } = 0;

        /// <summary>
        /// Açıklama.
        /// </summary>
        [JsonPropertyName("sth_aciklama")]
        public string SthAciklama { get; set; } = string.Empty;

        /// <summary>
        /// Cari sorumluluk merkezi.
        /// </summary>
        [JsonPropertyName("sth_cari_srm_merkezi")]
        public string SthCariSrmMerkezi { get; set; } = string.Empty;

        /// <summary>
        /// Stok sorumluluk merkezi.
        /// </summary>
        [JsonPropertyName("sth_stok_srm_merkezi")]
        public string SthStokSrmMerkezi { get; set; } = string.Empty;

        /// <summary>
        /// Kullanıcı tanımlı alanlar (satır bazında).
        /// </summary>
        [JsonPropertyName("user_tablo")]
        public List<MikroFaturaUserTabloDto>? UserTablo { get; set; }
    }

    /// <summary>
    /// E-Belge detay (ödeme şekli vb.).
    /// </summary>
    public class MikroFaturaEbelgeDetayDto
    {
        /// <summary>
        /// Ödeme şekli.
        /// 1 = Kredi Kartı, 2 = Nakit, 3 = Havale/EFT
        /// </summary>
        [JsonPropertyName("ebh_odeme_sekli")]
        public int EbhOdemeSekli { get; set; } = 1;

        /// <summary>
        /// Satışın yapıldığı web adresi.
        /// NEDEN: E-arşiv faturada gösterilir.
        /// </summary>
        [JsonPropertyName("ebh_satisin_webadresi")]
        public string EbhSatisinWebadresi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Fatura ödeme bilgisi.
    /// </summary>
    public class MikroFaturaOdemeDto
    {
        /// <summary>
        /// Ödeme tipi.
        /// </summary>
        [JsonPropertyName("odeme_tipi")]
        public string OdemeTipi { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme tutarı.
        /// </summary>
        [JsonPropertyName("odeme_tutari")]
        public decimal OdemeTutari { get; set; }

        /// <summary>
        /// Ödeme tarihi.
        /// </summary>
        [JsonPropertyName("odeme_tarihi")]
        public string OdemeTarihi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kullanıcı tanımlı alanlar.
    /// NEDEN: E-ticaret referansları (sipariş no, ödeme ref vb.) için.
    /// Mikro'nun user_tablo yapısı.
    /// </summary>
    public class MikroFaturaUserTabloDto
    {
        /// <summary>
        /// E-ticaret sipariş ID.
        /// </summary>
        [JsonPropertyName("EticaretOrderId")]
        public string? EticaretOrderId { get; set; }

        /// <summary>
        /// Ödeme referans numarası.
        /// </summary>
        [JsonPropertyName("PaymentReferenceNumber")]
        public string? PaymentReferenceNumber { get; set; }

        /// <summary>
        /// Taksit sayısı.
        /// </summary>
        [JsonPropertyName("InstallmentCount")]
        public int? InstallmentCount { get; set; }

        /// <summary>
        /// Genel açıklama alanı.
        /// </summary>
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
    }

    /// <summary>
    /// Fatura kaydet response DTO'su.
    /// </summary>
    public class MikroFaturaKaydetResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Mesaj.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Oluşturulan fatura evrak seri.
        /// </summary>
        [JsonPropertyName("evrak_seri")]
        public string? EvrakSeri { get; set; }

        /// <summary>
        /// Oluşturulan fatura evrak sıra.
        /// </summary>
        [JsonPropertyName("evrak_sira")]
        public int? EvrakSira { get; set; }

        /// <summary>
        /// Fatura GUID (Mikro internal ID).
        /// </summary>
        [JsonPropertyName("fatura_guid")]
        public string? FaturaGuid { get; set; }

        /// <summary>
        /// E-Arşiv fatura numarası (kesilmişse).
        /// </summary>
        [JsonPropertyName("e_arsiv_no")]
        public string? EArsivNo { get; set; }
    }
}
