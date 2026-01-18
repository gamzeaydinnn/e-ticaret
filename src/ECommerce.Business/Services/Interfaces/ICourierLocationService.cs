// ==========================================================================
// ICourierLocationService.cs - Kurye Konum Servis Interface'i
// ==========================================================================
// Bu interface, kurye konum takibini yönetir.
// GPS verisi işleme, ETA hesaplama ve anlık konum sorgulama sağlar.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kurye konum servis interface'i.
    /// </summary>
    public interface ICourierLocationService
    {
        // =====================================================================
        // KONUM GÜNCELLEME
        // =====================================================================

        /// <summary>
        /// Kurye konumunu günceller.
        /// </summary>
        Task<bool> UpdateLocationAsync(CourierLocationUpdate update);

        // =====================================================================
        // KONUM SORGULAMA
        // =====================================================================

        /// <summary>
        /// Kuryenin son bilinen konumunu getirir.
        /// </summary>
        Task<CourierLocationInfo?> GetCurrentLocationAsync(int courierId);

        /// <summary>
        /// Aktif kuryelerin konumlarını getirir.
        /// </summary>
        Task<IEnumerable<CourierLocationInfo>> GetActiveCouriersLocationsAsync(int? zoneId = null);

        /// <summary>
        /// Belirtilen konuma yakın kuryeleri getirir.
        /// </summary>
        Task<IEnumerable<Courier>> GetCouriersNearLocationAsync(double latitude, double longitude, double radiusKm);

        // =====================================================================
        // MESAFE VE ETA
        // =====================================================================

        /// <summary>
        /// Kuryenin teslimat noktasına olan mesafesini hesaplar.
        /// </summary>
        Task<double?> CalculateDistanceToDropoffAsync(int courierId, int deliveryTaskId);

        /// <summary>
        /// Tahmini varış süresini hesaplar.
        /// </summary>
        Task<DateTime?> CalculateEtaAsync(int courierId, int deliveryTaskId);

        /// <summary>
        /// Kuryenin teslimat noktasına yeterince yakın olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> IsWithinDeliveryRadiusAsync(int courierId, int deliveryTaskId, double radiusMeters = 100);

        // =====================================================================
        // KONUM GEÇMİŞİ
        // =====================================================================

        /// <summary>
        /// Kuryenin konum geçmişini getirir.
        /// </summary>
        Task<CourierLocationHistory> GetLocationHistoryAsync(int courierId, DateTime startTime, DateTime endTime);
    }

    // =========================================================================
    // DTO'LAR
    // =========================================================================

    /// <summary>
    /// Kurye konum güncelleme isteği.
    /// </summary>
    public class CourierLocationUpdate
    {
        public int CourierId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    /// <summary>
    /// Kurye konum bilgisi.
    /// </summary>
    public class CourierLocationInfo
    {
        public int CourierId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Accuracy { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public DateTime LastUpdated { get; set; }
        public string? VehicleType { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// Kurye konum geçmişi.
    /// </summary>
    public class CourierLocationHistory
    {
        public int CourierId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<LocationPoint> Locations { get; set; } = new();
        public double TotalDistanceKm { get; set; }
    }

    /// <summary>
    /// Konum noktası.
    /// </summary>
    public class LocationPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public double? Speed { get; set; }
    }
}
