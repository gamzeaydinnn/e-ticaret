namespace ECommerce.Infrastructure.Config
{
    /// <summary>
    /// MikroAPI entegrasyon ayarları.
    /// Bu sınıf, Mikro ERP sistemi ile iletişim için gerekli tüm konfigürasyon
    /// parametrelerini içerir. MikroAPI V2 endpoint'leri için tasarlanmıştır.
    /// </summary>
    /// <remarks>
    /// ÖNEMLİ: Sifre alanı plain text olarak tutulur, MD5 hash runtime'da
    /// "YYYY-MM-DD + Sifre" formatında hesaplanır (MikroAPI gerekliliği).
    /// </remarks>
    public class MikroSettings
    {
        // ==================== BAĞLANTI AYARLARI ====================
        
        /// <summary>
        /// MikroAPI servisinin temel URL'i.
        /// Örnek: "https://api.mikrofly.com" veya "https://localhost:8094"
        /// </summary>
        public string ApiUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// MikroAPI tarafından verilen API anahtarı.
        /// Bu anahtar her istekte "ApiKey" alanında gönderilir.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        
        /// <summary>
        /// Eski HMAC imzalama için kullanılan secret (artık MikroAPI V2'de kullanılmıyor).
        /// Geriye dönük uyumluluk için korunuyor.
        /// </summary>
        public string ApiSecret { get; set; } = string.Empty;
        
        // ==================== FİRMA BİLGİLERİ ====================
        
        /// <summary>
        /// Mikro ERP'deki firma kodu.
        /// Örnek: "FIRMAADI", "MIKROFLY"
        /// </summary>
        public string FirmaKodu { get; set; } = string.Empty;
        
        /// <summary>
        /// API için tanımlanmış kullanıcı kodu.
        /// Genellikle "SRV" (servis kullanıcısı) olarak ayarlanır.
        /// </summary>
        public string KullaniciKodu { get; set; } = "SRV";
        
        /// <summary>
        /// API kullanıcısının şifresi (PLAIN TEXT).
        /// UYARI: Bu değer runtime'da "YYYY-MM-DD + Sifre" formatında MD5 hash'e dönüştürülür.
        /// Örnek: Şifre="123asd" ise, bugün için "2026-02-03123asd" → MD5
        /// </summary>
        public string Sifre { get; set; } = string.Empty;
        
        /// <summary>
        /// Çalışma yılı (mali yıl).
        /// Örnek: "2026"
        /// </summary>
        public string CalismaYili { get; set; } = DateTime.Now.Year.ToString();
        
        // ==================== VARSAYILAN DEĞERLER ====================
        
        /// <summary>
        /// Varsayılan depo numarası.
        /// Online siparişler bu depodan düşülür.
        /// </summary>
        public int DefaultDepoNo { get; set; } = 1;
        
        /// <summary>
        /// Varsayılan şube numarası.
        /// </summary>
        public int DefaultSubeNo { get; set; } = 0;
        
        /// <summary>
        /// Varsayılan firma numarası.
        /// </summary>
        public int DefaultFirmaNo { get; set; } = 0;
        
        /// <summary>
        /// Sipariş evrak seri numarası.
        /// Örnek: "E" (E-Ticaret), "W" (Web)
        /// </summary>
        public string DefaultEvrakSeri { get; set; } = "E";
        
        // ==================== SENKRONIZASYON AYARLARI ====================
        
        /// <summary>
        /// Stok senkronizasyonu aralığı (dakika).
        /// Varsayılan: 15 dakika
        /// </summary>
        public int StokSyncIntervalMinutes { get; set; } = 15;
        
        /// <summary>
        /// Fiyat senkronizasyonu aralığı (dakika).
        /// Varsayılan: 60 dakika (1 saat)
        /// </summary>
        public int FiyatSyncIntervalMinutes { get; set; } = 60;
        
        /// <summary>
        /// Başarısız istekler için maksimum yeniden deneme sayısı.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// İstek timeout süresi (saniye).
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// SSL sertifika doğrulamasını atla.
        /// Self-signed sertifika kullanan sunucularda true yapın.
        /// ⚠️ Production'da dikkatli kullanın!
        /// </summary>
        public bool IgnoreSslErrors { get; set; } = true;
        
        // ==================== MÜŞTERİ KODU AYARLARI ====================
        
        /// <summary>
        /// Online müşteriler için Mikro'da oluşturulacak cari kodun prefix'i.
        /// Örnek: "WEB-" → "WEB-12345"
        /// </summary>
        public string OnlineMusteriPrefix { get; set; } = "WEB-";
    }
}

