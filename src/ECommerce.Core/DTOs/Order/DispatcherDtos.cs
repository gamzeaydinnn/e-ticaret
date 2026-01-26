// ==========================================================================
// DispatcherDtos.cs - Sevkiyat Görevlisi DTO'ları
// ==========================================================================
// Sevkiyat görevlisi paneli için sipariş, kurye ve özet DTO'larını içerir.
// Bu DTO'lar DispatcherOrderController tarafından kullanılır.
// ==========================================================================

using System;
using System.Collections.Generic;

namespace ECommerce.Core.DTOs.Order
{
    // =====================================================================
    // SİPARİŞ LİSTESİ RESPONSE
    // =====================================================================
    
    /// <summary>
    /// Sevkiyat görevlisi sipariş listesi response DTO.
    /// Siparişler, kuryeler ve özet istatistikleri içerir.
    /// </summary>
    public class DispatcherOrderListResponseDto
    {
        /// <summary>
        /// Sipariş listesi
        /// </summary>
        public List<DispatcherOrderDto> Orders { get; set; } = new();
        
        /// <summary>
        /// Özet istatistikler
        /// </summary>
        public DispatcherSummaryDto Summary { get; set; } = new();
        
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
    /// Sevkiyat görevlisi için sipariş kartı DTO.
    /// Dashboard'daki sipariş kartlarında gösterilen bilgileri içerir.
    /// </summary>
    public class DispatcherOrderDto
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
        /// Sipariş durumu
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
        /// Teslimat adresi
        /// </summary>
        public string DeliveryAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// Teslimat notu
        /// </summary>
        public string? DeliveryNotes { get; set; }
        
        /// <summary>
        /// Ödeme yöntemi
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Kapıda ödeme mi?
        /// </summary>
        public bool IsCashOnDelivery { get; set; }
        
        /// <summary>
        /// Hazır olma zamanı
        /// </summary>
        public DateTime? ReadyAt { get; set; }
        
        /// <summary>
        /// Kurye atanma zamanı
        /// </summary>
        public DateTime? AssignedAt { get; set; }
        
        /// <summary>
        /// Yola çıkış zamanı
        /// </summary>
        public DateTime? PickedUpAt { get; set; }
        
        /// <summary>
        /// Atanan kurye ID
        /// </summary>
        public int? CourierId { get; set; }
        
        /// <summary>
        /// Atanan kurye adı
        /// </summary>
        public string? CourierName { get; set; }
        
        /// <summary>
        /// Kurye telefonu
        /// </summary>
        public string? CourierPhone { get; set; }
        
        /// <summary>
        /// Ne kadar önce hazır oldu (örn: "5 dk önce")
        /// </summary>
        public string TimeAgo { get; set; } = string.Empty;
        
        /// <summary>
        /// Bekleme süresi (dakika)
        /// </summary>
        public int WaitingMinutes { get; set; }
        
        /// <summary>
        /// Acil mi? (30 dk'dan fazla bekleyen)
        /// </summary>
        public bool IsUrgent { get; set; }
        
        /// <summary>
        /// Ağırlık (gram cinsinden)
        /// </summary>
        public int? WeightInGrams { get; set; }
        
        /// <summary>
        /// Tahmini teslimat süresi (dakika)
        /// </summary>
        public int? EstimatedDeliveryMinutes { get; set; }
    }

    // =====================================================================
    // KURYE DTO'LARI
    // =====================================================================
    
    /// <summary>
    /// Sevkiyat görevlisi için kurye listesi DTO.
    /// Kurye seçim dropdown'ında kullanılır.
    /// </summary>
    public class DispatcherCourierDto
    {
        /// <summary>
        /// Kurye ID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Kurye tam adı
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Kurye telefonu
        /// </summary>
        public string Phone { get; set; } = string.Empty;
        
        /// <summary>
        /// Kurye durumu (online, busy, offline)
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Durum Türkçe açıklaması
        /// </summary>
        public string StatusText { get; set; } = string.Empty;
        
        /// <summary>
        /// Durum rengi (yeşil, sarı, kırmızı)
        /// </summary>
        public string StatusColor { get; set; } = string.Empty;
        
        /// <summary>
        /// Aktif sipariş sayısı
        /// </summary>
        public int ActiveOrderCount { get; set; }
        
        /// <summary>
        /// Bugün teslim edilen sipariş sayısı
        /// </summary>
        public int DeliveredTodayCount { get; set; }
        
        /// <summary>
        /// Araç tipi (motorcycle, car, bicycle, on_foot)
        /// </summary>
        public string VehicleType { get; set; } = string.Empty;
        
        /// <summary>
        /// Araç tipi Türkçe
        /// </summary>
        public string VehicleTypeText { get; set; } = string.Empty;
        
        /// <summary>
        /// Müsait mi? (online ve max_orders'a ulaşmamış)
        /// </summary>
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// Son konum güncelleme zamanı
        /// </summary>
        public DateTime? LastLocationUpdate { get; set; }
        
        /// <summary>
        /// Son görülme zamanı
        /// </summary>
        public DateTime? LastSeenAt { get; set; }
        
        /// <summary>
        /// Ortalama teslimat süresi (dakika)
        /// </summary>
        public double AverageDeliveryTimeMinutes { get; set; }
    }

    /// <summary>
    /// Kurye listesi response DTO.
    /// </summary>
    public class DispatcherCourierListResponseDto
    {
        /// <summary>
        /// Kurye listesi
        /// </summary>
        public List<DispatcherCourierDto> Couriers { get; set; } = new();
        
        /// <summary>
        /// Online kurye sayısı
        /// </summary>
        public int OnlineCount { get; set; }
        
        /// <summary>
        /// Müsait kurye sayısı
        /// </summary>
        public int AvailableCount { get; set; }
        
        /// <summary>
        /// Meşgul kurye sayısı
        /// </summary>
        public int BusyCount { get; set; }
        
        /// <summary>
        /// Offline kurye sayısı
        /// </summary>
        public int OfflineCount { get; set; }
    }

    // =====================================================================
    // ÖZET DTO
    // =====================================================================
    
    /// <summary>
    /// Sevkiyat görevlisi günlük özet istatistikleri.
    /// Dashboard üst kısmındaki istatistik kartlarında gösterilir.
    /// </summary>
    public class DispatcherSummaryDto
    {
        /// <summary>
        /// Hazır sipariş sayısı (kurye bekleyen)
        /// </summary>
        public int ReadyCount { get; set; }
        
        /// <summary>
        /// Atanan sipariş sayısı (kurye teslim almayı bekliyor)
        /// </summary>
        public int AssignedCount { get; set; }
        
        /// <summary>
        /// Yolda sipariş sayısı (teslimat süreci)
        /// </summary>
        public int OutForDeliveryCount { get; set; }
        
        /// <summary>
        /// Bugün teslim edilen sipariş sayısı
        /// </summary>
        public int DeliveredTodayCount { get; set; }
        
        /// <summary>
        /// Bugün başarısız teslimat sayısı
        /// </summary>
        public int FailedTodayCount { get; set; }
        
        /// <summary>
        /// Acil siparişler (30+ dk bekleyen)
        /// </summary>
        public int UrgentCount { get; set; }
        
        /// <summary>
        /// Online kurye sayısı
        /// </summary>
        public int OnlineCouriersCount { get; set; }
        
        /// <summary>
        /// Müsait kurye sayısı
        /// </summary>
        public int AvailableCouriersCount { get; set; }
        
        /// <summary>
        /// Ortalama bekleme süresi (dakika)
        /// </summary>
        public double AverageWaitingTimeMinutes { get; set; }
        
        /// <summary>
        /// Bugünkü toplam teslimat tutarı
        /// </summary>
        public decimal TodayTotalDeliveredAmount { get; set; }
        
        /// <summary>
        /// Son güncelleme zamanı
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    // =====================================================================
    // FİLTRE DTO
    // =====================================================================
    
    /// <summary>
    /// Sevkiyat görevlisi sipariş filtreleme parametreleri.
    /// </summary>
    public class DispatcherOrderFilterDto
    {
        /// <summary>
        /// Durum filtresi (ready, assigned, out_for_delivery, all)
        /// </summary>
        public string? Status { get; set; }
        
        /// <summary>
        /// Kurye ID filtresi
        /// </summary>
        public int? CourierId { get; set; }
        
        /// <summary>
        /// Sadece acil siparişler
        /// </summary>
        public bool? UrgentOnly { get; set; }
        
        /// <summary>
        /// Sayfa numarası (varsayılan: 1)
        /// </summary>
        public int Page { get; set; } = 1;
        
        /// <summary>
        /// Sayfa boyutu (varsayılan: 20, max: 100)
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Sıralama alanı (readyAt, totalAmount, waitingTime)
        /// </summary>
        public string? SortBy { get; set; }
        
        /// <summary>
        /// Sıralama yönü (asc, desc)
        /// </summary>
        public string? SortOrder { get; set; } = "asc"; // En eski hazır siparişler önce
    }

    // =====================================================================
    // AKSİYON DTO'LARI
    // =====================================================================
    
    /// <summary>
    /// Kurye atama request DTO.
    /// </summary>
    public class AssignCourierRequestDto
    {
        /// <summary>
        /// Kurye ID
        /// </summary>
        public int CourierId { get; set; }
        
        /// <summary>
        /// Not (opsiyonel)
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Öncelik (normal, high, urgent)
        /// </summary>
        public string Priority { get; set; } = "normal";
    }

    /// <summary>
    /// Kurye yeniden atama request DTO.
    /// </summary>
    public class ReassignCourierRequestDto
    {
        /// <summary>
        /// Eski kurye ID (bildirim göndermek için)
        /// </summary>
        public int? OldCourierId { get; set; }
        
        /// <summary>
        /// Yeni kurye ID
        /// </summary>
        public int NewCourierId { get; set; }
        
        /// <summary>
        /// Değişiklik nedeni
        /// </summary>
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Not (opsiyonel)
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Kurye atama response DTO.
    /// </summary>
    public class AssignCourierResponseDto
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
        public DispatcherOrderDto? Order { get; set; }
        
        /// <summary>
        /// Atanan kurye bilgisi
        /// </summary>
        public DispatcherCourierDto? Courier { get; set; }
    }
}
