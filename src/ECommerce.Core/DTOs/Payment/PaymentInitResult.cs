using System;

namespace ECommerce.Core.DTOs.Payment
{
    // Ödeme başlatma sonucu: hosted sayfa yönlendirmesi veya Stripe client secret gibi bilgiler taşır
    public class PaymentInitResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Token { get; set; }
        public string? CheckoutUrl { get; set; }
        
        public string Provider { get; set; } = string.Empty; // stripe | iyzico | paypal | paytr
        public bool RequiresRedirect { get; set; }
        public string? RedirectUrl { get; set; } // Stripe Checkout URL / Iyzico form URL

        // Stripe özel alanlar
        public string? CheckoutSessionId { get; set; }
        public string? ClientSecret { get; set; }

        // Iyzico özel alanlar
        public string? ProviderPaymentId { get; set; } // token / paymentId

        public string Currency { get; set; } = "TRY";
        public decimal Amount { get; set; }
        public int OrderId { get; set; }
    }
}

