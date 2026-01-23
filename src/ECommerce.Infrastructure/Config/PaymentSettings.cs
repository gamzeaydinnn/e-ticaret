using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Config
{
    public class PaymentSettings
    {
        // Iyzico ayarları
        public string IyzicoApiKey { get; set; } = string.Empty;
        public string IyzicoSecretKey { get; set; } = string.Empty;
        public string IyzicoBaseUrl { get; set; } = string.Empty;
        public string? IyzicoCallbackUrl { get; set; }

        // PayPal ayarları
        public string PayPalClientId { get; set; } = string.Empty;
        public string PayPalSecret { get; set; } = string.Empty;
        public string PayPalBaseUrl { get; set; } = string.Empty;

        // Stripe ayarları
        public string StripeSecretKey { get; set; } = string.Empty;
        public string StripePublishableKey { get; set; } = string.Empty;
        public string? StripeWebhookSecret { get; set; }

        // Ortak dönüş URL'leri
        public string? ReturnUrlSuccess { get; set; }
        public string? ReturnUrlCancel { get; set; }

        // PayTR ayarları (gerekirse production/sandbox anahtarları burada tutulur)
        public string PayTRMerchantId { get; set; } = string.Empty;
        public string PayTRSecretKey { get; set; } = string.Empty;
        public string? PayTRCallbackUrl { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // YAPI KREDİ POSNET AYARLARI
        // Yapı Kredi Bankası POSNET XML Servisleri Entegrasyonu (Versiyon 2.1.1.3)
        // Dokümantasyon: https://setmpos.ykb.com/ (Test) | https://posnet.yapikredi.com.tr/ (Canlı)
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Üye işyeri numarası - 10 haneli (Örnek: 6700000001)
        /// Banka tarafından sağlanan merchant kimlik numarası
        /// </summary>
        public string PosnetMerchantId { get; set; } = string.Empty;

        /// <summary>
        /// Terminal numarası - 8 haneli (Örnek: 67000001)
        /// İşyerine ait sanal POS terminal numarası
        /// </summary>
        public string PosnetTerminalId { get; set; } = string.Empty;

        /// <summary>
        /// POSNET numarası - 16 haneye kadar (Örnek: 9644)
        /// 3D Secure işlemleri için zorunlu şifreleme parametresi
        /// </summary>
        public string PosnetId { get; set; } = string.Empty;

        /// <summary>
        /// 3D Secure şifreleme anahtarı (Enckey)
        /// MAC (Message Authentication Code) doğrulaması için kullanılır
        /// Banka tarafından sağlanır, gizli tutulmalıdır
        /// </summary>
        public string PosnetEncKey { get; set; } = string.Empty;

        /// <summary>
        /// POSNET XML API Servis URL'i
        /// Test: https://setmpos.ykb.com/PosnetWebService/XML
        /// Canlı: https://posnet.yapikredi.com.tr/PosnetWebService/XML
        /// </summary>
        public string PosnetXmlServiceUrl { get; set; } = "https://setmpos.ykb.com/PosnetWebService/XML";

        /// <summary>
        /// 3D Secure OOS (On-us/Off-us) Servis URL'i
        /// Test: https://setmpos.ykb.com/3DSWebService/YKBPaymentService
        /// Canlı: https://posnet.yapikredi.com.tr/3DSWebService/YKBPaymentService
        /// </summary>
        public string Posnet3DServiceUrl { get; set; } = "https://setmpos.ykb.com/3DSWebService/YKBPaymentService";

        /// <summary>
        /// 3D Secure callback URL - Banka ödeme sonrası bu adrese POST yapar
        /// Örnek: https://yourdomain.com/api/payments/posnet/3d-callback
        /// Statik IP olarak banka tarafına bildirilmelidir
        /// </summary>
        public string? PosnetCallbackUrl { get; set; }

        /// <summary>
        /// POSNET ortamı: Test (sandbox) veya Production (canlı)
        /// true = Test ortamı, false = Canlı ortam
        /// </summary>
        public bool PosnetIsTestEnvironment { get; set; } = true;

        /// <summary>
        /// Varsayılan timeout süresi (saniye)
        /// POSNET bağlantı ve okuma timeout değeri
        /// </summary>
        public int PosnetTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// World Puan entegrasyonu aktif mi?
        /// true = Puan sorgulama ve kullanım aktif
        /// </summary>
        public bool PosnetWorldPointEnabled { get; set; } = false;

        /// <summary>
        /// Joker Vadaa (kişiye özel kampanyalar) aktif mi?
        /// true = Joker Vadaa sorgulama aktif
        /// </summary>
        public bool PosnetJokerVadaaEnabled { get; set; } = false;

        /// <summary>
        /// VFT (Vade Farklı İşlemler) aktif mi?
        /// true = VFT işlemleri aktif
        /// </summary>
        public bool PosnetVftEnabled { get; set; } = false;
    }
}

//Bu class, ödeme servislerinin yapılandırma ayarlarını tutar.
//Iyzico, PayPal, Stripe gibi farklı ödeme sağlayıcılarının API anahtarları, gizli anahtarları ve temel URL'leri burada saklanır.
//Uygulama başlatılırken bu ayarlar konfigürasyon dosyasından (appsettings.json gibi) okunur ve ilgili ödeme servislerine enjekte edilir.
//Bu sayede ödeme servisleri, gerekli kimlik doğrulama bilgilerine ve bağlantı ayarlarına kolayca erişebilir.
//Yeni bir ödeme sağlayıcı eklenirse, ilgili ayarlar bu sınıfa eklenebilir
