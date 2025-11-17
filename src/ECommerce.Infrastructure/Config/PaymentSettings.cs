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
    }
}

//Bu class, ödeme servislerinin yapılandırma ayarlarını tutar.
//Iyzico, PayPal, Stripe gibi farklı ödeme sağlayıcılarının API anahtarları, gizli anahtarları ve temel URL'leri burada saklanır.
//Uygulama başlatılırken bu ayarlar konfigürasyon dosyasından (appsettings.json gibi) okunur ve ilgili ödeme servislerine enjekte edilir.
//Bu sayede ödeme servisleri, gerekli kimlik doğrulama bilgilerine ve bağlantı ayarlarına kolayca erişebilir.
//Yeni bir ödeme sağlayıcı eklenirse, ilgili ayarlar bu sınıfa eklenebilir
