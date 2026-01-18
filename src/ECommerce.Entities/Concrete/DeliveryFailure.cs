// ==========================================================================
// DeliveryFailure.cs - Teslimat Başarısızlık Kaydı Entity
// ==========================================================================
// Bu entity, başarısız teslimat denemelerini kaydeder.
// Her başarısız deneme için sebep, not ve opsiyonel fotoğraf saklanır.
// Raporlama, analiz ve operasyonel iyileştirme için kritik veri sağlar.
//
// İş Kuralları:
// - Her başarısız teslimat için failure_reason_code zorunludur
// - "Other" seçildiğinde failure_note zorunlu olmalıdır
// - Fotoğraf eklenmesi bazı sebep kodları için zorunlu tutulabilir
// ==========================================================================

using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Teslimat başarısızlık kaydı entity'si.
    /// Her başarısız teslimat denemesi için ayrı bir kayıt tutulur.
    /// Operasyonel analiz ve müşteri iletişimi için kullanılır.
    /// </summary>
    public class DeliveryFailure : BaseEntity
    {
        // =====================================================================
        // İLİŞKİ ALANLARI
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi ID'si.
        /// Her failure kaydı bir DeliveryTask'a bağlıdır.
        /// </summary>
        public int DeliveryTaskId { get; set; }

        /// <summary>
        /// Başarısız teslimatı raporlayan kurye ID'si.
        /// Audit ve sorumluluk takibi için zorunlu.
        /// </summary>
        public int ReportedByCourierId { get; set; }

        // =====================================================================
        // BAŞARISIZLIK DETAYLARI
        // =====================================================================

        /// <summary>
        /// Başarısızlık sebep kodu.
        /// Enum değeri, raporlama ve kategorize etme için kullanılır.
        /// Zorunlu alan - her failure için seçilmelidir.
        /// </summary>
        public DeliveryFailureReason ReasonCode { get; set; }

        /// <summary>
        /// FailureReason alias - ReasonCode ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public DeliveryFailureReason FailureReason
        {
            get => ReasonCode;
            set => ReasonCode = value;
        }

        /// <summary>
        /// Başarısızlık notu.
        /// Kurye tarafından girilen açıklama.
        /// ReasonCode = Other olduğunda zorunludur.
        /// </summary>
        public string? FailureNote { get; set; }

        /// <summary>
        /// Ek detay JSON formatında.
        /// Esnek veri yapısı için (örn: müşteri tepkisi, çevre koşulları).
        /// </summary>
        public string? AdditionalDetailsJson { get; set; }

        // =====================================================================
        // FOTOĞRAF KANITI
        // =====================================================================

        /// <summary>
        /// Başarısızlık fotoğrafı URL'i.
        /// Kapı önü, kapalı dükkan vb. durumları belgelemek için.
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Fotoğraf thumbnail URL'i.
        /// Liste görünümlerinde hızlı yükleme için.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Fotoğrafın çekildiği konum enlemi.
        /// GPS doğrulaması için kullanılır.
        /// </summary>
        public double? PhotoLatitude { get; set; }

        /// <summary>
        /// Fotoğrafın çekildiği konum boylamı.
        /// GPS doğrulaması için kullanılır.
        /// </summary>
        public double? PhotoLongitude { get; set; }

        // =====================================================================
        // İLETİŞİM BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Müşteriyi arama deneme sayısı.
        /// Kurye kaç kez aradı.
        /// </summary>
        public int CustomerCallAttempts { get; set; } = 0;

        /// <summary>
        /// Müşteriye ulaşıldı mı?
        /// Telefon görüşmesi yapıldı mı.
        /// </summary>
        public bool CustomerContacted { get; set; } = false;

        /// <summary>
        /// Müşterinin verdiği yanıt.
        /// "Yarın tekrar gelin", "İptal edin" gibi.
        /// </summary>
        public string? CustomerResponse { get; set; }

        // =====================================================================
        // YENİDEN DENEME BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Yeniden deneme talep edildi mi?
        /// Kurye veya müşteri tarafından.
        /// </summary>
        public bool RetryRequested { get; set; } = false;

        /// <summary>
        /// Önerilen yeniden deneme zamanı.
        /// Kurye veya müşterinin önerdiği zaman.
        /// </summary>
        public DateTime? SuggestedRetryTime { get; set; }

        /// <summary>
        /// Yeniden deneme notu.
        /// "Müşteri saat 18:00'den sonra evde olacak" gibi.
        /// </summary>
        public string? RetryNote { get; set; }

        // =====================================================================
        // ZAMAN VE KONUM BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Başarısızlık raporlanma zamanı.
        /// Kurye panelinden bildirildiği an.
        /// </summary>
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Raporlandığı konum enlemi.
        /// Kuryenin bulunduğu konum.
        /// </summary>
        public double? ReportedLatitude { get; set; }

        /// <summary>
        /// Raporlandığı konum boylamı.
        /// Kuryenin bulunduğu konum.
        /// </summary>
        public double? ReportedLongitude { get; set; }

        /// <summary>
        /// Latitude alias - ReportedLatitude ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public double? Latitude
        {
            get => ReportedLatitude;
            set => ReportedLatitude = value;
        }

        /// <summary>
        /// Longitude alias - ReportedLongitude ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public double? Longitude
        {
            get => ReportedLongitude;
            set => ReportedLongitude = value;
        }

        /// <summary>
        /// FailedAt alias - ReportedAt ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public DateTime FailedAt
        {
            get => ReportedAt;
            set => ReportedAt = value;
        }

        /// <summary>
        /// Teslimat adresine olan mesafe (metre).
        /// Kurye gerçekten adrese gitti mi kontrolü.
        /// </summary>
        public double? DistanceToDropoffMeters { get; set; }

        // =====================================================================
        // ADMİN İŞLEMLERİ
        // =====================================================================

        /// <summary>
        /// Admin tarafından incelendi mi?
        /// Manuel review yapıldı mı.
        /// </summary>
        public bool ReviewedByAdmin { get; set; } = false;

        /// <summary>
        /// İnceleyen admin ID'si.
        /// </summary>
        public int? ReviewedByUserId { get; set; }

        /// <summary>
        /// Admin inceleme zamanı.
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Admin notu.
        /// İnceleme sırasında eklenen not.
        /// </summary>
        public string? AdminNote { get; set; }

        /// <summary>
        /// Admin kararı.
        /// "Yeniden ata", "İade başlat", "İptal et" gibi.
        /// </summary>
        public string? AdminDecision { get; set; }

        // =====================================================================
        // NAVİGASYON PROPERTİES
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi.
        /// </summary>
        public virtual DeliveryTask? DeliveryTask { get; set; }

        /// <summary>
        /// Başarısızlığı raporlayan kurye.
        /// </summary>
        public virtual Courier? ReportedByCourier { get; set; }

        /// <summary>
        /// İnceleyen admin kullanıcı.
        /// </summary>
        public virtual User? ReviewedByUser { get; set; }
    }
}
