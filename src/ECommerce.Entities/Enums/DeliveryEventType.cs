// ==========================================================================
// DeliveryEventType.cs - Teslimat Olay Tipleri (Audit Trail)
// ==========================================================================
// Bu enum, teslimat görevlerinde gerçekleşen tüm olay tiplerini tanımlar.
// Her önemli aksiyon audit log'a yazılır ve bu enum ile kategorize edilir.
// ==========================================================================

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Teslimat olayı tipleri.
    /// Her bir değer, DeliveryEvent audit log tablosunda event_type olarak kullanılır.
    /// </summary>
    public enum DeliveryEventType
    {
        // ===== OLUŞTURMA OLAYLARI =====

        /// <summary>
        /// Teslimat görevi oluşturuldu.
        /// Sipariş "READY_FOR_DISPATCH" olduğunda tetiklenir.
        /// </summary>
        Created = 0,

        // ===== ATAMA OLAYLARI =====

        /// <summary>
        /// Kuryeye atandı.
        /// Manuel veya otomatik atama sonrası tetiklenir.
        /// </summary>
        Assigned = 10,

        /// <summary>
        /// Kurye tarafından kabul edildi.
        /// Kurye panelinden "Kabul Et" tıklandığında tetiklenir.
        /// </summary>
        Accepted = 11,

        /// <summary>
        /// Başka kuryeye yeniden atandı.
        /// Timeout, iptal veya manuel reassign sonrası tetiklenir.
        /// </summary>
        Reassigned = 12,

        /// <summary>
        /// Kurye atama timeout oldu.
        /// Belirlenen sürede kurye kabul etmediğinde tetiklenir.
        /// </summary>
        AssignmentTimeout = 13,

        // ===== DURUM DEĞİŞİKLİĞİ OLAYLARI =====

        /// <summary>
        /// Paket teslim alındı (picked up).
        /// Kurye depo/mağazadan paketi aldığında tetiklenir.
        /// </summary>
        PickedUp = 20,

        /// <summary>
        /// Teslimat yolda (in transit).
        /// Kurye müşteriye doğru hareket etmeye başladığında tetiklenir.
        /// </summary>
        InTransit = 21,

        /// <summary>
        /// Teslimat tamamlandı.
        /// POD alındığında ve teslimat onaylandığında tetiklenir.
        /// </summary>
        Delivered = 22,

        /// <summary>
        /// Teslimat başarısız oldu.
        /// Kurye teslimatı tamamlayamadığında tetiklenir.
        /// </summary>
        Failed = 23,

        /// <summary>
        /// Teslimat görevi iptal edildi.
        /// Sipariş iptali veya diğer sebeplerle tetiklenir.
        /// </summary>
        Cancelled = 24,

        // ===== GÜNCELLEME OLAYLARI =====

        /// <summary>
        /// Öncelik değiştirildi.
        /// Admin tarafından görev önceliği güncellendiğinde tetiklenir.
        /// </summary>
        PriorityChanged = 30,

        /// <summary>
        /// Zaman penceresi değiştirildi.
        /// Teslimat zamanı güncellendiğinde tetiklenir.
        /// </summary>
        TimeWindowChanged = 31,

        /// <summary>
        /// Notlar güncellendi.
        /// Kurye veya admin notları değiştirdiğinde tetiklenir.
        /// </summary>
        NotesUpdated = 32,

        /// <summary>
        /// COD tutarı güncellendi.
        /// Kapıda ödeme tutarı değiştirildiğinde tetiklenir.
        /// </summary>
        CodAmountUpdated = 33,

        // ===== POD OLAYLARI =====

        /// <summary>
        /// POD fotoğrafı yüklendi.
        /// Kurye teslimat fotoğrafı çektiğinde tetiklenir.
        /// </summary>
        PodPhotoUploaded = 40,

        /// <summary>
        /// OTP doğrulandı.
        /// Müşteri OTP kodu başarıyla girildiğinde tetiklenir.
        /// </summary>
        OtpVerified = 41,

        /// <summary>
        /// İmza alındı.
        /// Müşteri dijital imza attığında tetiklenir.
        /// </summary>
        SignatureCaptured = 42,

        // ===== KONUM OLAYLARI =====

        /// <summary>
        /// Kurye konumu güncellendi.
        /// GPS verisi alındığında tetiklenir.
        /// Not: Bu event yüksek hacimli olabilir, ayrı tablo düşünülebilir.
        /// </summary>
        LocationUpdated = 50,

        /// <summary>
        /// ETA (Tahmini Varış Süresi) güncellendi.
        /// Konum değişikliği veya trafik durumuna göre tetiklenir.
        /// </summary>
        EtaUpdated = 51,

        // ===== BİLDİRİM OLAYLARI =====

        /// <summary>
        /// Müşteriye bildirim gönderildi.
        /// SMS/Push notification gönderildiğinde tetiklenir.
        /// </summary>
        CustomerNotified = 60,

        /// <summary>
        /// Kuryeye bildirim gönderildi.
        /// Push notification/SMS gönderildiğinde tetiklenir.
        /// </summary>
        CourierNotified = 61,

        /// <summary>
        /// Admin'e bildirim gönderildi.
        /// Kritik olaylar için admin bilgilendirildiğinde tetiklenir.
        /// </summary>
        AdminNotified = 62,

        // ===== EK DURUM OLAYLARI =====

        /// <summary>
        /// Kurye teslimatı reddetti.
        /// Atanan kurye görevi kabul etmediğinde tetiklenir.
        /// </summary>
        Rejected = 70,

        /// <summary>
        /// Not eklendi.
        /// Kurye veya admin teslimat görevine not eklediğinde tetiklenir.
        /// </summary>
        NoteAdded = 71,

        /// <summary>
        /// Durum değişikliği.
        /// Genel durum değişikliği olayı için kullanılır.
        /// </summary>
        StatusChanged = 72
    }
}
