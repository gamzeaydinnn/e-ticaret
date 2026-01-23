using ECommerce.Entities.Enums;
using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Weight
{
    #region Request DTO'ları

    /// <summary>
    /// Kurye tarafından tartı girişi için kullanılan DTO
    /// Kurye panelinden gönderilen tartı verisini taşır
    /// </summary>
    public class WeighItemRequest
    {
        /// <summary>
        /// Sipariş kalemi ID'si
        /// Hangi ürünün tartıldığını belirler
        /// </summary>
        public int OrderItemId { get; set; }

        /// <summary>
        /// Gerçek ağırlık (gram cinsinden)
        /// Kurye tarafından tartılan miktar
        /// </summary>
        public decimal ActualWeightGrams { get; set; }

        /// <summary>
        /// Kurye notu (opsiyonel)
        /// Özel durumlar için açıklama
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// Toplu tartı girişi için kullanılan DTO
    /// Bir siparişin tüm ağırlık bazlı ürünlerini tek seferde tartmak için
    /// </summary>
    public class BulkWeighRequest
    {
        /// <summary>
        /// Sipariş ID'si
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Tartılan ürünlerin listesi
        /// </summary>
        public List<WeighItemRequest> Items { get; set; } = new();

        /// <summary>
        /// Kurye ID'si
        /// JWT'den alınacak ama doğrulama için de gönderilebilir
        /// </summary>
        public int? CourierId { get; set; }
    }

    /// <summary>
    /// Admin tarafından fark onay/red işlemi için DTO
    /// </summary>
    public class AdminAdjustmentDecisionRequest
    {
        /// <summary>
        /// WeightAdjustment kayıt ID'si
        /// </summary>
        public int WeightAdjustmentId { get; set; }

        /// <summary>
        /// Onay durumu (true: onayla, false: reddet)
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Admin tarafından düzeltilmiş fiyat (opsiyonel)
        /// Onay durumunda farklı bir fiyat belirlenebilir
        /// </summary>
        public decimal? AdjustedPrice { get; set; }

        /// <summary>
        /// Admin notu
        /// Karar gerekçesi veya açıklama
        /// </summary>
        public string? AdminNote { get; set; }
    }

    /// <summary>
    /// Sipariş teslim ve ödeme kesinleştirme için DTO
    /// Kurye "Teslim Edildi" butonuna bastığında kullanılır
    /// </summary>
    public class FinalizeDeliveryRequest
    {
        /// <summary>
        /// Sipariş ID'si
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Kurye ID'si
        /// </summary>
        public int CourierId { get; set; }

        /// <summary>
        /// Teslimat notu (opsiyonel)
        /// </summary>
        public string? DeliveryNote { get; set; }

        /// <summary>
        /// Müşteri imzası alındı mı? (kapıda ödeme için)
        /// </summary>
        public bool CustomerSignatureReceived { get; set; } = false;
    }

    #endregion

    #region Response DTO'ları

    /// <summary>
    /// Ağırlık fark kaydı detay DTO'su
    /// API response'larında kullanılır
    /// </summary>
    public class WeightAdjustmentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }

        // Ağırlık bilgileri
        public WeightUnit WeightUnit { get; set; }
        public string WeightUnitDisplay { get; set; } = string.Empty;
        public decimal EstimatedWeight { get; set; }
        public decimal ActualWeight { get; set; }
        public decimal WeightDifference { get; set; }
        public decimal DifferencePercent { get; set; }
        public string WeightDifferenceDisplay { get; set; } = string.Empty;

        // Fiyat bilgileri
        public decimal PricePerUnit { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal ActualPrice { get; set; }
        public decimal PriceDifference { get; set; }
        public string PriceDifferenceDisplay { get; set; } = string.Empty;

        // Durum bilgileri
        public WeightAdjustmentStatus Status { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "gray"; // UI için renk kodu

        // Tartı bilgileri
        public DateTime? WeighedAt { get; set; }
        public int? WeighedByCourierId { get; set; }
        public string? WeighedByCourierName { get; set; }

        // Ödeme bilgileri
        public bool IsSettled { get; set; }
        public DateTime? SettledAt { get; set; }
        public string? PaymentTransactionId { get; set; }

        // Admin bilgileri
        public bool RequiresAdminApproval { get; set; }
        public bool AdminReviewed { get; set; }
        public bool? AdminApproved { get; set; }
        public decimal? AdminAdjustedPrice { get; set; }
        public string? AdminNote { get; set; }
        public DateTime? AdminReviewedAt { get; set; }
        public string? AdminUserName { get; set; }

        // Müşteri bilgilendirme
        public bool CustomerNotified { get; set; }
        public DateTime? CustomerNotifiedAt { get; set; }

        // Zaman damgaları
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Sipariş ağırlık özeti DTO'su
    /// Sipariş detayında gösterilecek ağırlık fark özeti
    /// </summary>
    public class OrderWeightSummaryDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Ağırlık bazlı ürün sayısı
        /// </summary>
        public int WeightBasedItemCount { get; set; }

        /// <summary>
        /// Tartılan ürün sayısı
        /// </summary>
        public int WeighedItemCount { get; set; }

        /// <summary>
        /// Tüm ürünler tartıldı mı?
        /// </summary>
        public bool AllItemsWeighed { get; set; }

        /// <summary>
        /// Tahmini toplam tutar
        /// </summary>
        public decimal EstimatedTotal { get; set; }

        /// <summary>
        /// Gerçek toplam tutar (tartı sonrası)
        /// </summary>
        public decimal ActualTotal { get; set; }

        /// <summary>
        /// Toplam fark tutarı
        /// Pozitif: Ek ödeme, Negatif: İade
        /// </summary>
        public decimal TotalDifference { get; set; }

        /// <summary>
        /// Fark yüzdesi
        /// </summary>
        public decimal DifferencePercent { get; set; }

        /// <summary>
        /// Genel durum
        /// </summary>
        public WeightAdjustmentStatus OverallStatus { get; set; }
        public string OverallStatusDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Admin onayı bekleyen kayıt var mı?
        /// </summary>
        public bool HasPendingAdminApproval { get; set; }

        /// <summary>
        /// Fark kayıtları detayı
        /// </summary>
        public List<WeightAdjustmentDto> Adjustments { get; set; } = new();
    }

    /// <summary>
    /// Kurye paneli için sipariş ağırlık bilgisi DTO'su
    /// Kurye tartı girişi ekranında kullanılır
    /// </summary>
    public class CourierWeighingDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;

        /// <summary>
        /// Tartılacak ürünler listesi
        /// </summary>
        public List<WeighableItemDto> Items { get; set; } = new();

        /// <summary>
        /// Tahmini toplam tutar
        /// </summary>
        public decimal EstimatedTotal { get; set; }

        /// <summary>
        /// Ödeme yöntemi
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme yöntemi görüntü adı
        /// </summary>
        public string PaymentMethodDisplay { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tartılacak ürün DTO'su
    /// Kurye panelinde ürün kartı için kullanılır
    /// </summary>
    public class WeighableItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        
        /// <summary>
        /// Ağırlık birimi
        /// </summary>
        public WeightUnit WeightUnit { get; set; }
        public string WeightUnitDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Birim fiyat (kg/gram/litre başına)
        /// </summary>
        public decimal PricePerUnit { get; set; }

        /// <summary>
        /// Müşterinin istediği miktar (gram)
        /// </summary>
        public decimal EstimatedWeight { get; set; }
        public string EstimatedWeightDisplay { get; set; } = string.Empty;

        /// <summary>
        /// Tahmini fiyat
        /// </summary>
        public decimal EstimatedPrice { get; set; }

        /// <summary>
        /// Tartıldı mı?
        /// </summary>
        public bool IsWeighed { get; set; }

        /// <summary>
        /// Gerçek ağırlık (tartıldıysa)
        /// </summary>
        public decimal? ActualWeight { get; set; }
        public string? ActualWeightDisplay { get; set; }

        /// <summary>
        /// Gerçek fiyat (tartıldıysa)
        /// </summary>
        public decimal? ActualPrice { get; set; }

        /// <summary>
        /// Fark tutarı (tartıldıysa)
        /// </summary>
        public decimal? PriceDifference { get; set; }
    }

    /// <summary>
    /// Tartı işlemi sonuç DTO'su
    /// API response olarak döner
    /// </summary>
    public class WeighingResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// İşlem yapılan kayıt
        /// </summary>
        public WeightAdjustmentDto? Adjustment { get; set; }

        /// <summary>
        /// Sipariş özeti (tüm ürünler tartıldıysa)
        /// </summary>
        public OrderWeightSummaryDto? OrderSummary { get; set; }

        /// <summary>
        /// Tüm ürünler tartıldı ve sipariş kesinleştirilebilir mi?
        /// </summary>
        public bool CanFinalize { get; set; }

        /// <summary>
        /// Admin onayı gerekiyor mu?
        /// </summary>
        public bool RequiresAdminApproval { get; set; }
    }

    /// <summary>
    /// Teslimat kesinleştirme sonuç DTO'su
    /// </summary>
    public class DeliveryFinalizationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Kesin tutar
        /// </summary>
        public decimal FinalAmount { get; set; }

        /// <summary>
        /// Fark tutarı
        /// </summary>
        public decimal DifferenceAmount { get; set; }

        /// <summary>
        /// Ödeme işlemi başarılı mı?
        /// </summary>
        public bool PaymentProcessed { get; set; }

        /// <summary>
        /// Ödeme işlem referans numarası
        /// </summary>
        public string? PaymentTransactionId { get; set; }

        /// <summary>
        /// Hata mesajı (varsa)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Admin Panel DTO'ları

    /// <summary>
    /// Admin panel için ağırlık fark listesi filtre DTO'su
    /// </summary>
    public class WeightAdjustmentFilterDto
    {
        /// <summary>
        /// Durum filtresi
        /// </summary>
        public WeightAdjustmentStatus? Status { get; set; }

        /// <summary>
        /// Sadece admin onayı bekleyenler
        /// </summary>
        public bool? RequiresAdminApproval { get; set; }

        /// <summary>
        /// Sadece ödeme/iade bekleyenler
        /// </summary>
        public bool? PendingSettlement { get; set; }

        /// <summary>
        /// Başlangıç tarihi
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Bitiş tarihi
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Sipariş numarası ile arama
        /// </summary>
        public string? OrderNumber { get; set; }

        /// <summary>
        /// Kurye ID filtresi
        /// </summary>
        public int? CourierId { get; set; }

        /// <summary>
        /// Sayfa numarası
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Sayfa boyutu
        /// </summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Admin dashboard için istatistik DTO'su
    /// </summary>
    public class WeightAdjustmentStatsDto
    {
        /// <summary>
        /// Toplam kayıt sayısı
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Tartı bekleyen
        /// </summary>
        public int PendingWeighingCount { get; set; }

        /// <summary>
        /// Admin onayı bekleyen
        /// </summary>
        public int PendingAdminApprovalCount { get; set; }

        /// <summary>
        /// Ödeme/İade bekleyen
        /// </summary>
        public int PendingSettlementCount { get; set; }

        /// <summary>
        /// Tamamlanan
        /// </summary>
        public int CompletedCount { get; set; }

        /// <summary>
        /// Toplam ek ödeme tutarı
        /// </summary>
        public decimal TotalAdditionalPayments { get; set; }

        /// <summary>
        /// Toplam iade tutarı
        /// </summary>
        public decimal TotalRefunds { get; set; }

        /// <summary>
        /// Bugünkü işlem sayısı
        /// </summary>
        public int TodayCount { get; set; }

        /// <summary>
        /// Bu haftaki işlem sayısı
        /// </summary>
        public int ThisWeekCount { get; set; }
    }

    #endregion
}
