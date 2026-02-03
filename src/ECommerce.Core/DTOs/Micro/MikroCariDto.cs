using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 CariKaydetV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: E-ticaret müşterilerini Mikro ERP'ye aktarmak için.
    /// Yeni müşteri sipariş verdiğinde önce cari hesap oluşturulur,
    /// sonra sipariş bu cari hesaba bağlanır.
    /// 
    /// AKIŞ: E-ticaret Müşteri → MikroCariKaydetRequestDto → MikroAPI → Mikro Cari Hesap
    /// </summary>
    public class MikroCariKaydetRequestDto
    {
        /// <summary>
        /// Cari hesap kodu.
        /// NEDEN: Her müşterinin benzersiz tanımlayıcısı.
        /// Format önerisi: "ONL-{UserId}" veya "WEB-{UserId}"
        /// Bu sayede online müşteriler kolayca ayrıştırılır.
        /// </summary>
        [JsonPropertyName("cari_kod")]
        public string CariKod { get; set; } = string.Empty;

        /// <summary>
        /// Cari ünvan 1 (Ana isim).
        /// Bireysel müşterilerde: Ad Soyad
        /// Kurumsal müşterilerde: Firma Adı
        /// </summary>
        [JsonPropertyName("cari_unvan1")]
        public string CariUnvan1 { get; set; } = string.Empty;

        /// <summary>
        /// Cari ünvan 2 (Ek bilgi).
        /// NEDEN: Uzun firma isimlerinde devamı buraya yazılır.
        /// Veya şube/departman bilgisi eklenebilir.
        /// </summary>
        [JsonPropertyName("cari_unvan2")]
        public string? CariUnvan2 { get; set; }

        /// <summary>
        /// Cari hesap tipi.
        /// 0 = Müşteri (Alıcı)
        /// 1 = Tedarikçi (Satıcı)
        /// 2 = Hem Müşteri Hem Tedarikçi
        /// NEDEN: E-ticaret için her zaman 0 (Müşteri).
        /// </summary>
        [JsonPropertyName("cari_hareket_tipi")]
        public int CariHareketTipi { get; set; } = 0;

        /// <summary>
        /// Cari grubu.
        /// NEDEN: Raporlama ve segmentasyon için.
        /// Örn: "ONLINE", "PREMIUM", "KURUMSAL"
        /// </summary>
        [JsonPropertyName("cari_bolge_kodu")]
        public string? CariBolgeKodu { get; set; }

        /// <summary>
        /// E-posta adresi.
        /// NEDEN: Fatura gönderimi ve iletişim için kritik.
        /// </summary>
        [JsonPropertyName("cari_EMail")]
        public string? CariEmail { get; set; }

        /// <summary>
        /// Cep telefonu.
        /// NEDEN: SMS bildirimleri ve kurye iletişimi için.
        /// Format: 05XXXXXXXXX (başında 0 ile)
        /// </summary>
        [JsonPropertyName("cari_CepTel")]
        public string? CariCepTel { get; set; }

        /// <summary>
        /// Sabit telefon.
        /// </summary>
        [JsonPropertyName("cari_Tel1")]
        public string? CariTel1 { get; set; }

        /// <summary>
        /// Vergi dairesi.
        /// NEDEN: Kurumsal müşteriler için fatura kesiminde zorunlu.
        /// </summary>
        [JsonPropertyName("cari_vdaire_adi")]
        public string? CariVdaireAdi { get; set; }

        /// <summary>
        /// Vergi numarası (10 haneli).
        /// NEDEN: Kurumsal müşteriler için fatura kesiminde zorunlu.
        /// </summary>
        [JsonPropertyName("cari_vdaire_no")]
        public string? CariVdaireNo { get; set; }

        /// <summary>
        /// TC Kimlik numarası (11 haneli).
        /// NEDEN: Bireysel müşteriler için e-fatura/e-arşiv zorunluluğu.
        /// </summary>
        [JsonPropertyName("cari_tckno")]
        public string? CariTckNo { get; set; }

        /// <summary>
        /// Şirket türü.
        /// 0 = Şahıs, 1 = Tüzel Kişi (Şirket)
        /// NEDEN: Fatura türü belirleme için.
        /// </summary>
        [JsonPropertyName("cari_kisi_kurum_flg")]
        public int CariKisiKurumFlg { get; set; } = 0;

        /// <summary>
        /// Ödeme vadesi (gün).
        /// NEDEN: Açık hesap çalışan müşteriler için.
        /// Varsayılan: 0 (Peşin)
        /// </summary>
        [JsonPropertyName("cari_odeme_vade")]
        public int? CariOdemeVade { get; set; }

        /// <summary>
        /// Risk limiti.
        /// NEDEN: Müşterinin açık hesap limiti.
        /// Bu limite ulaşılırsa yeni sipariş engellenebilir.
        /// </summary>
        [JsonPropertyName("cari_risk_limit")]
        public decimal? CariRiskLimit { get; set; }

        /// <summary>
        /// Fiyat listesi numarası (1-10).
        /// NEDEN: Mikro'da müşteriye özel fiyat listesi atanabilir.
        /// 1 = Standart, 2 = VIP, 3 = Kurumsal vb.
        /// </summary>
        [JsonPropertyName("cari_fiyat_listeno")]
        public int? CariFiyatListeNo { get; set; }

        /// <summary>
        /// İskonto oranı (%).
        /// NEDEN: Bu müşteriye her zaman uygulanan sabit iskonto.
        /// </summary>
        [JsonPropertyName("cari_iskonto_oran")]
        public decimal? CariIskontoOran { get; set; }

        /// <summary>
        /// Temsilci kodu.
        /// NEDEN: Satış temsilcisi bazlı raporlama ve komisyon hesabı için.
        /// </summary>
        [JsonPropertyName("cari_temsilci_kodu")]
        public string? CariTemsilciKodu { get; set; }

        /// <summary>
        /// Cari hesap durumu.
        /// false = Aktif, true = Pasif
        /// </summary>
        [JsonPropertyName("cari_pasif_fl")]
        public bool CariPasifFl { get; set; } = false;

        /// <summary>
        /// E-ticaret kullanıcı ID'si (referans).
        /// NEDEN: Çift yönlü eşleştirme için kritik.
        /// </summary>
        [JsonPropertyName("cari_ozel_kod")]
        public string? CariOzelKod { get; set; }

        /// <summary>
        /// Not/Açıklama.
        /// </summary>
        [JsonPropertyName("cari_aciklama")]
        public string? CariAciklama { get; set; }

        /// <summary>
        /// Web sitesi.
        /// </summary>
        [JsonPropertyName("cari_web")]
        public string? CariWeb { get; set; }

        /// <summary>
        /// Adres listesi.
        /// NEDEN: Bir müşterinin birden fazla adresi olabilir
        /// (ev, iş, teslimat noktası vb.).
        /// </summary>
        [JsonPropertyName("adresler")]
        public List<MikroCariAdresDto>? Adresler { get; set; }

        /// <summary>
        /// Yetkili kişiler listesi.
        /// NEDEN: Kurumsal müşterilerde birden fazla yetkili olabilir.
        /// </summary>
        [JsonPropertyName("yetkililer")]
        public List<MikroCariYetkiliDto>? Yetkililer { get; set; }
    }

    /// <summary>
    /// Cari adres bilgisi.
    /// </summary>
    public class MikroCariAdresDto
    {
        /// <summary>
        /// Adres numarası (1'den başlar).
        /// 1 genellikle fatura adresi olarak kullanılır.
        /// </summary>
        [JsonPropertyName("adr_no")]
        public int AdrNo { get; set; } = 1;

        /// <summary>
        /// Adres başlığı.
        /// Örn: "Merkez", "Şube", "Depo", "Ev", "İş"
        /// </summary>
        [JsonPropertyName("adr_baslik")]
        public string? AdrBaslik { get; set; }

        /// <summary>
        /// Adres tipi.
        /// 0 = Fatura Adresi
        /// 1 = Teslimat Adresi
        /// 2 = Her ikisi
        /// </summary>
        [JsonPropertyName("adr_tipi")]
        public int AdrTipi { get; set; } = 2;

        /// <summary>
        /// Cadde/Sokak.
        /// </summary>
        [JsonPropertyName("adr_cadde")]
        public string? AdrCadde { get; set; }

        /// <summary>
        /// Mahalle.
        /// </summary>
        [JsonPropertyName("adr_mahalle")]
        public string? AdrMahalle { get; set; }

        /// <summary>
        /// Bina no / Daire no.
        /// </summary>
        [JsonPropertyName("adr_bina_no")]
        public string? AdrBinaNo { get; set; }

        /// <summary>
        /// İlçe.
        /// </summary>
        [JsonPropertyName("adr_ilce")]
        public string? AdrIlce { get; set; }

        /// <summary>
        /// İl.
        /// </summary>
        [JsonPropertyName("adr_il")]
        public string? AdrIl { get; set; }

        /// <summary>
        /// Ülke.
        /// Varsayılan: "TÜRKİYE"
        /// </summary>
        [JsonPropertyName("adr_ulke")]
        public string? AdrUlke { get; set; } = "TÜRKİYE";

        /// <summary>
        /// Posta kodu.
        /// </summary>
        [JsonPropertyName("adr_posta_kodu")]
        public string? AdrPostaKodu { get; set; }

        /// <summary>
        /// Telefon (adres bazlı).
        /// </summary>
        [JsonPropertyName("adr_telefon")]
        public string? AdrTelefon { get; set; }

        /// <summary>
        /// Varsayılan adres mi?
        /// NEDEN: Birden fazla adres varsa hangisinin
        /// otomatik seçileceğini belirler.
        /// </summary>
        [JsonPropertyName("adr_varsayilan")]
        public bool? AdrVarsayilan { get; set; }
    }

    /// <summary>
    /// Cari yetkili kişi bilgisi.
    /// </summary>
    public class MikroCariYetkiliDto
    {
        /// <summary>
        /// Yetkili adı soyadı.
        /// </summary>
        [JsonPropertyName("ytk_ad_soyad")]
        public string YtkAdSoyad { get; set; } = string.Empty;

        /// <summary>
        /// Unvan/Pozisyon.
        /// Örn: "Satın Alma Müdürü", "Muhasebe Sorumlusu"
        /// </summary>
        [JsonPropertyName("ytk_unvan")]
        public string? YtkUnvan { get; set; }

        /// <summary>
        /// E-posta.
        /// </summary>
        [JsonPropertyName("ytk_email")]
        public string? YtkEmail { get; set; }

        /// <summary>
        /// Cep telefonu.
        /// </summary>
        [JsonPropertyName("ytk_cep")]
        public string? YtkCep { get; set; }

        /// <summary>
        /// Dahili telefon.
        /// </summary>
        [JsonPropertyName("ytk_dahili")]
        public string? YtkDahili { get; set; }

        /// <summary>
        /// Ana yetkili mi?
        /// </summary>
        [JsonPropertyName("ytk_ana_yetkili")]
        public bool? YtkAnaYetkili { get; set; }
    }

    /// <summary>
    /// CariListesiV2 endpoint'i için istek DTO'su.
    /// NEDEN: Mikro'dan e-ticaret'e müşteri senkronizasyonu için.
    /// Özellikle mağazada kayıtlı müşteriler e-ticaret'e aktarılabilir.
    /// </summary>
    public class MikroCariListesiRequestDto
    {
        /// <summary>
        /// Cari kodu başlangıç filtresi.
        /// </summary>
        [JsonPropertyName("CariKodBaslangic")]
        public string? CariKodBaslangic { get; set; }

        /// <summary>
        /// Cari kodu bitiş filtresi.
        /// </summary>
        [JsonPropertyName("CariKodBitis")]
        public string? CariKodBitis { get; set; }

        /// <summary>
        /// Cari tipi filtresi.
        /// </summary>
        [JsonPropertyName("CariTipi")]
        public int? CariTipi { get; set; }

        /// <summary>
        /// Bölge kodu filtresi.
        /// NEDEN: Sadece belirli gruptaki müşterileri çekmek için.
        /// </summary>
        [JsonPropertyName("BolgeKodu")]
        public string? BolgeKodu { get; set; }

        /// <summary>
        /// Son değişiklik tarihinden sonraki kayıtlar.
        /// </summary>
        [JsonPropertyName("DegisiklikTarihiBaslangic")]
        public string? DegisiklikTarihiBaslangic { get; set; }

        /// <summary>
        /// Pasif müşterileri dahil et.
        /// </summary>
        [JsonPropertyName("PasifDahil")]
        public bool PasifDahil { get; set; } = false;

        /// <summary>
        /// Adresleri de getir.
        /// </summary>
        [JsonPropertyName("AdresDahil")]
        public bool AdresDahil { get; set; } = true;

        /// <summary>
        /// Yetkilileri de getir.
        /// </summary>
        [JsonPropertyName("YetkiliDahil")]
        public bool YetkiliDahil { get; set; } = false;

        /// <summary>
        /// Sayfa numarası.
        /// </summary>
        [JsonPropertyName("SayfaNo")]
        public int SayfaNo { get; set; } = 1;

        /// <summary>
        /// Sayfa boyutu.
        /// </summary>
        [JsonPropertyName("SayfaBuyuklugu")]
        public int SayfaBuyuklugu { get; set; } = 100;
    }

    /// <summary>
    /// CariListesiV2 endpoint'inden dönen cari verisi.
    /// </summary>
    public class MikroCariResponseDto
    {
        [JsonPropertyName("cari_kod")]
        public string CariKod { get; set; } = string.Empty;

        [JsonPropertyName("cari_unvan1")]
        public string CariUnvan1 { get; set; } = string.Empty;

        [JsonPropertyName("cari_unvan2")]
        public string? CariUnvan2 { get; set; }

        [JsonPropertyName("cari_hareket_tipi")]
        public int CariHareketTipi { get; set; }

        [JsonPropertyName("cari_bolge_kodu")]
        public string? CariBolgeKodu { get; set; }

        [JsonPropertyName("cari_EMail")]
        public string? CariEmail { get; set; }

        [JsonPropertyName("cari_CepTel")]
        public string? CariCepTel { get; set; }

        [JsonPropertyName("cari_Tel1")]
        public string? CariTel1 { get; set; }

        [JsonPropertyName("cari_vdaire_adi")]
        public string? CariVdaireAdi { get; set; }

        [JsonPropertyName("cari_vdaire_no")]
        public string? CariVdaireNo { get; set; }

        [JsonPropertyName("cari_tckno")]
        public string? CariTckNo { get; set; }

        [JsonPropertyName("cari_kisi_kurum_flg")]
        public int CariKisiKurumFlg { get; set; }

        [JsonPropertyName("cari_odeme_vade")]
        public int? CariOdemeVade { get; set; }

        [JsonPropertyName("cari_risk_limit")]
        public decimal? CariRiskLimit { get; set; }

        [JsonPropertyName("cari_fiyat_listeno")]
        public int? CariFiyatListeNo { get; set; }

        /// <summary>
        /// Cari bakiye (borç - alacak).
        /// NEDEN: Müşterinin toplam borç durumu.
        /// </summary>
        [JsonPropertyName("cari_bakiye")]
        public decimal? CariBakiye { get; set; }

        /// <summary>
        /// Toplam borç.
        /// </summary>
        [JsonPropertyName("cari_borc")]
        public decimal? CariBorc { get; set; }

        /// <summary>
        /// Toplam alacak.
        /// </summary>
        [JsonPropertyName("cari_alacak")]
        public decimal? CariAlacak { get; set; }

        [JsonPropertyName("cari_pasif_fl")]
        public bool CariPasifFl { get; set; }

        [JsonPropertyName("cari_ozel_kod")]
        public string? CariOzelKod { get; set; }

        [JsonPropertyName("cari_create_date")]
        public DateTime? CariCreateDate { get; set; }

        [JsonPropertyName("cari_lastup_date")]
        public DateTime? CariLastupDate { get; set; }

        /// <summary>
        /// Adresler.
        /// </summary>
        [JsonPropertyName("adresler")]
        public List<MikroCariAdresResponseDto>? Adresler { get; set; }

        /// <summary>
        /// Yetkililer.
        /// </summary>
        [JsonPropertyName("yetkililer")]
        public List<MikroCariYetkiliDto>? Yetkililer { get; set; }
    }

    /// <summary>
    /// Cari adres response DTO'su.
    /// </summary>
    public class MikroCariAdresResponseDto : MikroCariAdresDto
    {
        /// <summary>
        /// Adres ID (Mikro internal).
        /// </summary>
        [JsonPropertyName("adr_id")]
        public long? AdrId { get; set; }
    }

    /// <summary>
    /// Cari bakiye sorgulama response DTO'su.
    /// NEDEN: Sipariş öncesi müşterinin kredi durumu kontrol edilebilir.
    /// </summary>
    public class MikroCariBakiyeResponseDto
    {
        [JsonPropertyName("cari_kod")]
        public string CariKod { get; set; } = string.Empty;

        [JsonPropertyName("cari_unvan")]
        public string? CariUnvan { get; set; }

        /// <summary>
        /// Toplam borç.
        /// </summary>
        [JsonPropertyName("borc")]
        public decimal Borc { get; set; }

        /// <summary>
        /// Toplam alacak.
        /// </summary>
        [JsonPropertyName("alacak")]
        public decimal Alacak { get; set; }

        /// <summary>
        /// Net bakiye (borç - alacak).
        /// Pozitif = müşteri borçlu, Negatif = müşteriden alacaklıyız
        /// </summary>
        [JsonPropertyName("bakiye")]
        public decimal Bakiye { get; set; }

        /// <summary>
        /// Risk limiti.
        /// </summary>
        [JsonPropertyName("risk_limit")]
        public decimal? RiskLimit { get; set; }

        /// <summary>
        /// Kullanılabilir kredi (limit - bakiye).
        /// </summary>
        [JsonPropertyName("kullanilabilir_kredi")]
        public decimal? KullanilabilirKredi { get; set; }

        /// <summary>
        /// Vadesi geçmiş borç tutarı.
        /// NEDEN: Tahsilat takibi için önemli.
        /// </summary>
        [JsonPropertyName("vadesi_gecmis")]
        public decimal? VadesiGecmis { get; set; }
    }

    /// <summary>
    /// Cari kayıt sonucu.
    /// </summary>
    public class MikroCariKaydetResponseDto
    {
        /// <summary>
        /// Oluşturulan/güncellenen cari kodu.
        /// </summary>
        [JsonPropertyName("cari_kod")]
        public string CariKod { get; set; } = string.Empty;

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("basarili")]
        public bool Basarili { get; set; }

        /// <summary>
        /// Mesaj.
        /// </summary>
        [JsonPropertyName("mesaj")]
        public string? Mesaj { get; set; }

        /// <summary>
        /// Yeni kayıt mı güncelleme mi?
        /// true = Yeni oluşturuldu, false = Güncellendi
        /// </summary>
        [JsonPropertyName("yeni_kayit")]
        public bool YeniKayit { get; set; }
    }
}
