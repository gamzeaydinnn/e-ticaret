using System;

namespace ECommerce.Core.DTOs.Payment
{
    /// <summary>
    /// Ödeme başlatma sonucu
    /// Hosted sayfa yönlendirmesi, 3D Secure veya Stripe client secret bilgilerini taşır
    /// </summary>
    public class PaymentInitResult
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // TEMEL ALANLAR (Tüm provider'lar için)
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>İşlem başarılı mı?</summary>
        public bool Success { get; set; }
        
        /// <summary>Hata mesajı (başarısız durumlarda)</summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>Hata mesajı (alias)</summary>
        public string? Error
        {
            get => ErrorMessage;
            set => ErrorMessage = value;
        }
        
        /// <summary>Hata kodu (provider-specific)</summary>
        public string? ErrorCode { get; set; }
        
        /// <summary>Token (legacy)</summary>
        public string? Token { get; set; }
        
        /// <summary>Checkout URL (legacy)</summary>
        public string? CheckoutUrl { get; set; }
        
        /// <summary>Provider adı: stripe | iyzico | paypal | paytr | posnet</summary>
        public string Provider { get; set; } = string.Empty;
        
        /// <summary>Redirect gerekli mi?</summary>
        public bool RequiresRedirect { get; set; }
        
        /// <summary>Yönlendirme URL'i (3D Secure, Hosted Checkout)</summary>
        public string? RedirectUrl { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // STRIPE ÖZEL ALANLAR
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Stripe Checkout Session ID</summary>
        public string? CheckoutSessionId { get; set; }
        
        /// <summary>Stripe Client Secret</summary>
        public string? ClientSecret { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // IYZICO ÖZEL ALANLAR
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Provider tarafından verilen ödeme ID'si</summary>
        public string? ProviderPaymentId { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // POSNET ÖZEL ALANLAR
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Genel Payment ID (tüm provider'lar için)</summary>
        public string? PaymentId { get; set; }
        
        /// <summary>Transaction ID (banka tarafından verilen)</summary>
        public string? TransactionId { get; set; }
        
        /// <summary>Host Log Key (POSNET - iptal/iade için gerekli)</summary>
        public string? HostLogKey { get; set; }
        
        /// <summary>Onay Kodu (POSNET)</summary>
        public string? AuthCode { get; set; }
        
        /// <summary>3D Secure kullanıldı mı?</summary>
        public bool Is3DSecure { get; set; }
        
        /// <summary>3D Secure form HTML'i (inline form için)</summary>
        public string? ThreeDSecureHtml { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // SİPARİŞ BİLGİLERİ
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Para birimi</summary>
        public string Currency { get; set; } = "TRY";
        
        /// <summary>Ödeme tutarı</summary>
        public decimal Amount { get; set; }
        
        /// <summary>Sipariş ID</summary>
        public int OrderId { get; set; }
    }
}

