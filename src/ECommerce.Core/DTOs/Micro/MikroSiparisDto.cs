using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 SiparisKaydetV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: E-ticaret siparişlerini Mikro ERP'ye aktarmak için.
    /// Online sipariş geldiğinde bu DTO ile Mikro'ya kaydedilir,
    /// böylece stok düşer ve faturalama yapılabilir.
    /// 
    /// AKIŞ: E-ticaret Sipariş → MikroSiparisKaydetRequestDto → MikroAPI → Mikro ERP
    /// </summary>
    public class MikroSiparisKaydetRequestDto
    {
        /// <summary>
        /// Sipariş evrak serisi.
        /// NEDEN: Mikro ERP'de evraklar seri ile ayrışır.
        /// Online siparişler için ayrı bir seri kullanılmalı (örn: "ONL").
        /// </summary>
        [JsonPropertyName("sip_evrakno_seri")]
        public string SipEvraknoSeri { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş evrak numarası.
        /// NEDEN: Her sipariş benzersiz bir numara alır.
        /// Boş bırakılırsa Mikro otomatik numara verir.
        /// </summary>
        [JsonPropertyName("sip_evrakno_sira")]
        public int? SipEvraknoSira { get; set; }

        /// <summary>
        /// Sipariş tarihi.
        /// Format: "yyyy-MM-dd" veya "yyyy-MM-dd HH:mm:ss"
        /// </summary>
        [JsonPropertyName("sip_tarih")]
        public string SipTarih { get; set; } = string.Empty;

        /// <summary>
        /// Teslim tarihi (tahmini).
        /// NEDEN: Mikro'da teslim tarihine göre planlama yapılır.
        /// </summary>
        [JsonPropertyName("sip_teslim_tarih")]
        public string? SipTeslimTarih { get; set; }

        /// <summary>
        /// Müşteri (cari) kodu.
        /// NEDEN: Siparişin hangi müşteriye ait olduğu.
        /// Eğer yeni müşteriyse önce CariKaydetV2 ile oluşturulmalı.
        /// </summary>
        [JsonPropertyName("sip_musteri_kod")]
        public string SipMusteriKod { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş tipi.
        /// 0 = Satış Siparişi, 1 = Alış Siparişi
        /// NEDEN: E-ticaret için her zaman 0 (satış).
        /// </summary>
        [JsonPropertyName("sip_tip")]
        public int SipTip { get; set; } = 0;

        /// <summary>
        /// Sipariş cinsi.
        /// 0 = Normal, 1 = İade, 2 = Teklif
        /// </summary>
        [JsonPropertyName("sip_cins")]
        public int SipCins { get; set; } = 0;

        /// <summary>
        /// Depo numarası.
        /// NEDEN: Stok hangi depodan düşecek.
        /// Online satışlar için ayrı depo olabilir.
        /// </summary>
        [JsonPropertyName("sip_depession")]
        public int SipDepoNo { get; set; }

        /// <summary>
        /// Şube numarası.
        /// NEDEN: Çoklu şube yapısında hangi şubenin satışı.
        /// </summary>
        [JsonPropertyName("sip_sube")]
        public int? SipSube { get; set; }

        /// <summary>
        /// Sipariş açıklaması.
        /// NEDEN: Müşteri notu, özel istekler vb.
        /// </summary>
        [JsonPropertyName("sip_aciklama")]
        public string? SipAciklama { get; set; }

        /// <summary>
        /// Sipariş durumu.
        /// 0 = Açık, 1 = Kısmi Teslim, 2 = Kapalı, 3 = İptal
        /// </summary>
        [JsonPropertyName("sip_durum")]
        public int SipDurum { get; set; } = 0;

        /// <summary>
        /// Ödeme şekli kodu.
        /// NEDEN: Mikro'daki ödeme şekli tanımlarına referans.
        /// Örn: NAK=Nakit, KK=Kredi Kartı, HVL=Havale
        /// </summary>
        [JsonPropertyName("sip_odeme_kod")]
        public string? SipOdemeKod { get; set; }

        /// <summary>
        /// Döviz cinsi.
        /// 0 = TL, 1 = USD, 2 = EUR vb.
        /// </summary>
        [JsonPropertyName("sip_doviz_cinsi")]
        public int SipDovizCinsi { get; set; } = 0;

        /// <summary>
        /// Döviz kuru.
        /// NEDEN: Dövizli siparişlerde kur bilgisi gerekli.
        /// </summary>
        [JsonPropertyName("sip_doviz_kuru")]
        public decimal? SipDovizKuru { get; set; }

        /// <summary>
        /// Toplam tutar (KDV hariç).
        /// </summary>
        [JsonPropertyName("sip_tutar")]
        public decimal SipTutar { get; set; }

        /// <summary>
        /// Toplam KDV tutarı.
        /// </summary>
        [JsonPropertyName("sip_vergi")]
        public decimal SipVergi { get; set; }

        /// <summary>
        /// İskonto tutarı.
        /// </summary>
        [JsonPropertyName("sip_iskonto")]
        public decimal? SipIskonto { get; set; }

        /// <summary>
        /// Genel toplam (KDV dahil).
        /// </summary>
        [JsonPropertyName("sip_genel_toplam")]
        public decimal SipGenelToplam { get; set; }

        /// <summary>
        /// Kargo/teslimat ücreti.
        /// NEDEN: E-ticaret'te kargo ayrı kalem olarak gösterilir.
        /// </summary>
        [JsonPropertyName("sip_kargo_tutar")]
        public decimal? SipKargoTutar { get; set; }

        /// <summary>
        /// E-ticaret sipariş numarası (referans).
        /// NEDEN: Çift yönlü eşleştirme için kritik.
        /// Mikro'da bu numara ile e-ticaret siparişi bulunabilir.
        /// </summary>
        [JsonPropertyName("sip_ozel_kod")]
        public string? SipOzelKod { get; set; }

        /// <summary>
        /// Teslimat adresi ID (Mikro'daki adres kodu).
        /// </summary>
        [JsonPropertyName("sip_adres_no")]
        public int? SipAdresNo { get; set; }

        /// <summary>
        /// Sipariş satırları (ürünler).
        /// </summary>
        [JsonPropertyName("satirlar")]
        public List<MikroSiparisSatirDto> Satirlar { get; set; } = new();

        /// <summary>
        /// Teslimat adresi bilgileri.
        /// NEDEN: Müşterinin varsayılan adresi dışında farklı
        /// bir adrese teslimat yapılacaksa burası doldurulur.
        /// </summary>
        [JsonPropertyName("teslimat_adresi")]
        public MikroSiparisTeslimatAdresiDto? TeslimatAdresi { get; set; }
    }

    /// <summary>
    /// Sipariş satır (kalem) bilgisi.
    /// Her bir ürün için ayrı satır oluşturulur.
    /// </summary>
    public class MikroSiparisSatirDto
    {
        /// <summary>
        /// Satır numarası (1'den başlar).
        /// </summary>
        [JsonPropertyName("sip_satirno")]
        public int SipSatirNo { get; set; }

        /// <summary>
        /// Stok kodu.
        /// NEDEN: Hangi ürün sipariş edildi.
        /// </summary>
        [JsonPropertyName("sip_stok_kod")]
        public string SipStokKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı (opsiyonel).
        /// NEDEN: Stok kodundan farklı isim gösterilecekse.
        /// </summary>
        [JsonPropertyName("sip_stok_isim")]
        public string? SipStokIsim { get; set; }

        /// <summary>
        /// Sipariş miktarı.
        /// </summary>
        [JsonPropertyName("sip_miktar")]
        public decimal SipMiktar { get; set; }

        /// <summary>
        /// Teslim edilen miktar.
        /// NEDEN: Kısmi teslimat durumunda takip için.
        /// </summary>
        [JsonPropertyName("sip_teslim_miktar")]
        public decimal? SipTeslimMiktar { get; set; }

        /// <summary>
        /// Birim fiyat (KDV hariç).
        /// </summary>
        [JsonPropertyName("sip_b_fiyat")]
        public decimal SipBFiyat { get; set; }

        /// <summary>
        /// KDV oranı (%).
        /// </summary>
        [JsonPropertyName("sip_vergi_puan")]
        public decimal SipVergiPuan { get; set; }

        /// <summary>
        /// Satır iskonto oranı (%).
        /// </summary>
        [JsonPropertyName("sip_iskonto_1")]
        public decimal? SipIskonto1 { get; set; }

        /// <summary>
        /// İkinci iskonto oranı (kademeli iskonto için).
        /// </summary>
        [JsonPropertyName("sip_iskonto_2")]
        public decimal? SipIskonto2 { get; set; }

        /// <summary>
        /// Satır tutarı (miktar × birim fiyat).
        /// </summary>
        [JsonPropertyName("sip_tutar")]
        public decimal SipTutar { get; set; }

        /// <summary>
        /// Depo numarası (satır bazlı).
        /// NEDEN: Farklı ürünler farklı depolardan çıkabilir.
        /// </summary>
        [JsonPropertyName("sip_depono")]
        public int? SipDepoNo { get; set; }

        /// <summary>
        /// Birim adı.
        /// </summary>
        [JsonPropertyName("sip_birim")]
        public string? SipBirim { get; set; }

        /// <summary>
        /// Satır açıklaması.
        /// NEDEN: Varyant bilgisi, özelleştirme notu vb.
        /// Örn: "Beden: L, Renk: Mavi"
        /// </summary>
        [JsonPropertyName("sip_aciklama")]
        public string? SipAciklama { get; set; }

        /// <summary>
        /// E-ticaret sipariş kalemi ID'si.
        /// NEDEN: Çift yönlü eşleştirme için.
        /// </summary>
        [JsonPropertyName("sip_ozel_kod")]
        public string? SipOzelKod { get; set; }
    }

    /// <summary>
    /// Teslimat adresi bilgileri.
    /// </summary>
    public class MikroSiparisTeslimatAdresiDto
    {
        /// <summary>
        /// Adres başlığı/adı.
        /// Örn: "Ev Adresi", "İş Adresi"
        /// </summary>
        [JsonPropertyName("adres_baslik")]
        public string? AdresBaslik { get; set; }

        /// <summary>
        /// Alıcı adı soyadı.
        /// </summary>
        [JsonPropertyName("adres_alici")]
        public string AdresAlici { get; set; } = string.Empty;

        /// <summary>
        /// Adres satırı 1.
        /// </summary>
        [JsonPropertyName("adres_cadde")]
        public string AdresCadde { get; set; } = string.Empty;

        /// <summary>
        /// Mahalle.
        /// </summary>
        [JsonPropertyName("adres_mahalle")]
        public string? AdresMahalle { get; set; }

        /// <summary>
        /// İlçe.
        /// </summary>
        [JsonPropertyName("adres_ilce")]
        public string AdresIlce { get; set; } = string.Empty;

        /// <summary>
        /// İl.
        /// </summary>
        [JsonPropertyName("adres_il")]
        public string AdresIl { get; set; } = string.Empty;

        /// <summary>
        /// Posta kodu.
        /// </summary>
        [JsonPropertyName("adres_posta_kodu")]
        public string? AdresPostaKodu { get; set; }

        /// <summary>
        /// Telefon numarası.
        /// </summary>
        [JsonPropertyName("adres_telefon")]
        public string? AdresTelefon { get; set; }

        /// <summary>
        /// Teslimat notu.
        /// NEDEN: Kurye için özel talimatlar.
        /// Örn: "Kapıda ödeme", "Komşuya bırakılabilir"
        /// </summary>
        [JsonPropertyName("adres_not")]
        public string? AdresNot { get; set; }
    }

    /// <summary>
    /// Sipariş kayıt sonucu.
    /// </summary>
    public class MikroSiparisKaydetResponseDto
    {
        /// <summary>
        /// Oluşturulan sipariş evrak serisi.
        /// </summary>
        [JsonPropertyName("evrak_seri")]
        public string EvrakSeri { get; set; } = string.Empty;

        /// <summary>
        /// Oluşturulan sipariş evrak numarası.
        /// NEDEN: E-ticaret tarafında bu numara saklanmalı,
        /// iade/iptal işlemlerinde bu numara kullanılacak.
        /// </summary>
        [JsonPropertyName("evrak_sira")]
        public int EvrakSira { get; set; }

        /// <summary>
        /// Mikro internal sipariş ID.
        /// </summary>
        [JsonPropertyName("siparis_id")]
        public long? SiparisId { get; set; }

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("basarili")]
        public bool Basarili { get; set; }

        /// <summary>
        /// Hata veya bilgi mesajı.
        /// </summary>
        [JsonPropertyName("mesaj")]
        public string? Mesaj { get; set; }

        /// <summary>
        /// Uyarılar listesi (hata olmasa bile uyarı olabilir).
        /// Örn: "Stok kritik seviyenin altında"
        /// </summary>
        [JsonPropertyName("uyarilar")]
        public List<string>? Uyarilar { get; set; }
    }

    /// <summary>
    /// Sipariş listesi sorgusu için istek DTO'su.
    /// NEDEN: Mikro'dan e-ticaret'e sipariş durumu senkronizasyonu için.
    /// Mağazadan verilen siparişler veya durum güncellemeleri çekilir.
    /// </summary>
    public class MikroSiparisListesiRequestDto
    {
        /// <summary>
        /// Başlangıç tarihi.
        /// </summary>
        [JsonPropertyName("BaslangicTarih")]
        public string? BaslangicTarih { get; set; }

        /// <summary>
        /// Bitiş tarihi.
        /// </summary>
        [JsonPropertyName("BitisTarih")]
        public string? BitisTarih { get; set; }

        /// <summary>
        /// Müşteri kodu filtresi.
        /// </summary>
        [JsonPropertyName("MusteriKod")]
        public string? MusteriKod { get; set; }

        /// <summary>
        /// Evrak serisi filtresi.
        /// NEDEN: Sadece online siparişleri (ONL serisi) çekmek için.
        /// </summary>
        [JsonPropertyName("EvrakSeri")]
        public string? EvrakSeri { get; set; }

        /// <summary>
        /// Sipariş durumu filtresi.
        /// </summary>
        [JsonPropertyName("Durum")]
        public int? Durum { get; set; }

        /// <summary>
        /// Son güncelleme tarihinden sonraki kayıtlar.
        /// NEDEN: Delta senkronizasyon için.
        /// </summary>
        [JsonPropertyName("GuncellemeTarihiBaslangic")]
        public string? GuncellemeTarihiBaslangic { get; set; }

        /// <summary>
        /// Sayfa numarası.
        /// </summary>
        [JsonPropertyName("SayfaNo")]
        public int SayfaNo { get; set; } = 1;

        /// <summary>
        /// Sayfa boyutu.
        /// </summary>
        [JsonPropertyName("SayfaBuyuklugu")]
        public int SayfaBuyuklugu { get; set; } = 50;
    }

    /// <summary>
    /// Sipariş listesi response DTO'su.
    /// </summary>
    public class MikroSiparisListesiResponseDto
    {
        [JsonPropertyName("sip_evrakno_seri")]
        public string SipEvraknoSeri { get; set; } = string.Empty;

        [JsonPropertyName("sip_evrakno_sira")]
        public int SipEvraknoSira { get; set; }

        [JsonPropertyName("sip_tarih")]
        public DateTime SipTarih { get; set; }

        [JsonPropertyName("sip_musteri_kod")]
        public string SipMusteriKod { get; set; } = string.Empty;

        [JsonPropertyName("sip_musteri_isim")]
        public string? SipMusteriIsim { get; set; }

        [JsonPropertyName("sip_durum")]
        public int SipDurum { get; set; }

        /// <summary>
        /// Sipariş durumu açıklaması.
        /// </summary>
        [JsonPropertyName("sip_durum_aciklama")]
        public string? SipDurumAciklama { get; set; }

        [JsonPropertyName("sip_genel_toplam")]
        public decimal SipGenelToplam { get; set; }

        /// <summary>
        /// E-ticaret sipariş referansı.
        /// </summary>
        [JsonPropertyName("sip_ozel_kod")]
        public string? SipOzelKod { get; set; }

        /// <summary>
        /// Sipariş satırları.
        /// </summary>
        [JsonPropertyName("satirlar")]
        public List<MikroSiparisListesiSatirDto>? Satirlar { get; set; }

        /// <summary>
        /// Son güncelleme tarihi.
        /// </summary>
        [JsonPropertyName("sip_lastup_date")]
        public DateTime? SipLastupDate { get; set; }
    }

    /// <summary>
    /// Sipariş listesi satır DTO'su.
    /// </summary>
    public class MikroSiparisListesiSatirDto
    {
        [JsonPropertyName("sip_stok_kod")]
        public string SipStokKod { get; set; } = string.Empty;

        [JsonPropertyName("sip_stok_isim")]
        public string? SipStokIsim { get; set; }

        [JsonPropertyName("sip_miktar")]
        public decimal SipMiktar { get; set; }

        [JsonPropertyName("sip_teslim_miktar")]
        public decimal? SipTeslimMiktar { get; set; }

        [JsonPropertyName("sip_b_fiyat")]
        public decimal SipBFiyat { get; set; }

        [JsonPropertyName("sip_tutar")]
        public decimal SipTutar { get; set; }
    }

    /// <summary>
    /// Sipariş durum güncelleme DTO'su.
    /// NEDEN: E-ticaret'ten sipariş durumu değiştiğinde
    /// (örn: kargoya verildi) Mikro'ya bildirim için.
    /// </summary>
    public class MikroSiparisDurumGuncelleRequestDto
    {
        /// <summary>
        /// Evrak serisi.
        /// </summary>
        [JsonPropertyName("evrak_seri")]
        public string EvrakSeri { get; set; } = string.Empty;

        /// <summary>
        /// Evrak numarası.
        /// </summary>
        [JsonPropertyName("evrak_sira")]
        public int EvrakSira { get; set; }

        /// <summary>
        /// Yeni durum.
        /// 0 = Açık, 1 = Kısmi Teslim, 2 = Kapalı, 3 = İptal
        /// </summary>
        [JsonPropertyName("yeni_durum")]
        public int YeniDurum { get; set; }

        /// <summary>
        /// Durum değişiklik açıklaması.
        /// </summary>
        [JsonPropertyName("aciklama")]
        public string? Aciklama { get; set; }

        /// <summary>
        /// Kargo takip numarası.
        /// NEDEN: Kargoya verildiğinde bu bilgi eklenir.
        /// </summary>
        [JsonPropertyName("kargo_takip_no")]
        public string? KargoTakipNo { get; set; }

        /// <summary>
        /// Kargo firması.
        /// </summary>
        [JsonPropertyName("kargo_firma")]
        public string? KargoFirma { get; set; }
    }

    /// <summary>
    /// Sipariş iptal/iade DTO'su.
    /// </summary>
    public class MikroSiparisIptalRequestDto
    {
        [JsonPropertyName("evrak_seri")]
        public string EvrakSeri { get; set; } = string.Empty;

        [JsonPropertyName("evrak_sira")]
        public int EvrakSira { get; set; }

        /// <summary>
        /// İptal mi iade mi?
        /// true = Tam iptal, false = Kısmi iade
        /// </summary>
        [JsonPropertyName("tam_iptal")]
        public bool TamIptal { get; set; } = true;

        /// <summary>
        /// İptal/iade nedeni.
        /// </summary>
        [JsonPropertyName("neden")]
        public string? Neden { get; set; }

        /// <summary>
        /// İade edilecek satırlar (kısmi iade için).
        /// </summary>
        [JsonPropertyName("iade_satirlar")]
        public List<MikroSiparisIadeSatirDto>? IadeSatirlari { get; set; }
    }

    /// <summary>
    /// İade satır bilgisi.
    /// </summary>
    public class MikroSiparisIadeSatirDto
    {
        [JsonPropertyName("stok_kod")]
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// İade miktarı.
        /// </summary>
        [JsonPropertyName("iade_miktar")]
        public decimal IadeMiktar { get; set; }

        /// <summary>
        /// İade nedeni (satır bazlı).
        /// </summary>
        [JsonPropertyName("neden")]
        public string? Neden { get; set; }
    }
}
