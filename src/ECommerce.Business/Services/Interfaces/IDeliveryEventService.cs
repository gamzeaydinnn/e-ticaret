// ==========================================================================
// IDeliveryEventService.cs - Teslimat Olay Servis Interface'i
// ==========================================================================
// Bu interface, teslimat sürecindeki tüm olayları kaydeder.
// Audit Trail Pattern ile tam izlenebilirlik sağlar.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat olay servis interface'i.
    /// </summary>
    public interface IDeliveryEventService
    {
        // =====================================================================
        // OLAY KAYDETME
        // =====================================================================

        /// <summary>
        /// Olay kaydeder.
        /// </summary>
        Task<DeliveryEvent> LogEventAsync(DeliveryEventRequest request);

        /// <summary>
        /// Durum değişikliği kaydeder.
        /// </summary>
        Task<DeliveryEvent> LogStatusChangeAsync(
            int deliveryTaskId,
            DeliveryStatus oldStatus,
            DeliveryStatus newStatus,
            int actorId,
            ActorType actorType,
            string? description = null);

        // =====================================================================
        // OLAY SORGULAMA
        // =====================================================================

        /// <summary>
        /// Teslimat ID'sine göre olayları getirir.
        /// </summary>
        Task<IEnumerable<DeliveryEvent>> GetByDeliveryTaskAsync(int deliveryTaskId);

        /// <summary>
        /// Aktör ID'sine göre olayları getirir.
        /// </summary>
        Task<IEnumerable<DeliveryEvent>> GetByActorAsync(
            int actorId, 
            ActorType actorType,
            DateTime? startDate = null,
            DateTime? endDate = null);

        /// <summary>
        /// Son olayları getirir.
        /// </summary>
        Task<IEnumerable<DeliveryEvent>> GetRecentEventsAsync(
            int count = 50,
            DeliveryEventType? eventType = null);

        // =====================================================================
        // ÖZET
        // =====================================================================

        /// <summary>
        /// Teslimat görev olay özetini getirir.
        /// </summary>
        Task<DeliveryEventSummary> GetTaskEventSummaryAsync(int deliveryTaskId);
    }

    // =========================================================================
    // DTO'LAR
    // =========================================================================

    /// <summary>
    /// Olay kayıt isteği.
    /// </summary>
    public class DeliveryEventRequest
    {
        public int DeliveryTaskId { get; set; }
        public DeliveryEventType EventType { get; set; }
        public ActorType ActorType { get; set; }
        public int ActorId { get; set; }
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? OldStateJson { get; set; }
        public string? NewStateJson { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    /// <summary>
    /// Teslimat olay özeti.
    /// </summary>
    public class DeliveryEventSummary
    {
        public int DeliveryTaskId { get; set; }
        public int TotalEvents { get; set; }
        public DateTime? FirstEventTime { get; set; }
        public DateTime? LastEventTime { get; set; }
        public DeliveryStatus CurrentStatus { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string DropoffAddress { get; set; } = string.Empty;
    }
}
