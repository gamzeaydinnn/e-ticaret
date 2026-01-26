using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Order;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null);
        Task<OrderListDto?> GetByIdAsync(int id);
        Task<OrderListDto?> GetByClientOrderIdAsync(Guid clientOrderId);
        Task<OrderListDto> CreateAsync(OrderCreateDto dto);
        Task UpdateAsync(int id, OrderUpdateDto dto);
        Task DeleteAsync(int id);
        Task<bool> ChangeOrderStatusAsync(int id, string newStatus);
        Task<OrderListDto> CheckoutAsync(OrderCreateDto dto);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        Task<bool> MarkPaymentFailedAsync(int orderId);
        Task<OrderListDto?> MarkOrderAsPreparingAsync(int orderId);
        Task<OrderListDto?> MarkOrderOutForDeliveryAsync(int orderId);
        Task<OrderListDto?> MarkOrderAsDeliveredAsync(int orderId);
        Task<OrderListDto?> CancelOrderByAdminAsync(int orderId);
        Task<OrderListDto?> RefundOrderAsync(int orderId);
        Task<int> GetOrderCountAsync();
        Task<int> GetTodayOrderCountAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<IEnumerable<OrderListDto>> GetAllOrdersAsync(int page = 1, int size = 20);
        Task<OrderListDto> GetOrderByIdAsync(int id);
        Task UpdateOrderStatusAsync(int id, string status);
        Task<IEnumerable<OrderListDto>> GetRecentOrdersAsync(int count = 10);
        Task<OrderDetailDto?> GetDetailByIdAsync(int id);
        
        // ============================================================
        // KURYE ATAMA
        // ============================================================
        /// <summary>
        /// Siparişe kurye atar ve durumu "assigned" olarak günceller.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <returns>Güncellenmiş sipariş bilgisi</returns>
        Task<OrderListDto?> AssignCourierAsync(int orderId, int courierId);
        
        // ============================================================
        // STORE ATTENDANT (MARKET GÖREVLİSİ) METODLARI
        // ============================================================
        
        /// <summary>
        /// Siparişi "Confirmed" (Admin onayladı) durumuna getirir.
        /// New → Confirmed geçişi yapar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="confirmedBy">Onaylayan kullanıcı adı</param>
        /// <returns>Güncellenmiş sipariş veya null</returns>
        Task<OrderListDto?> MarkOrderAsConfirmedAsync(int orderId, string confirmedBy);
        
        /// <summary>
        /// Siparişi "Preparing" (Hazırlanıyor) durumuna getirir.
        /// Confirmed → Preparing geçişi yapar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="preparedBy">Hazırlayan kullanıcı adı</param>
        /// <returns>Güncellenmiş sipariş veya null</returns>
        Task<OrderListDto?> StartPreparingAsync(int orderId, string preparedBy);
        
        /// <summary>
        /// Siparişi "Ready" (Hazır) durumuna getirir.
        /// Preparing → Ready geçişi yapar.
        /// Opsiyonel ağırlık bilgisi kaydedilebilir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="readyBy">Hazır işaretleyen kullanıcı adı</param>
        /// <param name="weightInGrams">Ağırlık (gram, opsiyonel)</param>
        /// <returns>Güncellenmiş sipariş veya null</returns>
        Task<OrderListDto?> MarkOrderAsReadyAsync(int orderId, string readyBy, int? weightInGrams = null);
        
        /// <summary>
        /// Market görevlisi için siparişleri getirir.
        /// Confirmed, Preparing, Ready durumlarındaki siparişleri listeler.
        /// </summary>
        /// <param name="filter">Filtreleme parametreleri</param>
        /// <returns>Sipariş listesi ve özet</returns>
        Task<StoreAttendantOrderListResponseDto> GetOrdersForStoreAttendantAsync(StoreAttendantOrderFilterDto? filter);
        
        /// <summary>
        /// Market görevlisi günlük özet istatistiklerini getirir.
        /// </summary>
        /// <returns>Özet istatistikler</returns>
        Task<StoreAttendantSummaryDto> GetStoreAttendantSummaryAsync();
        
        // ============================================================
        // DISPATCHER (SEVKİYAT GÖREVLİSİ) METODLARI
        // ============================================================
        
        /// <summary>
        /// Sevkiyat görevlisi için siparişleri getirir.
        /// Ready, Assigned, OutForDelivery durumlarındaki siparişleri listeler.
        /// </summary>
        /// <param name="filter">Filtreleme parametreleri</param>
        /// <returns>Sipariş listesi ve özet</returns>
        Task<DispatcherOrderListResponseDto> GetOrdersForDispatcherAsync(DispatcherOrderFilterDto? filter);
        
        /// <summary>
        /// Sevkiyat görevlisi günlük özet istatistiklerini getirir.
        /// </summary>
        /// <returns>Özet istatistikler</returns>
        Task<DispatcherSummaryDto> GetDispatcherSummaryAsync();
        
        /// <summary>
        /// Siparişe kurye atar ve Assigned durumuna getirir.
        /// Ready → Assigned geçişi yapar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="courierId">Kurye ID</param>
        /// <param name="assignedBy">Atayan kullanıcı adı</param>
        /// <param name="notes">Not (opsiyonel)</param>
        /// <returns>Güncellenmiş sipariş ve kurye bilgisi</returns>
        Task<AssignCourierResponseDto> AssignCourierToOrderAsync(int orderId, int courierId, string assignedBy, string? notes = null);
        
        /// <summary>
        /// Kuryeyi değiştirir ve yeni kurye atar.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="newCourierId">Yeni kurye ID</param>
        /// <param name="reassignedBy">Değiştiren kullanıcı adı</param>
        /// <param name="reason">Değişiklik nedeni</param>
        /// <returns>Güncellenmiş sipariş ve yeni kurye bilgisi</returns>
        Task<AssignCourierResponseDto> ReassignCourierAsync(int orderId, int newCourierId, string reassignedBy, string reason);
        
        /// <summary>
        /// Müsait kuryeleri listeler.
        /// </summary>
        /// <returns>Kurye listesi ve durum özeti</returns>
        Task<DispatcherCourierListResponseDto> GetAvailableCouriersAsync();
    }
}
/* Task<OrderSummaryDto> CreateOrderAsync(OrderCreateDto dto, Guid userId, CancellationToken ct = default);
    Task<OrderDetailDto> GetOrderAsync(Guid orderId);
    Task<IEnumerable<OrderListDto>> GetOrdersAsync(int? userId = null);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task ConfirmPaymentAsync(Guid orderId, PaymentResultDto paymentResult); // payment webhook tetikler
}*/
