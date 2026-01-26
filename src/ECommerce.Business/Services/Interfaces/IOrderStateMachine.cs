// ==========================================================================
// IOrderStateMachine.cs - Sipariş Durum Geçiş Interface'i
// ==========================================================================
// Sipariş durumları arasında geçişleri yöneten state machine interface.
// Guard koşulları ve geçiş validasyonları içerir.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Sipariş durumları arasında geçişleri yöneten state machine interface.
    /// </summary>
    public interface IOrderStateMachine
    {
        /// <summary>
        /// Mevcut durumdan hedef duruma geçiş yapılabilir mi kontrol eder.
        /// </summary>
        /// <param name="currentStatus">Mevcut sipariş durumu</param>
        /// <param name="targetStatus">Hedef sipariş durumu</param>
        /// <returns>Geçiş yapılabilirse true</returns>
        bool CanTransition(OrderStatus currentStatus, OrderStatus targetStatus);

        /// <summary>
        /// Sipariş için belirli bir duruma geçiş yapar.
        /// Guard koşullarını kontrol eder ve geçiş işlemini gerçekleştirir.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="targetStatus">Hedef durum</param>
        /// <param name="actorId">İşlemi yapan kullanıcı ID (admin/kurye/sistem)</param>
        /// <param name="reason">Durum değişikliği sebebi</param>
        /// <param name="metadata">Ek metadata (opsiyonel)</param>
        /// <returns>Geçiş sonucu</returns>
        Task<OrderTransitionResult> TransitionAsync(int orderId, OrderStatus targetStatus, 
            int? actorId, string? reason = null, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Mevcut durumdan geçilebilecek tüm durumları listeler.
        /// </summary>
        /// <param name="currentStatus">Mevcut sipariş durumu</param>
        /// <returns>Geçilebilecek durumlar listesi</returns>
        IEnumerable<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus);

        /// <summary>
        /// Bir durumun terminal (son) durum olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="status">Kontrol edilecek durum</param>
        /// <returns>Terminal durum ise true</returns>
        bool IsTerminalState(OrderStatus status);

        /// <summary>
        /// Siparişin iptal edilebilir olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="currentStatus">Mevcut sipariş durumu</param>
        /// <returns>İptal edilebilir ise true</returns>
        bool CanCancel(OrderStatus currentStatus);

        /// <summary>
        /// Siparişin iade edilebilir olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="currentStatus">Mevcut sipariş durumu</param>
        /// <returns>İade edilebilir ise true</returns>
        bool CanRefund(OrderStatus currentStatus);

        /// <summary>
        /// Siparişe kurye atanabilir mi kontrol eder.
        /// </summary>
        /// <param name="currentStatus">Mevcut sipariş durumu</param>
        /// <returns>Kurye atanabilirse true</returns>
        bool CanAssignCourier(OrderStatus currentStatus);

        /// <summary>
        /// Sipariş durumunun Türkçe açıklamasını döner.
        /// </summary>
        /// <param name="status">Sipariş durumu</param>
        /// <returns>Türkçe durum açıklaması</returns>
        string GetStatusDescription(OrderStatus status);
    }

    /// <summary>
    /// Sipariş durum geçiş sonucu.
    /// </summary>
    public class OrderTransitionResult
    {
        /// <summary>
        /// Geçiş başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Hata veya bilgi mesajı.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Önceki sipariş durumu.
        /// </summary>
        public OrderStatus? PreviousStatus { get; set; }

        /// <summary>
        /// Yeni sipariş durumu.
        /// </summary>
        public OrderStatus? NewStatus { get; set; }

        /// <summary>
        /// Geçiş zamanı.
        /// </summary>
        public DateTime TransitionTime { get; set; }

        /// <summary>
        /// Hata kodu (varsa).
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// Başarılı sonuç oluşturur.
        /// </summary>
        public static OrderTransitionResult Succeeded(OrderStatus previous, OrderStatus newStatus, string? message = null)
        {
            return new OrderTransitionResult
            {
                Success = true,
                Message = message,
                PreviousStatus = previous,
                NewStatus = newStatus,
                TransitionTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Başarısız sonuç oluşturur.
        /// </summary>
        public static OrderTransitionResult Failed(string message, string? errorCode = null)
        {
            return new OrderTransitionResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                TransitionTime = DateTime.UtcNow
            };
        }
    }
}
