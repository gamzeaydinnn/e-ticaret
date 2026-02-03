using System.Text.Json.Serialization;

namespace ECommerce.Core.DTOs.Micro
{
    /// <summary>
    /// MikroAPI V2 kimlik doğrulama wrapper'ı.
    /// 
    /// NEDEN: MikroAPI her istekte kimlik bilgilerini bekliyor.
    /// Bu class, tüm API isteklerinin temelini oluşturuyor.
    /// 
    /// ÖNEMLİ: Sifre alanı MD5 hash olarak gönderilmeli.
    /// Format: MD5("YYYY-MM-DD" + plain_password)
    /// Her gün saat 00:00'da yeni hash hesaplanmalı.
    /// </summary>
    public class MikroAuthDto
    {
        /// <summary>
        /// MikroAPI lisans anahtarı.
        /// appsettings.json'dan gelir, asla hardcode edilmemeli.
        /// </summary>
        [JsonPropertyName("ApiKey")]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Çalışma yılı (muhasebe dönemi).
        /// NEDEN: Mikro ERP yıl bazlı çalışıyor, farklı yılların
        /// verileri ayrı tutuluyor.
        /// </summary>
        [JsonPropertyName("CalismaYili")]
        public int CalismaYili { get; set; }

        /// <summary>
        /// Firma kodu - çoklu firma desteği için.
        /// NEDEN: Mikro ERP'de birden fazla firma tanımlı olabilir,
        /// her biri için ayrı veri seti var.
        /// </summary>
        [JsonPropertyName("FirmaKodu")]
        public int FirmaKodu { get; set; }

        /// <summary>
        /// Kullanıcı kodu - API erişim yetkisi olan kullanıcı.
        /// NEDEN: Loglama ve yetki kontrolü için kullanılır.
        /// </summary>
        [JsonPropertyName("KullaniciKodu")]
        public string KullaniciKodu { get; set; } = string.Empty;

        /// <summary>
        /// Günlük MD5 hash olarak şifre.
        /// NEDEN: Güvenlik için plain-text şifre gönderilmiyor,
        /// günlük değişen hash sayesinde eski istekler tekrar kullanılamaz.
        /// </summary>
        [JsonPropertyName("Sifre")]
        public string Sifre { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generic MikroAPI istek wrapper'ı.
    /// 
    /// NEDEN: MikroAPI V2'nin tüm endpointleri aynı yapıyı bekliyor:
    /// { "Mikro": { auth bilgileri + data } }
    /// 
    /// Bu generic class ile her endpoint için tip güvenli
    /// istek oluşturabiliyoruz.
    /// </summary>
    /// <typeparam name="TData">İstek verisinin tipi (stok, sipariş, cari vb.)</typeparam>
    public class MikroRequestWrapper<TData> where TData : class
    {
        /// <summary>
        /// Tüm MikroAPI istekleri "Mikro" anahtarı altında olmalı.
        /// NEDEN: API'nin beklediği format bu şekilde.
        /// </summary>
        [JsonPropertyName("Mikro")]
        public MikroRequestContent<TData> Mikro { get; set; } = new();
    }

    /// <summary>
    /// MikroAPI istek içeriği - auth + data birleşimi.
    /// 
    /// NEDEN: Her istekte hem kimlik doğrulama hem de 
    /// işlem verisi bir arada gönderiliyor.
    /// </summary>
    /// <typeparam name="TData">İşlem verisinin tipi</typeparam>
    public class MikroRequestContent<TData> : MikroAuthDto where TData : class
    {
        /// <summary>
        /// İşleme özel veri.
        /// Null olabilir çünkü bazı endpointler (örn: StokListesi)
        /// sadece auth bilgisi ile çalışıyor.
        /// </summary>
        [JsonPropertyName("Data")]
        public TData? Data { get; set; }
    }

    /// <summary>
    /// Sadece auth gerektiren istekler için basit wrapper.
    /// Örnek: StokListesiV2, CariListesiV2 gibi GET benzeri istekler.
    /// </summary>
    public class MikroSimpleRequest
    {
        [JsonPropertyName("Mikro")]
        public MikroAuthDto Mikro { get; set; } = new();
    }

    /// <summary>
    /// Generic MikroAPI response wrapper'ı.
    /// 
    /// NEDEN: Tüm MikroAPI yanıtları aynı yapıda geliyor:
    /// { "success": true/false, "message": "...", "data": [...] }
    /// </summary>
    /// <typeparam name="TData">Yanıt verisinin tipi</typeparam>
    public class MikroResponseWrapper<TData>
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Hata veya bilgi mesajı.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// İşlem sonucu veriler.
        /// Liste olarak geliyor çünkü çoğu endpoint çoklu kayıt döndürüyor.
        /// </summary>
        [JsonPropertyName("data")]
        public List<TData> Data { get; set; } = new();

        /// <summary>
        /// Toplam kayıt sayısı (sayfalama için).
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        /// <summary>
        /// Sunucu tarafında geçen süre (ms).
        /// NEDEN: Performans analizi için kullanılır.
        /// </summary>
        [JsonPropertyName("elapsedMs")]
        public long? ElapsedMs { get; set; }
    }

    /// <summary>
    /// Tek kayıt döndüren işlemler için response wrapper.
    /// Örnek: Sipariş kaydetme sonucu.
    /// </summary>
    /// <typeparam name="TData">Yanıt verisinin tipi</typeparam>
    public class MikroSingleResponseWrapper<TData>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Tek kayıt verisi.
        /// </summary>
        [JsonPropertyName("data")]
        public TData? Data { get; set; }

        /// <summary>
        /// Oluşturulan/güncellenen kaydın ID'si.
        /// NEDEN: Sipariş/stok kaydedildikten sonra
        /// e-ticaret tarafında bu ID ile eşleştirme yapılıyor.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
