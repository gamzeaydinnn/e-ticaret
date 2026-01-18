// ==========================================================================
// DeliveryZone.cs - Teslimat Bölgesi Entity
// ==========================================================================
// Bu entity, teslimat yapılabilecek coğrafi bölgeleri tanımlar.
// Kurye atama algoritması bölge eşleştirmesi yapar.
// Fiyatlandırma, SLA ve operasyonel planlama için kullanılır.
//
// Coğrafi Veri Yapısı:
// - Polygon: GeoJSON formatında bölge sınırları
// - Center: Bölge merkez noktası (hızlı mesafe hesabı için)
// - Geofencing için koordinat listeleri kullanılır
// ==========================================================================

using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Teslimat bölgesi entity'si.
    /// Şehir, ilçe veya özel tanımlı bölgeleri temsil eder.
    /// Kurye ataması ve teslimat planlaması için kritik.
    /// </summary>
    public class DeliveryZone : BaseEntity
    {
        // =====================================================================
        // TEMEL BİLGİLER
        // =====================================================================

        /// <summary>
        /// Bölge adı.
        /// Örn: "Kadıköy", "Şişli Merkez", "Anadolu Yakası"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Bölge kodu.
        /// Kısa referans kodu. Örn: "KDK", "SSL", "AY"
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Bölge açıklaması.
        /// Detaylı bilgi ve notlar.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Bölge tipi.
        /// City, District, Neighborhood, Custom
        /// </summary>
        public string ZoneType { get; set; } = "District";

        // =====================================================================
        // COĞRAFİ BİLGİLER
        // =====================================================================

        /// <summary>
        /// İl adı.
        /// Hiyerarşik filtreleme için.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// İlçe adı.
        /// Hiyerarşik filtreleme için.
        /// </summary>
        public string? District { get; set; }

        /// <summary>
        /// Bölge merkez noktası enlem.
        /// Hızlı mesafe hesabı için.
        /// </summary>
        public double? CenterLatitude { get; set; }

        /// <summary>
        /// Bölge merkez noktası boylam.
        /// Hızlı mesafe hesabı için.
        /// </summary>
        public double? CenterLongitude { get; set; }

        /// <summary>
        /// Bölge sınırları (GeoJSON Polygon).
        /// Geofencing için koordinat dizisi.
        /// Format: [[lng1,lat1],[lng2,lat2],...]
        /// </summary>
        public string? BoundaryPolygonJson { get; set; }

        /// <summary>
        /// Yaklaşık bölge alanı (km²).
        /// Kapasite planlaması için.
        /// </summary>
        public double? AreaSquareKm { get; set; }

        /// <summary>
        /// Bölge yarıçapı (km).
        /// Merkez noktasından itibaren kapsama alanı.
        /// </summary>
        public double? RadiusKm { get; set; }

        // =====================================================================
        // OPERASYONEL AYARLAR
        // =====================================================================

        /// <summary>
        /// Bölge aktif mi?
        /// False ise bu bölgeye teslimat kabul edilmez.
        /// </summary>
        public new bool IsActive { get; set; } = true;

        /// <summary>
        /// Teslimat yapılabilir mi?
        /// Geçici olarak kapatılabilir (hava durumu, özel gün vb.)
        /// </summary>
        public bool IsDeliverable { get; set; } = true;

        /// <summary>
        /// Devre dışı bırakılma sebebi.
        /// IsDeliverable = false olduğunda açıklama.
        /// </summary>
        public string? DisabledReason { get; set; }

        /// <summary>
        /// Öncelik sırası.
        /// Düşük değer = yüksek öncelik.
        /// Atama algoritmasında kullanılır.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Minimum kurye sayısı.
        /// Bu bölgede en az bu kadar kurye online olmalı.
        /// </summary>
        public int MinimumCouriers { get; set; } = 1;

        /// <summary>
        /// Maksimum kurye sayısı.
        /// Bu bölgeye en fazla bu kadar kurye atanabilir.
        /// </summary>
        public int MaximumCouriers { get; set; } = 100;

        // =====================================================================
        // FİYATLANDIRMA
        // =====================================================================

        /// <summary>
        /// Temel teslimat ücreti.
        /// Bu bölgeye teslimat için minimum ücret.
        /// </summary>
        public decimal BaseDeliveryFee { get; set; } = 0;

        /// <summary>
        /// Kilometre başına ek ücret.
        /// Mesafeye göre artan ücret.
        /// </summary>
        public decimal PerKmFee { get; set; } = 0;

        /// <summary>
        /// Minimum sipariş tutarı.
        /// Bu tutarın altında teslimat yapılmaz.
        /// </summary>
        public decimal MinimumOrderAmount { get; set; } = 0;

        /// <summary>
        /// Ücretsiz teslimat eşiği.
        /// Bu tutarın üzerinde teslimat ücretsiz.
        /// </summary>
        public decimal FreeDeliveryThreshold { get; set; } = 0;

        /// <summary>
        /// Para birimi.
        /// </summary>
        public string Currency { get; set; } = "TRY";

        // =====================================================================
        // SLA (Service Level Agreement)
        // =====================================================================

        /// <summary>
        /// Hedef teslimat süresi (dakika).
        /// Siparişten teslimata maksimum süre.
        /// </summary>
        public int TargetDeliveryTimeMinutes { get; set; } = 60;

        /// <summary>
        /// Maksimum teslimat süresi (dakika).
        /// SLA ihlali eşiği.
        /// </summary>
        public int MaxDeliveryTimeMinutes { get; set; } = 90;

        /// <summary>
        /// Kurye atama timeout (saniye).
        /// Bu sürede kabul edilmezse reassign.
        /// </summary>
        public int AssignmentTimeoutSeconds { get; set; } = 60;

        // =====================================================================
        // ÇALIŞMA SAATLERİ
        // =====================================================================

        /// <summary>
        /// Teslimat başlangıç saati.
        /// Bu saatten önce teslimat yapılmaz.
        /// </summary>
        public TimeSpan? OperatingHoursStart { get; set; }

        /// <summary>
        /// Teslimat bitiş saati.
        /// Bu saatten sonra teslimat yapılmaz.
        /// </summary>
        public TimeSpan? OperatingHoursEnd { get; set; }

        /// <summary>
        /// Hafta sonu teslimat var mı?
        /// </summary>
        public bool WeekendDeliveryEnabled { get; set; } = true;

        /// <summary>
        /// Çalışma günleri.
        /// Comma-separated: "Monday,Tuesday,..."
        /// </summary>
        public string? OperatingDays { get; set; }

        // =====================================================================
        // ÜST BÖLGE İLİŞKİSİ (Hiyerarşi)
        // =====================================================================

        /// <summary>
        /// Üst bölge ID'si.
        /// Hiyerarşik yapı için (İl > İlçe > Mahalle).
        /// </summary>
        public int? ParentZoneId { get; set; }

        /// <summary>
        /// Üst bölge navigation property.
        /// </summary>
        public virtual DeliveryZone? ParentZone { get; set; }

        /// <summary>
        /// Alt bölgeler koleksiyonu.
        /// </summary>
        public virtual ICollection<DeliveryZone> SubZones { get; set; } 
            = new HashSet<DeliveryZone>();

        // =====================================================================
        // İSTATİSTİKLER (Denormalize - periyodik güncellenir)
        // =====================================================================

        /// <summary>
        /// Aktif kurye sayısı.
        /// Şu an online olan kuryeler.
        /// </summary>
        public int ActiveCourierCount { get; set; } = 0;

        /// <summary>
        /// Bekleyen teslimat sayısı.
        /// Atanmamış görevler.
        /// </summary>
        public int PendingDeliveryCount { get; set; } = 0;

        /// <summary>
        /// Bugünkü teslimat sayısı.
        /// </summary>
        public int TodayDeliveryCount { get; set; } = 0;

        /// <summary>
        /// Ortalama teslimat süresi (dakika).
        /// Son 7 günlük ortalama.
        /// </summary>
        public int AverageDeliveryTimeMinutes { get; set; } = 0;

        /// <summary>
        /// Son güncelleme zamanı (istatistikler).
        /// </summary>
        public DateTime? StatsUpdatedAt { get; set; }

        // =====================================================================
        // NAVİGASYON PROPERTİES
        // =====================================================================

        /// <summary>
        /// Bu bölgeye atanan kuryeler.
        /// CourierZone üzerinden many-to-many.
        /// </summary>
        public virtual ICollection<CourierZone> CourierZones { get; set; } 
            = new HashSet<CourierZone>();

        /// <summary>
        /// Bu bölgedeki teslimat görevleri.
        /// </summary>
        public virtual ICollection<DeliveryTask> DeliveryTasks { get; set; } 
            = new HashSet<DeliveryTask>();
    }
}
