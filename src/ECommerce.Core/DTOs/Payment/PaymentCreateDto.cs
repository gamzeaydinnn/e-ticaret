using ECommerce.Core.DTOs;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Payment
{
    /// <summary>
    /// Ödeme başlatma DTO sınıfı
    /// Tüm ödeme sağlayıcıları için ortak alanlar + POSNET özel alanları içerir
    /// </summary>
    public class PaymentCreateDto
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // TEMEL ALANLAR (Tüm provider'lar için)
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Sipariş ID (zorunlu)</summary>
        public int OrderId { get; set; }
        
        /// <summary>Ödeme tutarı (zorunlu)</summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Ödeme yöntemi/sağlayıcı seçimi
        /// Değerler: "stripe", "paypal", "iyzico", "posnet", "yapikredi", "creditcard"
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>Para birimi (ISO 4217 kodu)</summary>
        public string Currency { get; set; } = "TRY";

        // ═══════════════════════════════════════════════════════════════════════════
        // POSNET / KREDİ KARTI ALANLARI
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Kredi kartı numarası (16 hane) - POSNET/Direct card payment için</summary>
        public string? CardNumber { get; set; }
        
        /// <summary>Kart son kullanma tarihi (MMYY veya MM/YY formatı)</summary>
        public string? ExpireDate { get; set; }
        
        /// <summary>Kart güvenlik kodu (CVV/CVC - 3 veya 4 hane)</summary>
        public string? Cvv { get; set; }
        
        /// <summary>Kart sahibinin adı soyadı</summary>
        public string? CardHolderName { get; set; }
        
        /// <summary>
        /// Taksit sayısı (0 = tek çekim)
        /// Geçerli değerler: 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
        /// </summary>
        public int InstallmentCount { get; set; } = 0;

        // ═══════════════════════════════════════════════════════════════════════════
        // 3D SECURE ALANLARI
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// 3D Secure kullanılsın mı?
        /// true: 3D Secure akışı başlatılır
        /// false: Direkt satış yapılır (2D)
        /// </summary>
        public bool Use3DSecure { get; set; } = true;
        
        /// <summary>3D Secure başarılı callback URL (opsiyonel - varsayılan config'den alınır)</summary>
        public string? SuccessUrl { get; set; }
        
        /// <summary>3D Secure başarısız callback URL (opsiyonel - varsayılan config'den alınır)</summary>
        public string? FailUrl { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // WORLD PUAN ALANLARI (POSNET Özel)
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>World puan kullanılsın mı?</summary>
        public bool UseWorldPoints { get; set; } = false;
        
        /// <summary>Kullanılacak world puan miktarı (boş ise tüm puan kullanılır)</summary>
        public int? WorldPointsToUse { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // MÜŞTERİ BİLGİLERİ (Opsiyonel - Raporlama/Fatura için)
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>Müşteri ID</summary>
        public int? UserId { get; set; }
        
        /// <summary>Müşteri email adresi</summary>
        public string? CustomerEmail { get; set; }
        
        /// <summary>Müşteri telefon numarası</summary>
        public string? CustomerPhone { get; set; }
        
        /// <summary>Müşteri IP adresi (fraud koruması için)</summary>
        public string? CustomerIp { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════
        // DOĞRULAMA METODLARI
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Kredi kartı bilgilerinin dolu olup olmadığını kontrol eder
        /// </summary>
        public bool HasCardInfo =>
            !string.IsNullOrWhiteSpace(CardNumber) &&
            !string.IsNullOrWhiteSpace(ExpireDate) &&
            !string.IsNullOrWhiteSpace(Cvv);

        /// <summary>
        /// POSNET/YapıKredi provider mı kontrol eder
        /// </summary>
        public bool IsPosnetProvider =>
            !string.IsNullOrWhiteSpace(PaymentMethod) &&
            (PaymentMethod.Equals("posnet", StringComparison.OrdinalIgnoreCase) ||
             PaymentMethod.Equals("yapikredi", StringComparison.OrdinalIgnoreCase) ||
             PaymentMethod.Equals("yapi_kredi", StringComparison.OrdinalIgnoreCase) ||
             PaymentMethod.Equals("yapı kredi", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Taksit sayısını normalize eder (geçersiz değerleri 0 yapar)
        /// </summary>
        public int GetNormalizedInstallment()
        {
            // Geçerli taksit değerleri: 0, 2-12
            if (InstallmentCount < 0 || InstallmentCount == 1 || InstallmentCount > 12)
                return 0;
            return InstallmentCount;
        }
    }
}
