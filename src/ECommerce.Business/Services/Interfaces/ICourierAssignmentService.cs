// ==========================================================================
// ICourierAssignmentService.cs - Kurye Atama Servis Interface'i
// ==========================================================================
// Bu interface, kurye atama algoritmasını yönetir.
// Manuel ve otomatik atama, uygunluk skoru hesaplama.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kurye atama servis interface'i.
    /// </summary>
    public interface ICourierAssignmentService
    {
        // =====================================================================
        // EN UYGUN KURYE BULMA
        // =====================================================================

        /// <summary>
        /// Teslimat için en uygun kuryeyi bulur.
        /// </summary>
        Task<Courier?> FindBestCourierAsync(int deliveryTaskId);

        // =====================================================================
        // ATAMA İŞLEMLERİ
        // =====================================================================

        /// <summary>
        /// Otomatik kurye ataması yapar.
        /// </summary>
        Task<CourierAssignmentResult> AutoAssignAsync(int deliveryTaskId, int assignedByUserId);

        /// <summary>
        /// Manuel kurye ataması yapar.
        /// </summary>
        Task<CourierAssignmentResult> ManualAssignAsync(int deliveryTaskId, int courierId, int assignedByUserId);

        /// <summary>
        /// Toplu otomatik atama yapar.
        /// </summary>
        Task<int> BatchAutoAssignAsync(IEnumerable<int> deliveryTaskIds, int assignedByUserId);

        // =====================================================================
        // MÜSAİT KURYE LİSTESİ
        // =====================================================================

        /// <summary>
        /// Müsait kuryeleri getirir.
        /// </summary>
        Task<IEnumerable<Courier>> GetAvailableCouriersAsync(int? zoneId = null);

        /// <summary>
        /// Kuryeleri skorlarına göre getirir.
        /// </summary>
        Task<IEnumerable<CourierAssignmentScore>> GetCourierScoresForTaskAsync(int deliveryTaskId);

        /// <summary>
        /// Kurye uygunluğunu kontrol eder.
        /// </summary>
        Task<bool> IsCourierAvailableAsync(int courierId, int? deliveryTaskId = null);

        /// <summary>
        /// Kurye iş yükü bilgisini getirir.
        /// </summary>
        Task<CourierWorkloadInfo> GetCourierWorkloadAsync(int courierId);

        /// <summary>
        /// Müsait kurye sayısını getirir.
        /// </summary>
        Task<int> GetAvailableCouriersCountAsync();

        /// <summary>
        /// Toplam kurye sayısını getirir.
        /// </summary>
        Task<int> GetTotalCouriersCountAsync();
    }

    // =========================================================================
    // DTO'LAR
    // =========================================================================

    /// <summary>
    /// Kurye atama sonucu.
    /// </summary>
    public class CourierAssignmentResult
    {
        public bool Success { get; set; }
        public int? AssignedCourierId { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Kurye atama skoru.
    /// </summary>
    public class CourierAssignmentScore
    {
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public double TotalScore { get; set; }
        public double DistanceScore { get; set; }
        public double CapacityScore { get; set; }
        public double RatingScore { get; set; }
        public double ZoneScore { get; set; }
        public double AvailabilityScore { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Kurye iş yükü bilgisi.
    /// </summary>
    public class CourierWorkloadInfo
    {
        public int CourierId { get; set; }
        public int CurrentTaskCount { get; set; }
        public int MaxCapacity { get; set; }
        public double UtilizationPercent { get; set; }
        public int PendingPickups { get; set; }
        public int InTransitCount { get; set; }
        public DateTime? EstimatedCompletionTime { get; set; }
    }
}
