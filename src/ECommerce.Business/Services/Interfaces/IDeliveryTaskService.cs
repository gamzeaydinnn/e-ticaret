// ==========================================================================
// IDeliveryTaskService.cs - Teslimat Görevi Servis Interface'i
// ==========================================================================
// Bu interface, teslimat görevlerinin yaşam döngüsünü yönetir.
// State Machine Pattern ile durum geçişleri kontrol edilir.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat görevi servis interface'i.
    /// </summary>
    public interface IDeliveryTaskService
    {
        // =====================================================================
        // OLUŞTURMA
        // =====================================================================

        /// <summary>
        /// Siparişten teslimat görevi oluşturur.
        /// </summary>
        Task<DeliveryTask> CreateFromOrderAsync(int orderId, int createdByUserId);

        /// <summary>
        /// Siparişten teslimat görevi oluşturur (gelişmiş overload).
        /// Öncelik, kuryeye not ve zaman penceresi parametreleriyle.
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="priority">Teslimat önceliği (Normal, High, Urgent)</param>
        /// <param name="notesForCourier">Kurye için özel notlar</param>
        /// <param name="timeWindowStart">Teslimat zaman penceresi başlangıcı</param>
        /// <param name="timeWindowEnd">Teslimat zaman penceresi bitişi</param>
        /// <returns>Oluşturulan teslimat görevi</returns>
        Task<DeliveryTask?> CreateFromOrderAsync(
            int orderId, 
            string? priority = null, 
            string? notesForCourier = null, 
            DateTime? timeWindowStart = null, 
            DateTime? timeWindowEnd = null);

        /// <summary>
        /// Manuel teslimat görevi oluşturur.
        /// </summary>
        Task<DeliveryTask> CreateManualAsync(CreateDeliveryTaskRequest request, int createdByUserId);

        // =====================================================================
        // DURUM GÜNCELLEMELERİ
        // =====================================================================

        /// <summary>
        /// Teslimatı kuryeye atar.
        /// </summary>
        Task<DeliveryTask> AssignAsync(int deliveryTaskId, int courierId, int assignedByUserId);

        /// <summary>
        /// Kurye teslimatı kabul eder.
        /// </summary>
        Task<DeliveryTask> AcceptAsync(int deliveryTaskId, int courierId);

        /// <summary>
        /// Kurye teslimatı reddeder.
        /// </summary>
        Task<DeliveryTask> RejectAsync(int deliveryTaskId, int courierId, string reason);

        /// <summary>
        /// Teslimat durumunu günceller.
        /// </summary>
        Task<DeliveryTask> UpdateStatusAsync(int deliveryTaskId, DeliveryStatus newStatus, int actorId, ActorType actorType);

        /// <summary>
        /// Teslimatı tamamlar.
        /// </summary>
        Task<DeliveryTask> CompleteAsync(int deliveryTaskId, CompleteDeliveryRequest request, int courierId);

        /// <summary>
        /// Teslimatı başarısız olarak işaretler.
        /// </summary>
        Task<DeliveryTask> FailAsync(int deliveryTaskId, FailDeliveryRequest request, int courierId);

        /// <summary>
        /// Teslimatı iptal eder.
        /// </summary>
        Task<DeliveryTask> CancelAsync(int deliveryTaskId, string reason, int cancelledByUserId);

        /// <summary>
        /// Teslimatı başka kuryeye yeniden atar.
        /// </summary>
        Task<DeliveryTask> ReassignAsync(int deliveryTaskId, int newCourierId, string reason, int reassignedByUserId);

        // =====================================================================
        // SORGULAMA
        // =====================================================================

        /// <summary>
        /// ID ile teslimat görevi getirir.
        /// </summary>
        Task<DeliveryTask?> GetByIdAsync(int deliveryTaskId);

        /// <summary>
        /// Sipariş ID ile teslimat görevi getirir.
        /// </summary>
        Task<DeliveryTask?> GetByOrderIdAsync(int orderId);

        /// <summary>
        /// Kuryeye atanmış görevleri getirir.
        /// </summary>
        Task<IEnumerable<DeliveryTask>> GetByCourierAsync(int courierId, DateTime? date = null);

        /// <summary>
        /// Duruma göre görevleri getirir.
        /// </summary>
        Task<IEnumerable<DeliveryTask>> GetByStatusAsync(DeliveryStatus status, int? zoneId = null);

        /// <summary>
        /// Atama bekleyen görevleri getirir.
        /// </summary>
        Task<IEnumerable<DeliveryTask>> GetPendingAssignmentAsync(int? zoneId = null);

        /// <summary>
        /// Tarih aralığına göre görevleri getirir.
        /// </summary>
        Task<List<DeliveryTask>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Kuryeye atanmış görevleri duruma göre getirir.
        /// </summary>
        Task<List<DeliveryTask>> GetByCourierIdAsync(int courierId, DeliveryStatus[]? statuses = null);

        /// <summary>
        /// Bekleyen görev sayısını getirir.
        /// </summary>
        Task<int> GetPendingCountAsync();

        /// <summary>
        /// Görevleri arar.
        /// </summary>
        Task<IEnumerable<DeliveryTask>> SearchAsync(DeliveryTaskSearchRequest request);

        /// <summary>
        /// Duruma göre sayım yapar.
        /// </summary>
        Task<int> GetCountByStatusAsync(DeliveryStatus status, DateTime? date = null);

        // =====================================================================
        // ÖNCELIK
        // =====================================================================

        /// <summary>
        /// Önceliği günceller.
        /// </summary>
        Task<DeliveryTask> UpdatePriorityAsync(int deliveryTaskId, DeliveryPriority newPriority, int updatedByUserId);

        // =====================================================================
        // DURUM GEÇİŞ VALİDASYONU
        // =====================================================================

        /// <summary>
        /// Durum geçişinin geçerli olup olmadığını kontrol eder.
        /// </summary>
        bool CanTransitionTo(DeliveryStatus currentStatus, DeliveryStatus newStatus);

        /// <summary>
        /// İzin verilen durum geçişlerini döndürür.
        /// </summary>
        IEnumerable<DeliveryStatus> GetAllowedTransitions(DeliveryStatus currentStatus);
    }

    // =========================================================================
    // REQUEST DTO'LAR
    // =========================================================================

    /// <summary>
    /// Manuel teslimat oluşturma isteği.
    /// </summary>
    public class CreateDeliveryTaskRequest
    {
        public int? OrderId { get; set; }
        public DeliveryPriority? Priority { get; set; }

        // Pickup
        public string PickupAddressLine { get; set; } = string.Empty;
        public string PickupCity { get; set; } = string.Empty;
        public string? PickupDistrict { get; set; }
        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
        public string? PickupContactName { get; set; }
        public string? PickupContactPhone { get; set; }

        // Dropoff
        public string DropoffAddressLine { get; set; } = string.Empty;
        public string DropoffCity { get; set; } = string.Empty;
        public string? DropoffDistrict { get; set; }
        public double? DropoffLatitude { get; set; }
        public double? DropoffLongitude { get; set; }

        // Customer
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerNotes { get; set; }

        // COD
        public bool IsCod { get; set; }
        public decimal CodAmount { get; set; }

        // Timing
        public DateTime? ScheduledPickupTime { get; set; }
        public DateTime? ScheduledDeliveryTime { get; set; }
    }

    /// <summary>
    /// Teslimat tamamlama isteği.
    /// </summary>
    public class CompleteDeliveryRequest
    {
        public DeliveryProofMethod? ProofMethod { get; set; }
        public string? PhotoUrl { get; set; }
        public string? SignatureUrl { get; set; }
        public string? OtpCode { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverRelation { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public decimal? CodCollected { get; set; }
        
        // Ek alanlar - kurye paneli uyumu
        public string? RecipientName { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Teslimat başarısızlık isteği.
    /// </summary>
    public class FailDeliveryRequest
    {
        public DeliveryFailureReason Reason { get; set; }
        public string? Note { get; set; }
        public string? PhotoUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Ek alanlar - kurye paneli uyumu
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Teslimat arama isteği.
    /// </summary>
    public class DeliveryTaskSearchRequest
    {
        public DeliveryStatus? Status { get; set; }
        public int? CourierId { get; set; }
        public int? ZoneId { get; set; }
        public DeliveryPriority? Priority { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? CustomerPhone { get; set; }
        public string? OrderNumber { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
