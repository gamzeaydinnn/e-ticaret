using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 FiyatDegisikligiKaydetV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: E-ticaret'ten Mikro'ya fiyat güncellemesi göndermek için.
    /// Kampanya fiyatları veya manuel fiyat değişiklikleri bu şekilde aktarılır.
    /// 
    /// ÖNEMLİ: Genelde tek yönlü akış (Mikro → E-ticaret) önerilir,
    /// ancak bazı senaryolarda (sadece e-ticaret kampanyası) ters yönde
    /// güncelleme gerekebilir.
    /// </summary>
    public class MikroFiyatDegisikligiRequestDto
    {
        /// <summary>
        /// Stok kodu.
        /// </summary>
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        /// <summary>
        /// Fiyat listesi numarası (1-10).
        /// NEDEN: Hangi fiyat listesinin güncelleneceği.
        /// 1 = Perakende (genelde e-ticaret bu listeyi kullanır)
        /// </summary>
        [JsonPropertyName("fiyat_no")]
        public int FiyatNo { get; set; } = 1;

        /// <summary>
        /// Yeni fiyat değeri.
        /// </summary>
        [JsonPropertyName("yeni_fiyat")]
        public decimal YeniFiyat { get; set; }

        /// <summary>
        /// Döviz cinsi.
        /// 0 = TL, 1 = USD, 2 = EUR
        /// </summary>
        [JsonPropertyName("doviz_cinsi")]
        public int DovizCinsi { get; set; } = 0;

        /// <summary>
        /// KDV dahil mi?
        /// </summary>
        [JsonPropertyName("kdv_dahil")]
        public bool KdvDahil { get; set; } = false;

        /// <summary>
        /// Fiyat geçerlilik başlangıç tarihi.
        /// NEDEN: Kampanya fiyatları için başlangıç tarihi.
        /// Boş bırakılırsa hemen geçerli olur.
        /// </summary>
        [JsonPropertyName("gecerlilik_baslangic")]
        public string? GecerlilikBaslangic { get; set; }

        /// <summary>
        /// Fiyat geçerlilik bitiş tarihi.
        /// NEDEN: Kampanya bitiminde fiyatın otomatik geri dönmesi için.
        /// </summary>
        [JsonPropertyName("gecerlilik_bitis")]
        public string? GecerlilikBitis { get; set; }

        /// <summary>
        /// Değişiklik nedeni/açıklaması.
        /// </summary>
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }
    }

    /// <summary>
    /// Toplu fiyat güncelleme için DTO.
    /// NEDEN: Tek tek güncellemek yerine çoklu ürün fiyatı
    /// tek istekte güncellenebilir (daha performanslı).
    /// </summary>
    public class MikroTopluFiyatGuncellemeRequestDto
    {
        /// <summary>
        /// Fiyat değişiklik listesi.
        /// </summary>
        [JsonPropertyName("fiyat_degisiklikleri")]
        public List<MikroFiyatDegisikligiRequestDto> FiyatDegisiklikleri { get; set; } = new();
    }

    /// <summary>
    /// Fiyat güncelleme sonucu.
    /// </summary>
    public class MikroFiyatGuncellemeResponseDto
    {
        [JsonPropertyName("basarili")]
        public bool Basarili { get; set; }

        [JsonPropertyName("mesaj")]
        public string? Mesaj { get; set; }

        /// <summary>
        /// Güncellenen kayıt sayısı.
        /// </summary>
        [JsonPropertyName("guncellenen_kayit")]
        public int GuncellenenKayit { get; set; }

        /// <summary>
        /// Hatalı kayıtlar (varsa).
        /// </summary>
        [JsonPropertyName("hatali_kayitlar")]
        public List<MikroFiyatHataDto>? HataliKayitlar { get; set; }
    }

    /// <summary>
    /// Fiyat güncelleme hatası.
    /// </summary>
    public class MikroFiyatHataDto
    {
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        [JsonPropertyName("hata_mesaj")]
        public string HataMesaj { get; set; } = string.Empty;
    }

    /// <summary>
    /// Fiyat listesi sorgulama request DTO'su.
    /// NEDEN: Mikro'dan güncel fiyatları çekmek için.
    /// Delta senkronizasyonda değişen fiyatlar tespit edilir.
    /// </summary>
    public class MikroFiyatListesiRequestDto
    {
        /// <summary>
        /// Fiyat listesi numarası.
        /// Boş bırakılırsa tüm listeler gelir.
        /// </summary>
        [JsonPropertyName("FiyatNo")]
        public int? FiyatNo { get; set; }

        /// <summary>
        /// Stok kodu filtresi (tek ürün).
        /// </summary>
        [JsonPropertyName("StoKod")]
        public string? StoKod { get; set; }

        /// <summary>
        /// Stok kodu başlangıç filtresi (aralık).
        /// </summary>
        [JsonPropertyName("StoKodBaslangic")]
        public string? StoKodBaslangic { get; set; }

        /// <summary>
        /// Stok kodu bitiş filtresi.
        /// </summary>
        [JsonPropertyName("StoKodBitis")]
        public string? StoKodBitis { get; set; }

        /// <summary>
        /// Son değişiklik tarihinden sonraki kayıtlar.
        /// </summary>
        [JsonPropertyName("DegisiklikTarihiBaslangic")]
        public string? DegisiklikTarihiBaslangic { get; set; }

        /// <summary>
        /// Sadece geçerli fiyatları getir.
        /// NEDEN: Süresi dolmuş kampanya fiyatları hariç tutulur.
        /// </summary>
        [JsonPropertyName("SadeceGecerliFiyatlar")]
        public bool SadeceGecerliFiyatlar { get; set; } = true;

        [JsonPropertyName("SayfaNo")]
        public int SayfaNo { get; set; } = 1;

        [JsonPropertyName("SayfaBuyuklugu")]
        public int SayfaBuyuklugu { get; set; } = 500;
    }

    /// <summary>
    /// Fiyat listesi response DTO'su.
    /// </summary>
    public class MikroFiyatListesiResponseDto
    {
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        [JsonPropertyName("sto_isim")]
        public string? StoIsim { get; set; }

        /// <summary>
        /// Fiyat listesi numarası.
        /// </summary>
        [JsonPropertyName("fiyat_no")]
        public int FiyatNo { get; set; }

        /// <summary>
        /// Fiyat listesi adı.
        /// </summary>
        [JsonPropertyName("fiyat_liste_adi")]
        public string? FiyatListeAdi { get; set; }

        /// <summary>
        /// Fiyat değeri.
        /// </summary>
        [JsonPropertyName("fiyat")]
        public decimal Fiyat { get; set; }

        /// <summary>
        /// Döviz cinsi.
        /// </summary>
        [JsonPropertyName("doviz_cinsi")]
        public int DovizCinsi { get; set; }

        /// <summary>
        /// Döviz kodu (USD, EUR, TL vb.).
        /// </summary>
        [JsonPropertyName("doviz_kod")]
        public string? DovizKod { get; set; }

        /// <summary>
        /// KDV dahil mi?
        /// </summary>
        [JsonPropertyName("kdv_dahil")]
        public bool? KdvDahil { get; set; }

        /// <summary>
        /// KDV oranı.
        /// </summary>
        [JsonPropertyName("kdv_oran")]
        public decimal? KdvOran { get; set; }

        /// <summary>
        /// KDV dahil fiyat (hesaplanmış).
        /// </summary>
        [JsonPropertyName("kdv_dahil_fiyat")]
        public decimal? KdvDahilFiyat { get; set; }

        /// <summary>
        /// Geçerlilik başlangıç tarihi.
        /// </summary>
        [JsonPropertyName("gecerlilik_baslangic")]
        public DateTime? GecerlilikBaslangic { get; set; }

        /// <summary>
        /// Geçerlilik bitiş tarihi.
        /// </summary>
        [JsonPropertyName("gecerlilik_bitis")]
        public DateTime? GecerlilikBitis { get; set; }

        /// <summary>
        /// Fiyat aktif mi?
        /// </summary>
        [JsonPropertyName("aktif")]
        public bool Aktif { get; set; } = true;

        /// <summary>
        /// Son güncelleme tarihi.
        /// </summary>
        [JsonPropertyName("son_guncelleme")]
        public DateTime? SonGuncelleme { get; set; }
    }

    /// <summary>
    /// Ürün fiyat değişikliği bildirimi.
    /// NEDEN: Mikro'dan gelen fiyat değişikliği webhook/polling ile
    /// tespit edildiğinde bu formatta işlenir.
    /// </summary>
    public class MikroFiyatDegisiklikBildirimDto
    {
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        [JsonPropertyName("fiyat_no")]
        public int FiyatNo { get; set; }

        /// <summary>
        /// Önceki fiyat.
        /// </summary>
        [JsonPropertyName("onceki_fiyat")]
        public decimal OncekiFiyat { get; set; }

        /// <summary>
        /// Yeni fiyat.
        /// </summary>
        [JsonPropertyName("yeni_fiyat")]
        public decimal YeniFiyat { get; set; }

        /// <summary>
        /// Değişim yüzdesi (+ veya -).
        /// </summary>
        [JsonPropertyName("degisim_yuzdesi")]
        public decimal DegisimYuzdesi { get; set; }

        /// <summary>
        /// Değişiklik tarihi.
        /// </summary>
        [JsonPropertyName("degisiklik_tarihi")]
        public DateTime DegisiklikTarihi { get; set; }

        /// <summary>
        /// Değişiklik yapan kullanıcı.
        /// </summary>
        [JsonPropertyName("degistiren_kullanici")]
        public string? DegistirenKullanici { get; set; }
    }

    /// <summary>
    /// Kampanyalı fiyat tanımı için DTO.
    /// NEDEN: Belirli tarih aralığında geçerli olan
    /// indirimli fiyatlar bu şekilde tanımlanır.
    /// </summary>
    public class MikroKampanyaFiyatDto
    {
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        /// <summary>
        /// Normal fiyat.
        /// </summary>
        [JsonPropertyName("normal_fiyat")]
        public decimal NormalFiyat { get; set; }

        /// <summary>
        /// Kampanya fiyatı (indirimli).
        /// </summary>
        [JsonPropertyName("kampanya_fiyat")]
        public decimal KampanyaFiyat { get; set; }

        /// <summary>
        /// İndirim tutarı.
        /// </summary>
        [JsonPropertyName("indirim_tutar")]
        public decimal IndirimTutar { get; set; }

        /// <summary>
        /// İndirim yüzdesi.
        /// </summary>
        [JsonPropertyName("indirim_yuzdesi")]
        public decimal IndirimYuzdesi { get; set; }

        /// <summary>
        /// Kampanya başlangıç tarihi.
        /// </summary>
        [JsonPropertyName("baslangic_tarihi")]
        public DateTime BaslangicTarihi { get; set; }

        /// <summary>
        /// Kampanya bitiş tarihi.
        /// </summary>
        [JsonPropertyName("bitis_tarihi")]
        public DateTime BitisTarihi { get; set; }

        /// <summary>
        /// Kampanya adı/açıklaması.
        /// </summary>
        [JsonPropertyName("kampanya_adi")]
        public string? KampanyaAdi { get; set; }

        /// <summary>
        /// Kampanya aktif mi?
        /// NEDEN: Manuel olarak devre dışı bırakılabilir.
        /// </summary>
        [JsonPropertyName("aktif")]
        public bool Aktif { get; set; } = true;
    }

    /// <summary>
    /// Döviz kuru bilgisi.
    /// NEDEN: Dövizli fiyat listelerinde TL karşılığı hesabı için.
    /// </summary>
    public class MikroDovizKuruDto
    {
        /// <summary>
        /// Döviz cinsi kodu.
        /// </summary>
        [JsonPropertyName("doviz_cinsi")]
        public int DovizCinsi { get; set; }

        /// <summary>
        /// Döviz kodu (USD, EUR vb.).
        /// </summary>
        [JsonPropertyName("doviz_kod")]
        public string DovizKod { get; set; } = string.Empty;

        /// <summary>
        /// Alış kuru.
        /// </summary>
        [JsonPropertyName("alis_kuru")]
        public decimal AlisKuru { get; set; }

        /// <summary>
        /// Satış kuru.
        /// </summary>
        [JsonPropertyName("satis_kuru")]
        public decimal SatisKuru { get; set; }

        /// <summary>
        /// Efektif alış.
        /// </summary>
        [JsonPropertyName("efektif_alis")]
        public decimal? EfektifAlis { get; set; }

        /// <summary>
        /// Efektif satış.
        /// </summary>
        [JsonPropertyName("efektif_satis")]
        public decimal? EfektifSatis { get; set; }

        /// <summary>
        /// Kur tarihi.
        /// </summary>
        [JsonPropertyName("tarih")]
        public DateTime Tarih { get; set; }
    }
}
