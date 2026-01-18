// ==========================================================================
// DeliveryProofOfDelivery.cs - Teslimat Kanıtı (POD) Entity
// ==========================================================================
// Bu entity, teslimat tamamlandığında alınan kanıtları saklar.
// Fotoğraf, OTP doğrulama veya dijital imza yöntemlerini destekler.
// Her başarılı teslimat için en az bir POD zorunludur.
//
// Güvenlik Notları:
// - Fotoğraflar blob storage'da saklanır, URL signed olmalıdır
// - OTP kodları hash'lenmiş olarak saklanır
// - İmza verileri şifrelenerek saklanabilir
// ==========================================================================

using System;
using ECommerce.Entities.Enums;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Teslimat kanıtı (Proof of Delivery) entity'si.
    /// Teslimatın gerçekleştiğini kanıtlayan belgeler için kullanılır.
    /// Hukuki gereklilikler ve müşteri itirazları için kritik önem taşır.
    /// </summary>
    public class DeliveryProofOfDelivery : BaseEntity
    {
        // =====================================================================
        // İLİŞKİ ALANLARI
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi ID'si.
        /// Her POD bir DeliveryTask'a bağlıdır.
        /// </summary>
        public int DeliveryTaskId { get; set; }

        /// <summary>
        /// Kanıtı oluşturan kurye ID'si.
        /// Audit ve doğrulama için zorunlu.
        /// </summary>
        public int CapturedByCourierId { get; set; }

        // =====================================================================
        // KANIT YÖNTEMİ
        // =====================================================================

        /// <summary>
        /// Kullanılan kanıt yöntemi.
        /// Photo, OTP, Signature, PinCode veya QrCode olabilir.
        /// </summary>
        public DeliveryProofMethod Method { get; set; }

        /// <summary>
        /// ProofMethod alias - Method ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public DeliveryProofMethod ProofMethod
        {
            get => Method;
            set => Method = value;
        }

        // =====================================================================
        // FOTOĞRAF KANITI
        // =====================================================================

        /// <summary>
        /// Teslimat fotoğrafı URL'i.
        /// Blob storage'da saklanan fotoğrafın erişim adresi.
        /// Method = Photo olduğunda zorunludur.
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Fotoğraf küçük resim URL'i (thumbnail).
        /// Liste görünümlerinde hızlı yükleme için kullanılır.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Fotoğrafın çekildiği konum enlem.
        /// GPS koordinatı - fotoğrafın doğru konumda çekildiğini doğrular.
        /// </summary>
        public double? PhotoLatitude { get; set; }

        /// <summary>
        /// Fotoğrafın çekildiği konum boylam.
        /// GPS koordinatı - fotoğrafın doğru konumda çekildiğini doğrular.
        /// </summary>
        public double? PhotoLongitude { get; set; }

        // =====================================================================
        // OTP DOĞRULAMA
        // =====================================================================

        /// <summary>
        /// OTP kodu (hash'lenmiş).
        /// Güvenlik için plain text olarak saklanmaz.
        /// </summary>
        public string? OtpCodeHash { get; set; }

        /// <summary>
        /// OtpCode alias - OtpCodeHash ile aynı değeri döner.
        /// Manager uyumu için kullanılır.
        /// </summary>
        public string? OtpCode
        {
            get => OtpCodeHash;
            set => OtpCodeHash = value;
        }

        /// <summary>
        /// OTP doğrulandı mı?
        /// True ise müşteri kodu doğru girmiş demektir.
        /// </summary>
        public bool OtpVerified { get; set; } = false;

        /// <summary>
        /// OTP doğrulama zamanı.
        /// Audit için kullanılır.
        /// </summary>
        public DateTime? OtpVerifiedAt { get; set; }

        /// <summary>
        /// OTP denememe sayısı.
        /// Brute-force koruması için maksimum deneme sınırı uygulanır.
        /// </summary>
        public int OtpAttemptCount { get; set; } = 0;

        // =====================================================================
        // DİJİTAL İMZA
        // =====================================================================

        /// <summary>
        /// Dijital imza görsel URL'i.
        /// İmza canvas'ından oluşturulan görüntü.
        /// </summary>
        public string? SignatureUrl { get; set; }

        /// <summary>
        /// İmza vektör verisi (SVG/JSON).
        /// İmzanın vektörel olarak saklanması için.
        /// </summary>
        public string? SignatureData { get; set; }

        /// <summary>
        /// İmza atan kişi adı.
        /// Müşteri veya yetkili kişi adı.
        /// </summary>
        public string? SignerName { get; set; }

        /// <summary>
        /// Teslimatı alan kişi adı.
        /// POD için alıcı bilgisi.
        /// </summary>
        public string? ReceiverName { get; set; }

        /// <summary>
        /// Alıcının müşteri ile ilişkisi.
        /// "Eşi", "Komşu", "İş arkadaşı" gibi.
        /// </summary>
        public string? ReceiverRelation { get; set; }

        /// <summary>
        /// Kanıtın yakalandığı enlem koordinatı.
        /// GPS doğrulaması için kullanılır.
        /// </summary>
        public double? CapturedLatitude { get; set; }

        /// <summary>
        /// Kanıtın yakalandığı boylam koordinatı.
        /// GPS doğrulaması için kullanılır.
        /// </summary>
        public double? CapturedLongitude { get; set; }

        // =====================================================================
        // PIN/QR KOD DOĞRULAMA
        // =====================================================================

        /// <summary>
        /// Pin kodu (hash'lenmiş).
        /// Müşteri tarafından belirlenen sabit doğrulama kodu.
        /// </summary>
        public string? PinCodeHash { get; set; }

        /// <summary>
        /// QR kod verisi.
        /// Taranan QR kodun içeriği.
        /// </summary>
        public string? QrCodeData { get; set; }

        // =====================================================================
        // ZAMAN VE KONUM BİLGİLERİ
        // =====================================================================

        /// <summary>
        /// Kanıtın oluşturulma zamanı.
        /// Cihaz saati veya sunucu saati olabilir.
        /// </summary>
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Kanıtın sunucuya yüklenme zamanı.
        /// Offline modda CapturedAt ile farklı olabilir.
        /// </summary>
        public DateTime? UploadedAt { get; set; }

        /// <summary>
        /// Kanıtın oluşturulduğu cihaz bilgisi.
        /// Debug ve doğrulama için kullanılır.
        /// </summary>
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// IP adresi.
        /// Güvenlik ve audit için.
        /// </summary>
        public string? IpAddress { get; set; }

        // =====================================================================
        // DOĞRULAMA DURUMU
        // =====================================================================

        /// <summary>
        /// Kanıt doğrulandı mı?
        /// Admin tarafından manuel doğrulama veya otomatik validasyon.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Doğrulayan admin ID'si.
        /// Manuel doğrulama yapıldıysa kim onayladı.
        /// </summary>
        public int? VerifiedByUserId { get; set; }

        /// <summary>
        /// Doğrulama zamanı.
        /// </summary>
        public DateTime? VerifiedAt { get; set; }

        /// <summary>
        /// Ek notlar.
        /// Doğrulama sırasında eklenen açıklamalar.
        /// </summary>
        public string? Notes { get; set; }

        // =====================================================================
        // NAVİGASYON PROPERTİES
        // =====================================================================

        /// <summary>
        /// İlişkili teslimat görevi.
        /// </summary>
        public virtual DeliveryTask? DeliveryTask { get; set; }

        /// <summary>
        /// Kanıtı oluşturan kurye.
        /// </summary>
        public virtual Courier? CapturedByCourier { get; set; }

        /// <summary>
        /// Doğrulayan admin kullanıcı.
        /// </summary>
        public virtual User? VerifiedByUser { get; set; }
    }
}
