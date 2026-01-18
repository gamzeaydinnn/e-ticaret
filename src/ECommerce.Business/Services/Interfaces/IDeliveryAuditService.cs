// ==========================================================================
// IDeliveryAuditService.cs - Teslimat Audit Servisi Interface
// ==========================================================================
// Tüm teslimat aksiyonlarının detaylı loglanması için interface.
// Güvenlik, uyumluluk ve sorun giderme için kritik.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Teslimat audit log servisi interface'i.
    /// 
    /// Loglanan Aksiyonlar:
    /// ────────────────────
    /// - Teslimat oluşturma
    /// - Kurye atama/yeniden atama
    /// - Durum değişiklikleri (Accept, PickUp, Deliver, Fail, Cancel)
    /// - POD yükleme
    /// - Konum güncelleme (opsiyonel, yüksek frekanslı)
    /// - Timeout ve otomatik işlemler
    /// 
    /// Güvenlik:
    /// ─────────
    /// - Kim, ne zaman, ne yaptı kaydı
    /// - Eski ve yeni değer karşılaştırması
    /// - IP adresi ve user agent
    /// </summary>
    public interface IDeliveryAuditService
    {
        /// <summary>
        /// Teslimat oluşturulduğunda log kaydı oluşturur.
        /// </summary>
        Task LogDeliveryCreatedAsync(DeliveryAuditContext context);

        /// <summary>
        /// Kurye atandığında log kaydı oluşturur.
        /// </summary>
        Task LogCourierAssignedAsync(DeliveryAuditContext context);

        /// <summary>
        /// Kurye yeniden atandığında log kaydı oluşturur.
        /// </summary>
        Task LogCourierReassignedAsync(DeliveryAuditContext context);

        /// <summary>
        /// Teslimat durumu değiştiğinde log kaydı oluşturur.
        /// </summary>
        Task LogStatusChangedAsync(DeliveryAuditContext context);

        /// <summary>
        /// POD yüklendiğinde log kaydı oluşturur.
        /// </summary>
        Task LogPodUploadedAsync(DeliveryAuditContext context);

        /// <summary>
        /// Teslimat başarısız olduğunda log kaydı oluşturur.
        /// </summary>
        Task LogDeliveryFailedAsync(DeliveryAuditContext context);

        /// <summary>
        /// Teslimat iptal edildiğinde log kaydı oluşturur.
        /// </summary>
        Task LogDeliveryCancelledAsync(DeliveryAuditContext context);

        /// <summary>
        /// Genel amaçlı audit log kaydı oluşturur.
        /// </summary>
        Task LogAsync(DeliveryAuditContext context);

        /// <summary>
        /// Belirli bir teslimat için tüm audit loglarını getirir.
        /// </summary>
        Task<System.Collections.Generic.List<DeliveryAuditLogDto>> GetDeliveryAuditLogsAsync(int deliveryTaskId);

        /// <summary>
        /// Belirli bir kurye için audit loglarını getirir.
        /// </summary>
        Task<System.Collections.Generic.List<DeliveryAuditLogDto>> GetCourierAuditLogsAsync(int courierId, DateTime? startDate = null, DateTime? endDate = null);
    }

    /// <summary>
    /// Audit log context - Log oluşturmak için gereken tüm bilgiler.
    /// </summary>
    public class DeliveryAuditContext
    {
        /// <summary>
        /// Teslimat görevi ID'si.
        /// </summary>
        public int DeliveryTaskId { get; set; }

        /// <summary>
        /// Sipariş ID'si (varsa).
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Kurye ID'si (varsa).
        /// </summary>
        public int? CourierId { get; set; }

        /// <summary>
        /// İşlemi yapan aktör tipi.
        /// </summary>
        public AuditActorType ActorType { get; set; }

        /// <summary>
        /// İşlemi yapan aktör ID'si.
        /// </summary>
        public int? ActorId { get; set; }

        /// <summary>
        /// İşlemi yapan aktör adı.
        /// </summary>
        public string? ActorName { get; set; }

        /// <summary>
        /// Olay tipi.
        /// </summary>
        public DeliveryAuditEventType EventType { get; set; }

        /// <summary>
        /// Olay açıklaması.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Eski değer (JSON formatında).
        /// </summary>
        public string? OldValue { get; set; }

        /// <summary>
        /// Yeni değer (JSON formatında).
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// İstek IP adresi.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent bilgisi.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Ek metadata (JSON).
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Olay zamanı.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Audit log aktör tipleri.
    /// </summary>
    public enum AuditActorType
    {
        /// <summary>Bilinmeyen/Sistem</summary>
        Unknown = 0,

        /// <summary>Admin kullanıcı</summary>
        Admin = 1,

        /// <summary>Kurye</summary>
        Courier = 2,

        /// <summary>Müşteri</summary>
        Customer = 3,

        /// <summary>Sistem (otomatik işlem)</summary>
        System = 4,

        /// <summary>API (external)</summary>
        Api = 5
    }

    /// <summary>
    /// Teslimat audit olay tipleri.
    /// </summary>
    public enum DeliveryAuditEventType
    {
        /// <summary>Teslimat görevi oluşturuldu</summary>
        Created = 1,

        /// <summary>Kurye atandı</summary>
        CourierAssigned = 2,

        /// <summary>Kurye yeniden atandı</summary>
        CourierReassigned = 3,

        /// <summary>Kurye görevi kabul etti</summary>
        Accepted = 4,

        /// <summary>Paket teslim alındı</summary>
        PickedUp = 5,

        /// <summary>Kurye yolda</summary>
        InTransit = 6,

        /// <summary>Teslim edildi</summary>
        Delivered = 7,

        /// <summary>Teslimat başarısız</summary>
        Failed = 8,

        /// <summary>Teslimat iptal edildi</summary>
        Cancelled = 9,

        /// <summary>POD yüklendi (fotoğraf)</summary>
        PodPhotoUploaded = 10,

        /// <summary>POD OTP doğrulandı</summary>
        PodOtpVerified = 11,

        /// <summary>POD imza alındı</summary>
        PodSignatureCaptured = 12,

        /// <summary>Konum güncellendi</summary>
        LocationUpdated = 13,

        /// <summary>ETA güncellendi</summary>
        EtaUpdated = 14,

        /// <summary>Notlar güncellendi</summary>
        NotesUpdated = 15,

        /// <summary>Öncelik değiştirildi</summary>
        PriorityChanged = 16,

        /// <summary>Timeout - Otomatik reassign</summary>
        TimeoutReassign = 17,

        /// <summary>Kurye offline oldu</summary>
        CourierWentOffline = 18,

        /// <summary>Müşteri ile iletişim kuruldu</summary>
        CustomerContacted = 19,

        /// <summary>Adres güncellendi</summary>
        AddressUpdated = 20
    }

    /// <summary>
    /// Audit log DTO - Listeleme için.
    /// </summary>
    public class DeliveryAuditLogDto
    {
        public int Id { get; set; }
        public int DeliveryTaskId { get; set; }
        public int? OrderId { get; set; }
        public int? CourierId { get; set; }
        public string? CourierName { get; set; }
        public AuditActorType ActorType { get; set; }
        public string ActorTypeName => ActorType.ToString();
        public int? ActorId { get; set; }
        public string? ActorName { get; set; }
        public DeliveryAuditEventType EventType { get; set; }
        public string EventTypeName => EventType.ToString();
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
