using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 StokKaydetV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: Yeni ürün ekleme veya mevcut ürün güncelleme için kullanılır.
    /// E-ticaret'ten Mikro'ya ürün senkronizasyonu bu DTO ile yapılır.
    /// 
    /// ÖNEMLİ: Alan adları Türkçe çünkü MikroAPI bu isimleri bekliyor.
    /// JsonPropertyName attribute'ları ile .NET naming convention'a uyum sağlanıyor.
    /// </summary>
    public class MikroStokKaydetRequestDto
    {
        /// <summary>
        /// Stok kodu - ürünün benzersiz tanımlayıcısı.
        /// NEDEN: Mikro ERP'de her ürün bu kodla aranıyor,
        /// e-ticaret SKU değeri buraya yazılmalı.
        /// Format: Max 25 karakter, boşluk içeremez.
        /// </summary>
        [JsonPropertyName("sto_kod")]
        public string StoKod { get; set; } = string.Empty;

        /// <summary>
        /// Ürün adı - ana ürün ismi.
        /// NEDEN: Müşteriye gösterilen ve faturada çıkan isim.
        /// Format: Max 100 karakter.
        /// </summary>
        [JsonPropertyName("sto_isim")]
        public string StoIsim { get; set; } = string.Empty;

        /// <summary>
        /// Birim adı (Adet, Kg, Lt, Kutu vb.).
        /// NEDEN: Stok takibi ve satış işlemlerinde miktar hesabı için.
        /// Varsayılan: "ADET"
        /// </summary>
        [JsonPropertyName("sto_birim1_ad")]
        public string StoBirim1Ad { get; set; } = "ADET";

        /// <summary>
        /// Ürün cinsi/türü.
        /// NEDEN: Raporlama ve filtreleme için kategorik bilgi.
        /// Örnek: Ticari Mal, Hammadde, Yarı Mamul, Hizmet
        /// Varsayılan: 0 = Ticari Mal
        /// </summary>
        [JsonPropertyName("sto_cins")]
        public int StoCins { get; set; } = 0;

        /// <summary>
        /// Ürün grubu kodu.
        /// NEDEN: Mikro ERP'deki kategori yapısıyla eşleşme için.
        /// E-ticaret kategorileri bu kodlara map edilmeli.
        /// </summary>
        [JsonPropertyName("sto_anagrup_kod")]
        public string? StoAnagrupKod { get; set; }

        /// <summary>
        /// Alt grup kodu.
        /// NEDEN: Daha detaylı kategorilendirme için.
        /// </summary>
        [JsonPropertyName("sto_altgrup_kod")]
        public string? StoAltgrupKod { get; set; }

        /// <summary>
        /// KDV oranı (%).
        /// NEDEN: Fatura kesiminde vergi hesabı için kritik.
        /// Türkiye'de yaygın oranlar: 1, 10, 20
        /// </summary>
        [JsonPropertyName("sto_toptan_vergi")]
        public decimal StoToptanVergi { get; set; } = 20;

        /// <summary>
        /// Perakende satış KDV oranı.
        /// NEDEN: Toptan ve perakende farklı vergi oranına tabi olabilir.
        /// </summary>
        [JsonPropertyName("sto_perakende_vergi")]
        public decimal StoPerakendeVergi { get; set; } = 20;

        /// <summary>
        /// Minimum stok seviyesi.
        /// NEDEN: Bu seviyenin altına düşünce uyarı üretmek için.
        /// </summary>
        [JsonPropertyName("sto_min_miktar")]
        public decimal? StoMinMiktar { get; set; }

        /// <summary>
        /// Maximum stok seviyesi.
        /// NEDEN: Fazla stok maliyetini önlemek için üst limit.
        /// </summary>
        [JsonPropertyName("sto_max_miktar")]
        public decimal? StoMaxMiktar { get; set; }

        /// <summary>
        /// Ürün açıklaması.
        /// NEDEN: E-ticaret'teki ürün açıklaması buraya yazılabilir.
        /// </summary>
        [JsonPropertyName("sto_kisa_ismi")]
        public string? StoKisaIsmi { get; set; }

        /// <summary>
        /// Ürün menşei/üretim yeri.
        /// NEDEN: Gümrük ve ithalat işlemleri için gerekli.
        /// </summary>
        [JsonPropertyName("sto_uretici_kod")]
        public string? StoUreticiKod { get; set; }

        /// <summary>
        /// Barkod listesi.
        /// NEDEN: Bir ürünün birden fazla barkodu olabilir
        /// (farklı ambalaj boyutları, eski/yeni barkod vb.).
        /// </summary>
        [JsonPropertyName("barkodlar")]
        public List<MikroStokBarkodDto> Barkodlar { get; set; } = new();

        /// <summary>
        /// Satış fiyatları listesi.
        /// NEDEN: Mikro ERP'de 10'a kadar farklı fiyat tanımlanabiliyor
        /// (perakende, toptan, bayi, kampanyalı vb.).
        /// </summary>
        [JsonPropertyName("satis_fiyatlari")]
        public List<MikroStokFiyatDto> SatisFiyatlari { get; set; } = new();

        /// <summary>
        /// Alış fiyatları listesi.
        /// NEDEN: Maliyet hesabı ve kar marjı analizi için.
        /// </summary>
        [JsonPropertyName("alis_fiyatlari")]
        public List<MikroStokFiyatDto>? AlisFiyatlari { get; set; }

        /// <summary>
        /// Resim URL'leri.
        /// NEDEN: Mikro V2 artık ürün resimlerini de destekliyor.
        /// E-ticaret'teki resim URL'leri buraya aktarılabilir.
        /// </summary>
        [JsonPropertyName("resimler")]
        public List<MikroStokResimDto>? Resimler { get; set; }
    }

    /// <summary>
    /// Stok barkod bilgisi.
    /// NEDEN: Kasada ve depoda ürün tanıma için barkod kritik.
    /// </summary>
    public class MikroStokBarkodDto
    {
        /// <summary>
        /// Barkod numarası.
        /// Format: EAN-13, UPC-A veya şirket içi kod olabilir.
        /// </summary>
        [JsonPropertyName("bar_barkodno")]
        public string BarBarkodNo { get; set; } = string.Empty;

        /// <summary>
        /// Bu barkod için çarpan (miktar katsayısı).
        /// NEDEN: Kolide 12 adet varsa burada 12 yazılır,
        /// koli barkodu okutulduğunda 12 adet sayılır.
        /// </summary>
        [JsonPropertyName("bar_carpan")]
        public decimal BarCarpan { get; set; } = 1;

        /// <summary>
        /// Barkod tipi.
        /// NEDEN: Ana barkod ve alternatif barkod ayrımı için.
        /// 0 = Ana barkod, 1 = Alternatif
        /// </summary>
        [JsonPropertyName("bar_birimi")]
        public int BarBirimi { get; set; } = 0;
    }

    /// <summary>
    /// Stok fiyat bilgisi.
    /// NEDEN: Mikro ERP çoklu fiyat listesi destekliyor,
    /// her müşteri grubu için farklı fiyat olabilir.
    /// </summary>
    public class MikroStokFiyatDto
    {
        /// <summary>
        /// Fiyat numarası (1-10 arası).
        /// NEDEN: Mikro'da 10 farklı fiyat tipi var:
        /// 1 = Perakende, 2 = Toptan, 3 = Bayi vb.
        /// E-ticaret için genelde 1 (Perakende) kullanılır.
        /// </summary>
        [JsonPropertyName("sfiyat_no")]
        public int SfiyatNo { get; set; } = 1;

        /// <summary>
        /// Fiyat değeri (KDV hariç).
        /// NEDEN: Mikro'da fiyatlar genelde KDV hariç tutulur,
        /// satış anında KDV eklenir.
        /// </summary>
        [JsonPropertyName("sfiyat_fiyati")]
        public decimal SfiyatFiyati { get; set; }

        /// <summary>
        /// Para birimi kodu.
        /// NEDEN: Dövizli fiyat listesi desteği için.
        /// 0 = TL, 1 = USD, 2 = EUR vb.
        /// </summary>
        [JsonPropertyName("sfiyat_doviz_cinsi")]
        public int SfiyatDovizCinsi { get; set; } = 0;
    }

    /// <summary>
    /// Stok resim bilgisi.
    /// NEDEN: E-ticaret'teki ürün resimleri Mikro'ya aktarılabilir.
    /// </summary>
    public class MikroStokResimDto
    {
        /// <summary>
        /// Resim URL'si veya base64 encoded data.
        /// NEDEN: Mikro V2 API hem URL hem base64 kabul ediyor.
        /// </summary>
        [JsonPropertyName("resim_url")]
        public string ResimUrl { get; set; } = string.Empty;

        /// <summary>
        /// Resim sırası.
        /// NEDEN: Birden fazla resim varsa hangi sırada gösterileceği.
        /// 1 = Ana resim
        /// </summary>
        [JsonPropertyName("resim_sira")]
        public int ResimSira { get; set; } = 1;
    }

    /// <summary>
    /// StokListesiV2 endpoint'i için istek DTO'su.
    /// 
    /// NEDEN: Mikro'dan e-ticaret'e stok çekme işlemi için.
    /// Filtreleme parametreleri ile sadece değişen ürünler çekilebilir.
    /// 
    /// API DOKÜMANTASYONU BAZLI PARAMETRELER:
    /// - IlkTarih: Başlangıç tarihi filtresi
    /// - SonTarih: Bitiş tarihi filtresi
    /// - Index: Sayfa numarası (0-based)
    /// - Size: Sayfa boyutu (string olarak gönderilmeli)
    /// - Sort: Sıralama alanı (örn: "-sto_kod" veya "sto_kod")
    /// - StokKod: Stok kodu filtresi
    /// - TarihTipi: 1=Kayıt Tarihi, 2=Değişiklik Tarihi
    /// </summary>
    public class MikroStokListesiRequestDto
    {
        // ==================== MİKRO API V2 PARAMETRELERİ ====================
        // Bu parametreler Mikro API dokümantasyonundaki isimlere göre düzenlendi
        
        /// <summary>
        /// Başlangıç tarihi filtresi.
        /// Format: "yyyy-MM-dd" (örn: "2025-01-01")
        /// </summary>
        [JsonPropertyName("IlkTarih")]
        public string? IlkTarih { get; set; }

        /// <summary>
        /// Bitiş tarihi filtresi.
        /// Format: "yyyy-MM-dd" (örn: "2025-12-31")
        /// </summary>
        [JsonPropertyName("SonTarih")]
        public string? SonTarih { get; set; }

        /// <summary>
        /// Sayfa numarası (0-based index).
        /// NEDEN: Büyük veri setlerinde sayfalama için.
        /// </summary>
        [JsonPropertyName("Index")]
        public int Index { get; set; } = 0;

        /// <summary>
        /// Sayfa başına kayıt sayısı (string olarak gönderilmeli).
        /// NEDEN: MikroAPI bu alanı string bekliyor.
        /// Önerilen: 100-500 arası.
        /// </summary>
        [JsonPropertyName("Size")]
        public string Size { get; set; } = "100";

        /// <summary>
        /// Sıralama alanı ve yönü.
        /// Format: "alan_adi" (artan) veya "-alan_adi" (azalan)
        /// Örnek: "-sto_kod" (stok koduna göre azalan)
        /// </summary>
        [JsonPropertyName("Sort")]
        public string Sort { get; set; } = "sto_kod";

        /// <summary>
        /// Stok kodu filtresi.
        /// Boş bırakılırsa tüm stoklar gelir.
        /// </summary>
        [JsonPropertyName("StokKod")]
        public string StokKod { get; set; } = string.Empty;

        /// <summary>
        /// Tarih tipi filtresi.
        /// 1 = Kayıt Tarihi
        /// 2 = Değişiklik Tarihi (delta sync için)
        /// </summary>
        [JsonPropertyName("TarihTipi")]
        public int TarihTipi { get; set; } = 2;

        // ==================== EK FİLTRELER (Opsiyonel) ====================
        // Bu parametreler API'de destekleniyorsa kullanılır

        /// <summary>
        /// Depo numarası filtresi.
        /// NEDEN: Çoklu depo yapısında sadece ilgili deponun
        /// stoğu çekilmeli (online satış deposu gibi).
        /// </summary>
        [JsonPropertyName("DepoNo")]
        public int? DepoNo { get; set; }

        /// <summary>
        /// Grup kodu filtresi.
        /// NEDEN: Sadece belirli kategorideki ürünleri çekmek için.
        /// </summary>
        [JsonPropertyName("GrupKodu")]
        public string? GrupKodu { get; set; }

        /// <summary>
        /// Pasif ürünleri dahil et.
        /// NEDEN: Silinen/pasife alınan ürünleri de görmek için.
        /// </summary>
        [JsonPropertyName("PasifDahil")]
        public bool PasifDahil { get; set; } = false;

        /// <summary>
        /// Stok miktarı sıfırdan büyük olanları getir.
        /// NEDEN: E-ticaret'te sadece stokta olan ürünleri
        /// göstermek istiyorsak bu filtre kullanılır.
        /// </summary>
        [JsonPropertyName("StokluOlanlar")]
        public bool? StokluOlanlar { get; set; }

        /// <summary>
        /// Fiyatları da getir.
        /// NEDEN: Tek istekte hem stok hem fiyat bilgisi
        /// alınarak API çağrısı sayısı azaltılır.
        /// </summary>
        [JsonPropertyName("FiyatDahil")]
        public bool FiyatDahil { get; set; } = true;

        /// <summary>
        /// Barkodları da getir.
        /// </summary>
        [JsonPropertyName("BarkodDahil")]
        public bool BarkodDahil { get; set; } = true;

        /// <summary>
        /// Resimleri de getir.
        /// </summary>
        [JsonPropertyName("ResimDahil")]
        public bool ResimDahil { get; set; } = false;

        // ==================== HELPER PROPERTIES ====================
        
        /// <summary>
        /// Sayfa numarası (1-based, kullanım kolaylığı için).
        /// Bu property Index'i otomatik ayarlar.
        /// </summary>
        [JsonIgnore]
        public int SayfaNo
        {
            get => Index + 1;
            set => Index = Math.Max(0, value - 1);
        }

        /// <summary>
        /// Sayfa boyutu (int olarak).
        /// Bu property Size'ı otomatik ayarlar.
        /// </summary>
        [JsonIgnore]
        public int SayfaBuyuklugu
        {
            get => int.TryParse(Size, out var s) ? s : 100;
            set => Size = value.ToString();
        }
    }
}
