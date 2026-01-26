// ==========================================================================
// StoreAttendantDtos.cs - Market Görevlisi DTO'ları
// ==========================================================================
// Market görevlisi paneli için sipariş ve özet DTO'larını içerir.
// Bu DTO'lar StoreAttendantOrderController tarafından kullanılır.
// ==========================================================================

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order
{
    // =====================================================================
    // SİPARİŞ LİSTESİ RESPONSE
    // =====================================================================
    
    /// <summary>
    /// Market görevlisi sipariş listesi response DTO.
    /// Siparişler ve özet istatistikleri içerir.
    /// </summary>
    public class StoreAttendantOrderListResponseDto
    {
        /// <summary>
        /// Sipariş listesi
        /// </summary>
        public List<StoreAttendantOrderDto> Orders { get; set; } = new();
        
        /// <summary>
        /// Özet istatistikler
        /// </summary>
        public StoreAttendantSummaryDto Summary { get; set; } = new();
        
        /// <summary>
        /// Toplam sayfa sayısı
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// Toplam sipariş sayısı
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Mevcut sayfa
        /// </summary>
        public int CurrentPage { get; set; }
    }

    // =====================================================================
    // SİPARİŞ DTO
    // =====================================================================
    
    /// <summary>
    /// Market görevlisi için sipariş kartı DTO.
    /// Dashboard'daki sipariş kartlarında gösterilen bilgileri içerir.
    /// </summary>
    public class StoreAttendantOrderDto
    {
        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Sipariş numarası (ORD-xxxxx)
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// Müşteri adı
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;
        
        /// <summary>
        /// Müşteri telefon numarası
        /// </summary>
        public string? CustomerPhone { get; set; }
        
        /// <summary>
        /// Sipariş durumu (Confirmed, Preparing, Ready)
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Durum Türkçe açıklaması
        /// </summary>
        public string StatusText { get; set; } = string.Empty;
        
        /// <summary>
        /// Sipariş toplam tutarı
        /// </summary>
        public decimal TotalAmount { get; set; }
        
        /// <summary>
        /// Toplam ürün sayısı
        /// </summary>
        public int ItemCount { get; set; }
        
        /// <summary>
        /// Sipariş oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Sipariş onaylanma zamanı
        /// </summary>
        public DateTime? ConfirmedAt { get; set; }
        
        /// <summary>
        /// Hazırlanmaya başlanma zamanı
        /// </summary>
        public DateTime? PreparingStartedAt { get; set; }
        
        /// <summary>
        /// Hazır olma zamanı
        /// </summary>
        public DateTime? ReadyAt { get; set; }
        
        /// <summary>
        /// Ödeme yöntemi
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Kapıda ödeme mi?
        /// </summary>
        public bool IsCashOnDelivery { get; set; }
        
        /// <summary>
        /// Sipariş notu (müşteriden)
        /// </summary>
        public string? OrderNotes { get; set; }
        
        /// <summary>
        /// Teslimat adresi
        /// </summary>
        public string? DeliveryAddress { get; set; }
        
        /// <summary>
        /// Ürün özeti (ilk 3 ürün)
        /// </summary>
        public List<StoreOrderItemSummaryDto> Items { get; set; } = new();
        
        /// <summary>
        /// Siparişi hazırlayan görevli (eğer hazırlanıyorsa)
        /// </summary>
        public string? PreparedBy { get; set; }
        
        /// <summary>
        /// Ne kadar önce oluşturuldu (örn: "5 dk önce")
        /// </summary>
        public string TimeAgo { get; set; } = string.Empty;
        
        /// <summary>
        /// Ağırlık bilgisi (gram cinsinden, opsiyonel)
        /// </summary>
        public int? WeightInGrams { get; set; }
        
        /// <summary>
        /// Ağırlık bazlı ürün var mı?
        /// </summary>
        public bool HasWeightBasedItems { get; set; }
    }

    /// <summary>
    /// Sipariş kartında gösterilecek ürün özeti.
    /// </summary>
    public class StoreOrderItemSummaryDto
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }
        
        /// <summary>
        /// Ürün adı
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        
        /// <summary>
        /// Miktar
        /// </summary>
        public int Quantity { get; set; }
        
        /// <summary>
        /// Birim fiyat
        /// </summary>
        public decimal UnitPrice { get; set; }
        
        /// <summary>
        /// Ürün görseli URL
        /// </summary>
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Ağırlık bazlı mı?
        /// </summary>
        public bool IsWeightBased { get; set; }
        
        /// <summary>
        /// Birim (adet, kg, gr)
        /// </summary>
        public string Unit { get; set; } = "adet";
    }

    // =====================================================================
    // ÖZET DTO
    // =====================================================================
    
    /// <summary>
    /// Market görevlisi günlük özet istatistikleri.
    /// Dashboard üst kısmındaki istatistik kartlarında gösterilir.
    /// </summary>
    public class StoreAttendantSummaryDto
    {
        /// <summary>
        /// Onay bekleyen sipariş sayısı (Confirmed durumunda)
        /// </summary>
        public int PendingCount { get; set; }
        
        /// <summary>
        /// Hazırlanmakta olan sipariş sayısı (Preparing durumunda)
        /// </summary>
        public int PreparingCount { get; set; }
        
        /// <summary>
        /// Hazır sipariş sayısı (Ready durumunda, kurye bekliyor)
        /// </summary>
        public int ReadyCount { get; set; }
        
        /// <summary>
        /// Bugün tamamlanan sipariş sayısı
        /// </summary>
        public int CompletedTodayCount { get; set; }
        
        /// <summary>
        /// Bugünkü toplam sipariş tutarı
        /// </summary>
        public decimal TodayTotalAmount { get; set; }
        
        /// <summary>
        /// Ortalama hazırlık süresi (dakika)
        /// </summary>
        public double AveragePreparationTimeMinutes { get; set; }
        
        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // =====================================================================
    // FİLTRE DTO
    // =====================================================================
    
    /// <summary>
    /// Market görevlisi sipariş filtreleme parametreleri.
    /// </summary>
    public class StoreAttendantOrderFilterDto
    {
        /// <summary>
        /// Durum filtresi (confirmed, preparing, ready, all)
        /// </summary>
        public string? Status { get; set; }
        
        /// <summary>
        /// Sayfa numarası (varsayılan: 1)
        /// </summary>
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Sayfa boyutu (varsayılan: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Sıralama alanı (createdAt, totalAmount)
        /// </summary>
        public string? SortBy { get; set; }
        
        /// <summary>
        /// Sıralama yönü (asc, desc)
        /// </summary>
        public string? SortOrder { get; set; } = "desc";
    }

    // =====================================================================
    // AKSİYON DTO'LARI
    // =====================================================================
    
    /// <summary>
    /// Sipariş hazır işaretleme request DTO.
    /// </summary>
    public class MarkOrderReadyRequestDto
    {
        /// <summary>
        /// Ağırlık (gram cinsinden, opsiyonel)
        /// Ağırlık bazlı ürünler için zorunlu olabilir.
        /// </summary>
        public int? WeightInGrams { get; set; }
        
        /// <summary>
        /// Not (opsiyonel)
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Sipariş hazır işaretleme response DTO.
    /// </summary>
    public class MarkOrderReadyResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Mesaj
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Güncellenmiş sipariş bilgisi
        /// </summary>
        public StoreAttendantOrderDto? Order { get; set; }
    }
}
