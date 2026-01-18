// ==========================================================================
// DeliveryEvent.cs - Teslimat Olayları Audit Trail Entity
// ==========================================================================
// Bu entity, teslimat görevlerinde gerçekleşen TÜM olayları kaydeder.
// Tam bir audit trail sağlar - kim, ne zaman, ne yaptı, eski/yeni değerler.
// Hukuki gereklilikler, debug ve operasyonel analiz için kritik önem taşır.
//
// DİKKAT: Bu tablo yüksek hacimli olacaktır!
// - Partition stratejisi düşünülmeli (tarih bazlı)
// - Eski kayıtlar arşivlenebilir (cold storage)
// - Index stratejisi performans için kritik
// ==========================================================================

using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Teslimat olayı audit log entity'si.
    /// Her durum değişikliği, atama, güncelleme bu tabloya yazılır.
    /// Immutable tasarım - kayıtlar oluşturulduktan sonra değiştirilemez.
    /// </summary>
    public class DeliveryEvent : BaseEntity
    {
        // =====================================================================
        // İLİŞKİ ALANLARI
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi ID'si.
        /// Her event bir DeliveryTask'a bağlıdır.
        /// </summary>
        public int DeliveryTaskId { get; set; }

        /// <summary>
        /// İlişkili sipariş ID'si.
        /// Denormalize edilmiş - sipariş bazlı sorgular için.
        /// </summary>
        public int? OrderId { get; set; }

        // =====================================================================
        // AKTÖR BİLGİLERİ (Kim yaptı?)
        // =====================================================================

        /// <summary>
        /// Olayı gerçekleştiren aktör tipi.
        /// System, Admin, Courier veya Customer olabilir.
        /// </summary>
        public ActorType ActorType { get; set; }

        /// <summary>
        /// Aktör ID'si.
        /// ActorType'a göre UserId veya CourierId olabilir.
        /// System için null olabilir.
        /// </summary>
        public int? ActorId { get; set; }

        /// <summary>
        /// Aktör adı.
        /// Denormalize - ID'den lookup yapmadan gösterim için.
        /// </summary>
        public string? ActorName { get; set; }

        /// <summary>
        /// Aktör IP adresi.
        /// Güvenlik ve audit için.
        /// </summary>
        public string? ActorIpAddress { get; set; }

        /// <summary>
        /// IpAddress alias - ActorIpAddress ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? IpAddress
        {
            get => ActorIpAddress;
            set => ActorIpAddress = value;
        }

        /// <summary>
        /// Aktör user agent.
        /// Hangi cihaz/tarayıcıdan yapıldı.
        /// </summary>
        public string? ActorUserAgent { get; set; }

        /// <summary>
        /// UserAgent alias - ActorUserAgent ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? UserAgent
        {
            get => ActorUserAgent;
            set => ActorUserAgent = value;
        }

        // =====================================================================
        // OLAY DETAYLARI
        // =====================================================================

        /// <summary>
        /// Olay tipi.
        /// Created, Assigned, Accepted, Delivered vb.
        /// </summary>
        public DeliveryEventType EventType { get; set; }

        /// <summary>
        /// Olay açıklaması.
        /// İnsan okunabilir format.
        /// Örn: "Sipariş #12345 için teslimat görevi oluşturuldu"
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Olay zamanı.
        /// UTC formatında, sunucu saati.
        /// </summary>
        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        // =====================================================================
        // ESKİ VE YENİ DEĞERLER (Change Tracking)
        // =====================================================================

        /// <summary>
        /// Eski değerler JSON formatında.
        /// Değişiklik öncesi entity durumu.
        /// Null ise yeni kayıt oluşturulmuş demektir.
        /// </summary>
        public string? OldValuesJson { get; set; }

        /// <summary>
        /// OldValue alias - OldValuesJson ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? OldValue
        {
            get => OldValuesJson;
            set => OldValuesJson = value;
        }

        /// <summary>
        /// OldStateJson alias - OldValuesJson ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? OldStateJson
        {
            get => OldValuesJson;
            set => OldValuesJson = value;
        }

        /// <summary>
        /// Yeni değerler JSON formatında.
        /// Değişiklik sonrası entity durumu.
        /// Silme işlemlerinde null olabilir.
        /// </summary>
        public string? NewValuesJson { get; set; }

        /// <summary>
        /// NewValue alias - NewValuesJson ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? NewValue
        {
            get => NewValuesJson;
            set => NewValuesJson = value;
        }

        /// <summary>
        /// NewStateJson alias - NewValuesJson ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? NewStateJson
        {
            get => NewValuesJson;
            set => NewValuesJson = value;
        }

        /// <summary>
        /// Değişen alanlar.
        /// Comma-separated alan isimleri.
        /// Örn: "Status,AssignedCourierId,AssignedAt"
        /// </summary>
        public string? ChangedFields { get; set; }

        // =====================================================================
        // DURUM DEĞİŞİKLİĞİ ÖZETİ
        // =====================================================================

        /// <summary>
        /// Önceki durum.
        /// Durum değişikliği eventleri için.
        /// </summary>
        public DeliveryStatus? OldStatus { get; set; }

        /// <summary>
        /// Yeni durum.
        /// Durum değişikliği eventleri için.
        /// </summary>
        public DeliveryStatus? NewStatus { get; set; }

        /// <summary>
        /// Önceki atanan kurye ID.
        /// Reassign eventleri için.
        /// </summary>
        public int? OldCourierId { get; set; }

        /// <summary>
        /// Yeni atanan kurye ID.
        /// Atama eventleri için.
        /// </summary>
        public int? NewCourierId { get; set; }

        // =====================================================================
        // KONUM BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Olay konumu enlem.
        /// Kurye eventleri için GPS verisi.
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Olay konumu boylam.
        /// Kurye eventleri için GPS verisi.
        /// </summary>
        public double? Longitude { get; set; }

        // =====================================================================
        // EK BİLGİLER
        // =====================================================================

        /// <summary>
        /// Ek metadata JSON formatında.
        /// Esnek veri yapısı için.
        /// Örn: timeout süresi, reassign sebebi, hata detayı.
        /// </summary>
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Korelasyon ID.
        /// İlişkili eventleri gruplamak için.
        /// Örn: Aynı istek zincirindeki tüm eventler.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Kaynak sistem.
        /// Hangi servis/modülden geldi.
        /// Örn: "DeliveryTaskService", "CourierApp", "AdminPanel"
        /// </summary>
        public string? SourceSystem { get; set; }

        /// <summary>
        /// Önem seviyesi.
        /// Info, Warning, Error, Critical.
        /// Filtreleme ve alerting için.
        /// </summary>
        public string Severity { get; set; } = "Info";

        /// <summary>
        /// İşlendi mi? (bildirim gönderildi mi?)
        /// Async notification işleme için flag.
        /// </summary>
        public bool Processed { get; set; } = false;

        /// <summary>
        /// İşlenme zamanı.
        /// Bildirimler gönderildiğinde set edilir.
        /// </summary>
        public DateTime? ProcessedAt { get; set; }

        // =====================================================================
        // NAVİGASYON PROPERTİES
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi.
        /// </summary>
        public virtual DeliveryTask? DeliveryTask { get; set; }

        /// <summary>
        /// İlişkili sipariş.
        /// Denormalize ilişki - optional.
        /// </summary>
        public virtual Order? Order { get; set; }
    }
}
