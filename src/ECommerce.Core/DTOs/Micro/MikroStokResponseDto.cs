using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI StokListesiV2 endpoint'inden dönen stok verisi.
    /// 
    /// NEDEN: Mikro ERP'den e-ticaret'e ürün senkronizasyonu için.
    /// Bu DTO, Mikro'nun döndürdüğü veri yapısını birebir yansıtır.
    /// 
    /// MAPPING: Bu veriler MikroStokMapper ile Product entity'sine dönüştürülür.
    /// </summary>
    public class MikroStokResponseDto
    {
        /// <summary>
        /// Stok kodu - e-ticaret'te SKU olarak kullanılır.
        /// </summary>
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı.
        /// </summary>
        [JsonPropertyName("sto_isim")]
        public string StoIsim { get; set; } = string.Empty;

        /// <summary>
        /// Kısa ürün adı / açıklaması.
        /// </summary>
        [JsonPropertyName("sto_kisa_ismi")]
        public string? StoKisaIsmi { get; set; }

        /// <summary>
        /// Ana birim adı.
        /// </summary>
        [JsonPropertyName("sto_birim1_ad")]
        public string? StoBirim1Ad { get; set; }

        /// <summary>
        /// İkinci birim adı (koli, paket vb.).
        /// </summary>
        [JsonPropertyName("sto_birim2_ad")]
        public string? StoBirim2Ad { get; set; }

        /// <summary>
        /// Birim dönüşüm katsayısı.
        /// NEDEN: 1 koli = 12 adet gibi dönüşümler için.
        /// </summary>
        [JsonPropertyName("sto_birim2_katsayi")]
        public decimal? StoBirim2Katsayi { get; set; }

        /// <summary>
        /// Stok cinsi.
        /// 0=Ticari Mal, 1=Hammadde, 2=Yarı Mamul, 3=Mamul, 4=Hizmet
        /// </summary>
        [JsonPropertyName("sto_cins")]
        public int StoCins { get; set; }

        /// <summary>
        /// Ana grup kodu - üst kategori.
        /// </summary>
        [JsonPropertyName("sto_anagrup_kod")]
        public string? StoAnagrupKod { get; set; }

        /// <summary>
        /// Alt grup kodu - alt kategori.
        /// </summary>
        [JsonPropertyName("sto_altgrup_kod")]
        public string? StoAltgrupKod { get; set; }

        /// <summary>
        /// Marka kodu.
        /// NEDEN: E-ticaret'te marka bazlı filtreleme için.
        /// </summary>
        [JsonPropertyName("sto_marka_kod")]
        public string? StoMarkaKod { get; set; }

        /// <summary>
        /// KDV oranı (toptan).
        /// </summary>
        [JsonPropertyName("sto_toptan_vergi")]
        public decimal StoToptanVergi { get; set; }

        /// <summary>
        /// KDV oranı (perakende).
        /// </summary>
        [JsonPropertyName("sto_perakende_vergi")]
        public decimal StoPerakendeVergi { get; set; }

        /// <summary>
        /// Toplam stok miktarı (tüm depolar).
        /// NEDEN: Genel envanter durumu için.
        /// </summary>
        [JsonPropertyName("sto_miktar")]
        public decimal StoMiktar { get; set; }

        /// <summary>
        /// Belirli bir depodaki stok miktarı.
        /// NEDEN: E-ticaret sadece kendi deposunun stoğunu göstermeli.
        /// </summary>
        [JsonPropertyName("depo_miktar")]
        public decimal? DepoMiktar { get; set; }

        /// <summary>
        /// Rezerve edilen miktar.
        /// NEDEN: Bekleyen siparişler için ayrılan stok.
        /// Satılabilir stok = depo_miktar - rezerve_miktar
        /// </summary>
        [JsonPropertyName("rezerve_miktar")]
        public decimal? RezerveMiktar { get; set; }

        /// <summary>
        /// Minimum stok seviyesi.
        /// </summary>
        [JsonPropertyName("sto_min_miktar")]
        public decimal? StoMinMiktar { get; set; }

        /// <summary>
        /// Maximum stok seviyesi.
        /// </summary>
        [JsonPropertyName("sto_max_miktar")]
        public decimal? StoMaxMiktar { get; set; }

        /// <summary>
        /// Ortalama maliyet (FIFO/Ortalama maliyet yöntemine göre).
        /// NEDEN: Kar marjı hesabı için.
        /// </summary>
        [JsonPropertyName("sto_ort_maliyet")]
        public decimal? StoOrtMaliyet { get; set; }

        /// <summary>
        /// Son alış fiyatı.
        /// </summary>
        [JsonPropertyName("sto_son_alis")]
        public decimal? StoSonAlis { get; set; }

        /// <summary>
        /// Ürün durumu - aktif/pasif.
        /// NEDEN: Pasif ürünler e-ticaret'te gösterilmemeli.
        /// false = Aktif, true = Pasif
        /// </summary>
        [JsonPropertyName("sto_pasif_fl")]
        public bool StoPasifFl { get; set; }

        /// <summary>
        /// Oluşturulma tarihi.
        /// </summary>
        [JsonPropertyName("sto_create_date")]
        public DateTime? StoCreateDate { get; set; }

        /// <summary>
        /// Son güncelleme tarihi.
        /// NEDEN: Delta senkronizasyon için kritik.
        /// Bu tarihten sonra değişen ürünler çekilir.
        /// </summary>
        [JsonPropertyName("sto_lastup_date")]
        public DateTime? StoLastupDate { get; set; }

        /// <summary>
        /// GTİP kodu (gümrük tarife istatistik pozisyonu).
        /// NEDEN: İhracat/ithalat yapan işletmeler için gerekli.
        /// </summary>
        [JsonPropertyName("sto_gtip_no")]
        public string? StoGtipNo { get; set; }

        /// <summary>
        /// Ürün ağırlığı (kg).
        /// NEDEN: Kargo hesaplaması için.
        /// </summary>
        [JsonPropertyName("sto_brut_agirlik")]
        public decimal? StoBrutAgirlik { get; set; }

        /// <summary>
        /// Ürün hacmi (m³).
        /// NEDEN: Depo yerleşimi ve kargo desi hesabı için.
        /// </summary>
        [JsonPropertyName("sto_hacim")]
        public decimal? StoHacim { get; set; }

        /// <summary>
        /// Barkod listesi.
        /// </summary>
        [JsonPropertyName("barkodlar")]
        public List<MikroStokBarkodResponseDto>? Barkodlar { get; set; }

        /// <summary>
        /// Satış fiyatları listesi.
        /// </summary>
        [JsonPropertyName("satis_fiyatlari")]
        public List<MikroStokFiyatResponseDto>? SatisFiyatlari { get; set; }

        /// <summary>
        /// Alış fiyatları listesi.
        /// </summary>
        [JsonPropertyName("alis_fiyatlari")]
        public List<MikroStokFiyatResponseDto>? AlisFiyatlari { get; set; }

        /// <summary>
        /// Depo bazlı stok detayları.
        /// NEDEN: Çoklu depo yapısında her deponun ayrı stoğu.
        /// </summary>
        [JsonPropertyName("depo_stoklari")]
        public List<MikroDepoStokResponseDto>? DepoStoklari { get; set; }

        /// <summary>
        /// Ürün resimleri.
        /// </summary>
        [JsonPropertyName("resimler")]
        public List<MikroStokResimResponseDto>? Resimler { get; set; }

        /// <summary>
        /// Özel alanlar (custom fields).
        /// NEDEN: Mikro'daki özel tanımlı alanlar burada gelir.
        /// E-ticaret'e özel veriler (SEO başlık, meta description vb.)
        /// bu alanlarda tutulabilir.
        /// </summary>
        [JsonPropertyName("ozel_alanlar")]
        public Dictionary<string, string>? OzelAlanlar { get; set; }

        // ==================== HELPER PROPERTIES ====================
        // Bu property'ler mapping işlemlerini kolaylaştırır

        /// <summary>
        /// Ana barkod (computed).
        /// </summary>
        [JsonIgnore]
        public string? Barkod => Barkodlar?.FirstOrDefault(b => b.BarAnaBarkod == true)?.BarBarkodNo
                                ?? Barkodlar?.FirstOrDefault()?.BarBarkodNo;

        /// <summary>
        /// Satış fiyatı 1 (perakende fiyatı).
        /// </summary>
        [JsonIgnore]
        public decimal? SatisFiyat1 => SatisFiyatlari?.FirstOrDefault(f => f.SfiyatNo == 1)?.SfiyatFiyati;

        /// <summary>
        /// KDV oranı (perakende).
        /// </summary>
        [JsonIgnore]
        public decimal? KdvOrani => StoPerakendeVergi;

        /// <summary>
        /// Mevcut stok miktarı (varsayılan depo veya toplam).
        /// </summary>
        [JsonIgnore]
        public decimal? MevcutMiktar => DepoMiktar ?? StoMiktar;

        /// <summary>
        /// Kullanılabilir miktar (mevcut - rezerve).
        /// </summary>
        [JsonIgnore]
        public decimal? KullanilabilirMiktar => (DepoMiktar ?? StoMiktar) - (RezerveMiktar ?? 0);

        /// <summary>
        /// Rezerve miktar.
        /// </summary>
        [JsonIgnore]
        public decimal? RezervedMiktar => RezerveMiktar;

        /// <summary>
        /// Grup kodu (ana veya alt).
        /// </summary>
        [JsonIgnore]
        public string? GrupKodu => StoAltgrupKod ?? StoAnagrupKod;

        /// <summary>
        /// Birim adı.
        /// </summary>
        [JsonIgnore]
        public string? BirimAdi => StoBirim1Ad;

        /// <summary>
        /// Aktif mi?
        /// </summary>
        [JsonIgnore]
        public bool? Aktif => !StoPasifFl;

        /// <summary>
        /// Son değişiklik tarihi.
        /// </summary>
        [JsonIgnore]
        public DateTime? DegisiklikTarihi => StoLastupDate ?? StoCreateDate;
    }

    /// <summary>
    /// Barkod response DTO'su.
    /// </summary>
    public class MikroStokBarkodResponseDto
    {
        [JsonPropertyName("bar_barkodno")]
        public string BarBarkodNo { get; set; } = string.Empty;

        [JsonPropertyName("bar_carpan")]
        public decimal BarCarpan { get; set; } = 1;

        [JsonPropertyName("bar_birimi")]
        public int BarBirimi { get; set; }

        /// <summary>
        /// Ana barkod mu?
        /// </summary>
        [JsonPropertyName("bar_ana_barkod")]
        public bool? BarAnaBarkod { get; set; }
    }

    /// <summary>
    /// Fiyat response DTO'su.
    /// </summary>
    public class MikroStokFiyatResponseDto
    {
        /// <summary>
        /// Fiyat numarası (1-10).
        /// </summary>
        [JsonPropertyName("sfiyat_no")]
        public int SfiyatNo { get; set; }

        /// <summary>
        /// Fiyat tipi açıklaması.
        /// </summary>
        [JsonPropertyName("sfiyat_aciklama")]
        public string? SfiyatAciklama { get; set; }

        /// <summary>
        /// Fiyat değeri.
        /// </summary>
        [JsonPropertyName("sfiyat_fiyati")]
        public decimal SfiyatFiyati { get; set; }

        /// <summary>
        /// Para birimi (0=TL, 1=USD, 2=EUR).
        /// </summary>
        [JsonPropertyName("sfiyat_doviz_cinsi")]
        public int SfiyatDovizCinsi { get; set; }

        /// <summary>
        /// KDV dahil mi?
        /// NEDEN: Fiyat gösteriminde KDV dahil/hariç seçimi için.
        /// </summary>
        [JsonPropertyName("sfiyat_vergi_dahil")]
        public bool? SfiyatVergiDahil { get; set; }

        /// <summary>
        /// Fiyat geçerlilik başlangıç tarihi.
        /// NEDEN: Kampanyalı fiyatların belirli tarih aralığında geçerli olması için.
        /// </summary>
        [JsonPropertyName("sfiyat_baslangic_tarihi")]
        public DateTime? SfiyatBaslangicTarihi { get; set; }

        /// <summary>
        /// Fiyat geçerlilik bitiş tarihi.
        /// </summary>
        [JsonPropertyName("sfiyat_bitis_tarihi")]
        public DateTime? SfiyatBitisTarihi { get; set; }
    }

    /// <summary>
    /// Depo bazlı stok bilgisi.
    /// </summary>
    public class MikroDepoStokResponseDto
    {
        /// <summary>
        /// Depo numarası.
        /// </summary>
        [JsonPropertyName("dep_no")]
        public int DepNo { get; set; }

        /// <summary>
        /// Depo adı.
        /// </summary>
        [JsonPropertyName("dep_ad")]
        public string? DepAd { get; set; }

        /// <summary>
        /// Bu depodaki stok miktarı.
        /// </summary>
        [JsonPropertyName("stok_miktar")]
        public decimal StokMiktar { get; set; }

        /// <summary>
        /// Rezerve edilen miktar.
        /// </summary>
        [JsonPropertyName("rezerve_miktar")]
        public decimal? RezerveMiktar { get; set; }

        /// <summary>
        /// Satılabilir miktar (stok - rezerve).
        /// </summary>
        [JsonPropertyName("satilabilir_miktar")]
        public decimal? SatilabilirMiktar { get; set; }
    }

    /// <summary>
    /// Stok resim response DTO'su.
    /// </summary>
    public class MikroStokResimResponseDto
    {
        /// <summary>
        /// Resim URL'si.
        /// </summary>
        [JsonPropertyName("resim_url")]
        public string? ResimUrl { get; set; }

        /// <summary>
        /// Base64 encoded resim data.
        /// NEDEN: URL yerine doğrudan resim verisi de gelebilir.
        /// </summary>
        [JsonPropertyName("resim_data")]
        public string? ResimData { get; set; }

        /// <summary>
        /// Resim sırası.
        /// </summary>
        [JsonPropertyName("resim_sira")]
        public int ResimSira { get; set; }

        /// <summary>
        /// Resim tipi (jpg, png vb.).
        /// </summary>
        [JsonPropertyName("resim_tipi")]
        public string? ResimTipi { get; set; }
    }

    /// <summary>
    /// Stok değişikliği bildirim DTO'su.
    /// 
    /// NEDEN: Mikro'dan gelen webhook veya polling ile
    /// tespit edilen stok değişiklikleri bu formatta işlenir.
    /// Mağaza satışı olduğunda bu bilgi e-ticaret'e yansıtılır.
    /// </summary>
    public class MikroStokDegisiklikDto
    {
        /// <summary>
        /// Stok kodu.
        /// </summary>
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        /// <summary>
        /// Depo numarası.
        /// </summary>
        [JsonPropertyName("depo_no")]
        public int DepoNo { get; set; }

        /// <summary>
        /// Önceki miktar.
        /// </summary>
        [JsonPropertyName("onceki_miktar")]
        public decimal OncekiMiktar { get; set; }

        /// <summary>
        /// Yeni miktar.
        /// </summary>
        [JsonPropertyName("yeni_miktar")]
        public decimal YeniMiktar { get; set; }

        /// <summary>
        /// Değişiklik miktarı (+ veya -).
        /// </summary>
        [JsonPropertyName("degisim_miktari")]
        public decimal DegisimMiktari { get; set; }

        /// <summary>
        /// Değişiklik tarihi.
        /// </summary>
        [JsonPropertyName("degisiklik_tarihi")]
        public DateTime DegisiklikTarihi { get; set; }

        /// <summary>
        /// Değişiklik nedeni.
        /// NEDEN: Satış mı, iade mi, sayım farkı mı anlamak için.
        /// </summary>
        [JsonPropertyName("degisiklik_nedeni")]
        public string? DegisiklikNedeni { get; set; }

        /// <summary>
        /// İlgili evrak numarası (fatura no, irsaliye no vb.).
        /// </summary>
        [JsonPropertyName("evrak_no")]
        public string? EvrakNo { get; set; }
    }
}
