// ==========================================================================
// DeliveryTask.cs - Teslimat Görevi Entity
// ==========================================================================
// Bu entity, siparişten (Order) bağımsız bir teslimat görevi tanımlar.
// Sipariş "ticari kayıt", teslimat ise "lojistik görev"dir.
// Order ile 1:1 ilişkisi vardır ancak yaşam döngüleri farklıdır.
//
// SOLID Prensipleri:
// - Single Responsibility: Sadece teslimat görevi verilerini tutar
// - Open/Closed: Enum'lar ile genişletilebilir, değiştirmeden ekleme yapılabilir
// - Liskov Substitution: BaseEntity'den düzgün miras alır
// - Interface Segregation: Navigation property'ler lazy load destekler
// - Dependency Inversion: Concrete sınıflara değil, abstraction'lara bağlı
// ==========================================================================

using System;
using System.Collections.Generic;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Teslimat görevi entity'si.
    /// Order'dan bağımsız olarak teslimat operasyonlarını yönetir.
    /// Kurye ataması, durum takibi ve POD yönetimi bu entity üzerinden yapılır.
    /// </summary>
    public class DeliveryTask : BaseEntity
    {
        // =====================================================================
        // İLİŞKİ ALANLARI (Foreign Keys)
        // =====================================================================

        /// <summary>
        /// İlişkili sipariş ID'si.
        /// Her teslimat görevi bir siparişe bağlıdır.
        /// Not: Bir siparişin birden fazla teslimat görevi olabilir (yeniden deneme durumunda).
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Atanan kurye ID'si.
        /// Null ise henüz kurye atanmamış demektir (Status = Created).
        /// </summary>
        public int? AssignedCourierId { get; set; }

        /// <summary>
        /// Teslimat bölgesi ID'si.
        /// Kurye atama algoritmasında bölge eşleştirmesi için kullanılır.
        /// </summary>
        public int? DeliveryZoneId { get; set; }

        // =====================================================================
        // TESLİMAT NOKTASI BİLGİLERİ (Pickup Location - Depo/Mağaza)
        // =====================================================================

        /// <summary>
        /// Teslim alınacak yer adresi (depo, mağaza vb.).
        /// Kurye bu adresten paketi teslim alır.
        /// </summary>
        public string PickupAddress { get; set; } = string.Empty;

        /// <summary>
        /// PickupAddressLine alias - PickupAddress ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string PickupAddressLine
        {
            get => PickupAddress;
            set => PickupAddress = value;
        }

        /// <summary>
        /// Teslim alınacak yer şehir/il bilgisi.
        /// </summary>
        public string? PickupCity { get; set; }

        /// <summary>
        /// Teslim alınacak yer ilçe bilgisi.
        /// </summary>
        public string? PickupDistrict { get; set; }

        /// <summary>
        /// Teslim alınacak yer kişi adı.
        /// Depo sorumlusu veya mağaza görevlisi adı.
        /// </summary>
        public string? PickupContactName { get; set; }

        /// <summary>
        /// Teslim alınacak yer enlem koordinatı.
        /// Navigasyon ve mesafe hesaplama için kullanılır.
        /// </summary>
        public double? PickupLatitude { get; set; }

        /// <summary>
        /// Teslim alınacak yer boylam koordinatı.
        /// Navigasyon ve mesafe hesaplama için kullanılır.
        /// </summary>
        public double? PickupLongitude { get; set; }

        /// <summary>
        /// Teslim alınacak yerin iletişim bilgisi.
        /// Depo sorumlusu veya mağaza telefonu.
        /// </summary>
        public string? PickupContactPhone { get; set; }

        /// <summary>
        /// Teslim alınacak yer notları.
        /// "Arka kapıdan girin", "Depo sorumlusu Ahmet" gibi.
        /// </summary>
        public string? PickupNotes { get; set; }

        // =====================================================================
        // TESLİMAT NOKTASI BİLGİLERİ (Dropoff Location - Müşteri Adresi)
        // =====================================================================

        /// <summary>
        /// Teslimat yapılacak adres (müşteri adresi).
        /// Açık adres formatında, kurye navigasyonu için yeterli detayda olmalı.
        /// </summary>
        public string DropoffAddress { get; set; } = string.Empty;

        /// <summary>
        /// DropoffAddressLine alias - DropoffAddress ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string DropoffAddressLine
        {
            get => DropoffAddress;
            set => DropoffAddress = value;
        }

        /// <summary>
        /// Teslimat yapılacak il.
        /// Bölge bazlı raporlama ve filtreleme için kullanılır.
        /// </summary>
        public string? DropoffCity { get; set; }

        /// <summary>
        /// Teslimat yapılacak ilçe.
        /// Bölge bazlı kurye ataması için kullanılır.
        /// </summary>
        public string? DropoffDistrict { get; set; }

        /// <summary>
        /// Teslimat noktası enlem koordinatı.
        /// Navigasyon, ETA hesaplama ve mesafe için kritik.
        /// </summary>
        public double? DropoffLatitude { get; set; }

        /// <summary>
        /// Teslimat noktası boylam koordinatı.
        /// Navigasyon, ETA hesaplama ve mesafe için kritik.
        /// </summary>
        public double? DropoffLongitude { get; set; }

        // =====================================================================
        // MÜŞTERİ İLETİŞİM BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Teslimat alacak kişinin adı.
        /// Kurye bu ismi doğrulama için kullanır.
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// CustomerName alias - ContactName ile aynı değeri döner.
        /// Manager ve eski kod uyumu için kullanılır.
        /// </summary>
        public string CustomerName
        {
            get => ContactName;
            set => ContactName = value;
        }

        /// <summary>
        /// Teslimat alacak kişinin telefonu.
        /// Kurye ulaşamadığında arayacağı numara.
        /// </summary>
        public string ContactPhone { get; set; } = string.Empty;

        /// <summary>
        /// CustomerPhone alias - ContactPhone ile aynı değeri döner.
        /// Manager ve eski kod uyumu için kullanılır.
        /// </summary>
        public string CustomerPhone
        {
            get => ContactPhone;
            set => ContactPhone = value;
        }

        /// <summary>
        /// Alternatif iletişim telefonu.
        /// Birincil numara ulaşılamadığında kullanılır.
        /// </summary>
        public string? AlternateContactPhone { get; set; }

        // =====================================================================
        // ZAMAN PENCERESI VE TAHMİNLER
        // =====================================================================

        /// <summary>
        /// Teslimat zaman penceresi başlangıcı.
        /// Müşterinin teslimat için belirttiği en erken zaman.
        /// </summary>
        public DateTime? TimeWindowStart { get; set; }

        /// <summary>
        /// Teslimat zaman penceresi bitişi.
        /// Müşterinin teslimat için belirttiği en geç zaman.
        /// </summary>
        public DateTime? TimeWindowEnd { get; set; }

        /// <summary>
        /// SLA son teslim zamanı.
        /// Bu zamana kadar teslimat yapılmazsa SLA ihlali sayılır.
        /// </summary>
        public DateTime? SlaDeadline { get; set; }

        /// <summary>
        /// Tahmini teslimat zamanı (ETA).
        /// Kurye konumu ve trafik durumuna göre dinamik güncellenir.
        /// </summary>
        public DateTime? EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// Son deneme tarihi.
        /// Başarısız teslimat sonrası yeniden deneme için referans.
        /// </summary>
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>
        /// Planlanan teslim alma zamanı.
        /// Kurye bu zamanda paketi almalı.
        /// </summary>
        public DateTime? ScheduledPickupTime { get; set; }

        /// <summary>
        /// Planlanan teslimat zamanı.
        /// Müşterinin tercih ettiği teslimat zamanı.
        /// </summary>
        public DateTime? ScheduledDeliveryTime { get; set; }

        // =====================================================================
        // DURUM VE ÖNCELİK BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Teslimat görevi durumu.
        /// State machine mantığıyla yönetilir, geçişler validasyonludur.
        /// </summary>
        public DeliveryStatus Status { get; set; } = DeliveryStatus.Created;

        /// <summary>
        /// Teslimat önceliği.
        /// Kurye atama ve sıralama algoritmasında kullanılır.
        /// </summary>
        public DeliveryPriority Priority { get; set; } = DeliveryPriority.Normal;

        /// <summary>
        /// Toplam deneme sayısı.
        /// Başarısız teslimatlar sonrası yeniden deneme sayacı.
        /// </summary>
        public int AttemptCount { get; set; } = 0;

        /// <summary>
        /// Maksimum izin verilen deneme sayısı.
        /// Bu sayı aşılırsa görev manuel müdahale gerektirir.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Kurye reddetme sayısı.
        /// Kaç kez kurye tarafından reddedildiğini takip eder.
        /// </summary>
        public int? RejectionCount { get; set; }

        /// <summary>
        /// Kapıda ödeme var mı? (Hesaplanan alan)
        /// CodAmount > 0 ise true döner.
        /// </summary>
        public bool IsCod => CodAmount.HasValue && CodAmount.Value > 0;

        // =====================================================================
        // FİNANSAL BİLGİLER (COD - Kapıda Ödeme)
        // =====================================================================

        /// <summary>
        /// Kapıda ödeme tutarı.
        /// 0 veya null ise ödeme online yapılmış demektir.
        /// </summary>
        public decimal? CodAmount { get; set; }

        /// <summary>
        /// Para birimi.
        /// Varsayılan TRY, multi-currency desteği için.
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Teslimat ücreti.
        /// Kurye performans hesaplamalarında kullanılır.
        /// </summary>
        public decimal DeliveryFee { get; set; } = 0;

        /// <summary>
        /// COD tahsil edildi mi?
        /// Kurye parayı aldığında true olarak işaretlenir.
        /// </summary>
        public bool CodCollected { get; set; } = false;

        /// <summary>
        /// COD tahsilat zamanı.
        /// Nakit akış raporlaması için kullanılır.
        /// </summary>
        public DateTime? CodCollectedAt { get; set; }

        // =====================================================================
        // NOTLAR VE EK BİLGİLER
        // =====================================================================

        /// <summary>
        /// Kurye için özel notlar.
        /// "Kapıda bekleyin", "Zili çalmayın" gibi talimatlar.
        /// </summary>
        public string? NotesForCourier { get; set; }

        /// <summary>
        /// Notes alias - NotesForCourier ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? Notes
        {
            get => NotesForCourier;
            set => NotesForCourier = value;
        }

        /// <summary>
        /// Admin notları.
        /// İç kullanım için, kuryeye gösterilmez.
        /// </summary>
        public string? AdminNotes { get; set; }

        /// <summary>
        /// Müşteri teslimat notu.
        /// Müşterinin sipariş sırasında girdiği not.
        /// </summary>
        public string? CustomerDeliveryNote { get; set; }

        /// <summary>
        /// CustomerNotes alias - CustomerDeliveryNote ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? CustomerNotes
        {
            get => CustomerDeliveryNote;
            set => CustomerDeliveryNote = value;
        }

        /// <summary>
        /// Özel talimatlar JSON formatında.
        /// Esnek, genişletilebilir ek bilgiler için.
        /// </summary>
        public string? SpecialInstructionsJson { get; set; }

        /// <summary>
        /// Dahili notlar (admin için, kuryeye gösterilmez).
        /// Sistem tarafından otomatik oluşturulan notlar.
        /// </summary>
        public string? NotesInternal { get; set; }

        // =====================================================================
        // YENİDEN DENEME VE İADE BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Yeniden deneme planlandı mı?
        /// Başarısız teslimat sonrası planlanan tarih.
        /// </summary>
        public DateTime? RetryScheduledAt { get; set; }

        /// <summary>
        /// İade görevi planlandı mı?
        /// Maksimum deneme sonrası iade oluşturuldu.
        /// </summary>
        public bool IsReturnScheduled { get; set; } = false;

        /// <summary>
        /// Bu bir iade görevi mi?
        /// Başarısız teslimat sonrası oluşturulan iade.
        /// </summary>
        public bool IsReturnTask { get; set; } = false;

        /// <summary>
        /// Ana teslimat görevi ID'si (yeniden deneme veya iade için).
        /// Orijinal görevle ilişkilendirmek için kullanılır.
        /// </summary>
        public int? ParentDeliveryTaskId { get; set; }

        // =====================================================================
        // ZAMAN DAMGALARI (Audit & Tracking)
        // =====================================================================

        /// <summary>
        /// Kuryeye atanma zamanı.
        /// SLA ve performans ölçümü için kritik.
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Kurye kabul zamanı.
        /// Kabul hızı metriği için kullanılır.
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>
        /// Paket teslim alma zamanı.
        /// Kurye depodan paketi aldığında kaydedilir.
        /// </summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>
        /// Yola çıkış zamanı.
        /// Transit süre hesaplaması için kullanılır.
        /// </summary>
        public DateTime? InTransitAt { get; set; }

        /// <summary>
        /// Teslimat tamamlanma zamanı.
        /// Başarılı teslimat zamanı.
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Başarısız teslimat zamanı.
        /// En son başarısız deneme zamanı.
        /// </summary>
        public DateTime? FailedAt { get; set; }

        /// <summary>
        /// İptal zamanı.
        /// Görev iptal edildiğinde kaydedilir.
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// İptal sebebi.
        /// Raporlama ve analiz için kullanılır.
        /// </summary>
        public string? CancellationReason { get; set; }

        /// <summary>
        /// Güncelleyen kullanıcı ID'si.
        /// Audit trail için kullanılır.
        /// </summary>
        public int? UpdatedByUserId { get; set; }

        /// <summary>
        /// Oluşturan kullanıcı ID'si.
        /// Audit trail için kullanılır.
        /// </summary>
        public int? CreatedByUserId { get; set; }

        // =====================================================================
        // MESAFE VE SÜRE HESAPLAMALARI
        // =====================================================================

        /// <summary>
        /// Tahmini mesafe (km).
        /// Pickup'tan dropoff'a mesafe.
        /// </summary>
        public double? EstimatedDistanceKm { get; set; }

        /// <summary>
        /// Gerçekleşen mesafe (km).
        /// GPS verilerinden hesaplanan gerçek mesafe.
        /// </summary>
        public double? ActualDistanceKm { get; set; }

        /// <summary>
        /// Tahmini süre (dakika).
        /// Trafik durumuna göre hesaplanan tahmini teslimat süresi.
        /// </summary>
        public int? EstimatedDurationMinutes { get; set; }

        /// <summary>
        /// Gerçekleşen süre (dakika).
        /// Pickup'tan delivery'e geçen gerçek süre.
        /// </summary>
        public int? ActualDurationMinutes { get; set; }

        // =====================================================================
        // UYUMLULUK ALİAS PROPERTİES (Manager uyumu için)
        // =====================================================================

        /// <summary>
        /// CourierId alias - AssignedCourierId ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public int? CourierId
        {
            get => AssignedCourierId;
            set => AssignedCourierId = value;
        }

        /// <summary>
        /// ZoneId alias - DeliveryZoneId ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public int? ZoneId
        {
            get => DeliveryZoneId;
            set => DeliveryZoneId = value;
        }

        // =====================================================================
        // NAVİGASYON PROPERTİES (İlişkiler)
        // =====================================================================

        /// <summary>
        /// İlişkili sipariş navigation property.
        /// Lazy loading destekler.
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// Atanan kurye navigation property.
        /// Null olabilir (henüz atanmamış).
        /// </summary>
        public virtual Courier? AssignedCourier { get; set; }

        /// <summary>
        /// Courier alias - AssignedCourier ile aynı nesneyi döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public virtual Courier? Courier
        {
            get => AssignedCourier;
            set => AssignedCourier = value;
        }

        /// <summary>
        /// Teslimat bölgesi navigation property.
        /// Bölge bazlı operasyonlar için.
        /// </summary>
        public virtual DeliveryZone? DeliveryZone { get; set; }

        /// <summary>
        /// Teslimat kanıtları koleksiyonu.
        /// Bir teslimat birden fazla POD içerebilir (foto + OTP gibi).
        /// </summary>
        public virtual ICollection<DeliveryProofOfDelivery> ProofsOfDelivery { get; set; } 
            = new HashSet<DeliveryProofOfDelivery>();

        /// <summary>
        /// Başarısızlık kayıtları koleksiyonu.
        /// Her başarısız deneme için bir kayıt tutulur.
        /// </summary>
        public virtual ICollection<DeliveryFailure> Failures { get; set; } 
            = new HashSet<DeliveryFailure>();

        /// <summary>
        /// Teslimat olayları koleksiyonu (audit trail).
        /// Tüm durum değişiklikleri ve aksiyonlar kaydedilir.
        /// </summary>
        public virtual ICollection<DeliveryEvent> Events { get; set; } 
            = new HashSet<DeliveryEvent>();
    }
}
