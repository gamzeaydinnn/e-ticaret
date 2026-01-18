// =============================================================================
// Kupon Doğrulama Sonuç DTO'ları - E-Ticaret Kupon Sistemi
// =============================================================================
// Bu DTO'lar kupon doğrulama işlemlerinin sonuçlarını temsil eder.
// Frontend'e detaylı bilgi döndürmek ve iş mantığını kapsüllemek için kullanılır.
// =============================================================================

using System;
using System.Collections.Generic;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Coupon
{
    /// <summary>
    /// Kupon doğrulama sonucunu temsil eden DTO.
    /// Başarılı/başarısız durumu, hata mesajı ve indirim detaylarını içerir.
    /// </summary>
    public class CouponValidationResult
    {
        // =============================================================================
        // Doğrulama Durumu
        // =============================================================================

        /// <summary>
        /// Kupon geçerli mi?
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Hata kodu (başarısız durumlarda)
        /// Frontend'de i18n için kullanılabilir
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Kullanıcı dostu hata mesajı
        /// </summary>
        public string? ErrorMessage { get; set; }

        // =============================================================================
        // Kupon Bilgileri
        // =============================================================================

        /// <summary>
        /// Kupon ID'si
        /// </summary>
        public int? CouponId { get; set; }

        /// <summary>
        /// Kupon kodu
        /// </summary>
        public string? CouponCode { get; set; }

        /// <summary>
        /// Kupon başlığı/açıklaması
        /// </summary>
        public string? CouponTitle { get; set; }

        /// <summary>
        /// Kupon türü
        /// </summary>
        public CouponType? CouponType { get; set; }

        /// <summary>
        /// İndirim değeri (yüzde veya sabit tutar)
        /// </summary>
        public decimal? DiscountValue { get; set; }

        /// <summary>
        /// Yüzde bazlı indirim mi?
        /// </summary>
        public bool? IsPercentage { get; set; }

        // =============================================================================
        // Hesaplanan İndirim
        // =============================================================================

        /// <summary>
        /// Hesaplanan indirim tutarı (TL)
        /// </summary>
        public decimal CalculatedDiscount { get; set; }

        /// <summary>
        /// İndirim öncesi toplam
        /// </summary>
        public decimal OriginalTotal { get; set; }

        /// <summary>
        /// İndirim sonrası toplam
        /// </summary>
        public decimal FinalTotal { get; set; }

        /// <summary>
        /// Ücretsiz kargo uygulandı mı?
        /// </summary>
        public bool FreeShippingApplied { get; set; }

        // =============================================================================
        // Ek Bilgiler
        // =============================================================================

        /// <summary>
        /// Kuponun kalan kullanım hakkı
        /// </summary>
        public int? RemainingUsage { get; set; }

        /// <summary>
        /// Kuponun son kullanma tarihi
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Minimum sipariş tutarı (karşılanmadıysa uyarı için)
        /// </summary>
        public decimal? MinOrderAmount { get; set; }

        // =============================================================================
        // Statik Factory Metodları - Clean Code için
        // =============================================================================

        /// <summary>
        /// Başarılı doğrulama sonucu oluşturur
        /// </summary>
        public static CouponValidationResult Success(
            int couponId,
            string couponCode,
            string? couponTitle,
            CouponType couponType,
            decimal discountValue,
            bool isPercentage,
            decimal calculatedDiscount,
            decimal originalTotal,
            decimal finalTotal,
            bool freeShippingApplied = false,
            int? remainingUsage = null,
            DateTime? expirationDate = null)
        {
            return new CouponValidationResult
            {
                IsValid = true,
                CouponId = couponId,
                CouponCode = couponCode,
                CouponTitle = couponTitle,
                CouponType = couponType,
                DiscountValue = discountValue,
                IsPercentage = isPercentage,
                CalculatedDiscount = calculatedDiscount,
                OriginalTotal = originalTotal,
                FinalTotal = finalTotal,
                FreeShippingApplied = freeShippingApplied,
                RemainingUsage = remainingUsage,
                ExpirationDate = expirationDate
            };
        }

        /// <summary>
        /// Başarısız doğrulama sonucu oluşturur
        /// </summary>
        public static CouponValidationResult Failure(string errorCode, string errorMessage)
        {
            return new CouponValidationResult
            {
                IsValid = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CalculatedDiscount = 0
            };
        }
    }

    /// <summary>
    /// Kupon doğrulama isteği DTO'su
    /// </summary>
    public class CouponValidateRequestDto
    {
        /// <summary>
        /// Doğrulanacak kupon kodu
        /// </summary>
        public string CouponCode { get; set; } = string.Empty;

        /// <summary>
        /// Sepet toplam tutarı (indirim öncesi)
        /// </summary>
        public decimal CartTotal { get; set; }

        /// <summary>
        /// Kargo ücreti
        /// </summary>
        public decimal ShippingCost { get; set; }

        /// <summary>
        /// Sepetteki ürün ID'leri (ürün bazlı kupon kontrolü için)
        /// </summary>
        public List<int> ProductIds { get; set; } = new();

        /// <summary>
        /// Sepetteki kategori ID'leri (kategori bazlı kupon kontrolü için)
        /// </summary>
        public List<int> CategoryIds { get; set; } = new();

        /// <summary>
        /// Sepetteki ürün adetleri (BuyXGetY için)
        /// Key: ProductId, Value: Quantity
        /// </summary>
        public Dictionary<int, int>? ProductQuantities { get; set; }
    }

    /// <summary>
    /// Kupon kullanım kaydı oluşturma DTO'su
    /// </summary>
    public class CouponUsageCreateDto
    {
        public int CouponId { get; set; }
        public int? UserId { get; set; }
        public int OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public decimal OrderTotalBeforeDiscount { get; set; }
        public decimal OrderTotalAfterDiscount { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? SessionId { get; set; }
    }

    /// <summary>
    /// Kupon bilgisi özet DTO'su (liste görünümü için)
    /// </summary>
    public class CouponSummaryDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Title { get; set; }
        public CouponType Type { get; set; }
        public decimal Value { get; set; }
        public bool IsActive { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int UsageCount { get; set; }
        public int UsageLimit { get; set; }
        public decimal? MinOrderAmount { get; set; }
    }

    /// <summary>
    /// Kupon detay DTO'su (tam bilgi)
    /// </summary>
    public class CouponDetailDto : CouponSummaryDto
    {
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public int? MaxUsagePerUser { get; set; }
        public bool IsSingleUse { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IncludeSubCategories { get; set; }
        public bool IsPrivate { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? BuyXPayY { get; set; }
        public List<int>? ProductIds { get; set; }
        public List<string>? ProductNames { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
