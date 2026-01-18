// ==========================================================================
// CourierZone.cs - Kurye-Bölge İlişki Entity (Join Table)
// ==========================================================================
// Bu entity, kuryeler ile teslimat bölgeleri arasındaki many-to-many
// ilişkiyi yönetir. Hangi kurye hangi bölgelere teslimat yapabilir.
//
// İş Kuralları:
// - Bir kurye birden fazla bölgeye atanabilir
// - Bir bölgeye birden fazla kurye atanabilir
// - Priority ile öncelik sırası belirlenir (düşük = yüksek öncelik)
// - IsPrimary = true olan bölge kuryenin ana çalışma bölgesidir
// ==========================================================================

using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kurye-Bölge ilişki entity'si (Join Table).
    /// Kurye atama algoritmasında bölge eşleştirmesi için kullanılır.
    /// </summary>
    public class CourierZone : BaseEntity
    {
        // =====================================================================
        // İLİŞKİ ALANLARI (Foreign Keys)
        // =====================================================================

        /// <summary>
        /// Kurye ID'si.
        /// </summary>
        public int CourierId { get; set; }

        /// <summary>
        /// Teslimat bölgesi ID'si.
        /// </summary>
        public int DeliveryZoneId { get; set; }

        /// <summary>
        /// ZoneId alias - DeliveryZoneId ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public int ZoneId
        {
            get => DeliveryZoneId;
            set => DeliveryZoneId = value;
        }

        // =====================================================================
        // ATAMA DETAYLARI
        // =====================================================================

        /// <summary>
        /// Bu kurye için birincil bölge mi?
        /// True ise kurye öncelikli olarak bu bölgeye atanır.
        /// Her kuryenin sadece bir primary bölgesi olabilir.
        /// </summary>
        public bool IsPrimary { get; set; } = false;

        /// <summary>
        /// Bölge önceliği.
        /// Düşük değer = yüksek öncelik.
        /// Atama algoritmasında sıralama için kullanılır.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Bölgede aktif mi?
        /// False ise bu bölgeden görev almaz.
        /// Geçici devre dışı bırakma için.
        /// </summary>
        public bool IsActiveInZone { get; set; } = true;

        // =====================================================================
        // ATAMA BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Atama tarihi.
        /// Kurye bu bölgeye ne zaman atandı.
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Atamayı yapan admin ID'si.
        /// Audit için.
        /// </summary>
        public int? AssignedByUserId { get; set; }

        /// <summary>
        /// Son güncelleme yapan admin ID'si.
        /// </summary>
        public int? UpdatedByUserId { get; set; }

        /// <summary>
        /// Atama notu.
        /// Admin tarafından eklenen açıklama.
        /// </summary>
        public string? AssignmentNote { get; set; }

        // =====================================================================
        // PERFORMANS METRİKLERİ (Bölge bazlı)
        // =====================================================================

        /// <summary>
        /// Bu bölgede tamamlanan teslimat sayısı.
        /// </summary>
        public int DeliveriesCompleted { get; set; } = 0;

        /// <summary>
        /// Bu bölgede başarısız teslimat sayısı.
        /// </summary>
        public int DeliveriesFailed { get; set; } = 0;

        /// <summary>
        /// Bu bölgedeki ortalama teslimat süresi (dakika).
        /// </summary>
        public int AverageDeliveryTimeMinutes { get; set; } = 0;

        /// <summary>
        /// Bu bölgedeki ortalama puan.
        /// Bölge bazlı müşteri memnuniyeti.
        /// </summary>
        public decimal AverageRating { get; set; } = 0;

        /// <summary>
        /// Son teslimat tarihi.
        /// Bu bölgede en son ne zaman teslimat yaptı.
        /// </summary>
        public DateTime? LastDeliveryAt { get; set; }

        // =====================================================================
        // KISITLAMALAR
        // =====================================================================

        /// <summary>
        /// Maksimum günlük teslimat sayısı.
        /// 0 = sınırsız.
        /// </summary>
        public int MaxDailyDeliveries { get; set; } = 0;

        /// <summary>
        /// Bugün bu bölgede yapılan teslimat sayısı.
        /// Her gün sıfırlanır.
        /// </summary>
        public int TodayDeliveryCount { get; set; } = 0;

        /// <summary>
        /// Bu bölgede COD teslimat yapabilir mi?
        /// Bazı kuryeler COD yetkisi olmayabilir.
        /// </summary>
        public bool CanHandleCod { get; set; } = true;

        /// <summary>
        /// Bu bölgede express teslimat yapabilir mi?
        /// Deneyimli kuryeler için.
        /// </summary>
        public bool CanHandleExpress { get; set; } = false;

        // =====================================================================
        // NAVİGASYON PROPERTİES
        // =====================================================================

        /// <summary>
        /// İlişkili kurye.
        /// </summary>
        public virtual Courier? Courier { get; set; }

        /// <summary>
        /// İlişkili teslimat bölgesi.
        /// </summary>
        public virtual DeliveryZone? DeliveryZone { get; set; }

        /// <summary>
        /// Atamayı yapan admin.
        /// </summary>
        public virtual User? AssignedByUser { get; set; }

        /// <summary>
        /// Son güncelleyen admin.
        /// </summary>
        public virtual User? UpdatedByUser { get; set; }
    }
}
