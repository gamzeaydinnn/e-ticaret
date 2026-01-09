using System;

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// SMS doğrulama kaydının durumunu belirten enum.
    /// </summary>
    public enum SmsVerificationStatus
    {
        /// <summary>Kod gönderildi, doğrulama bekleniyor</summary>
        Pending = 0,

        /// <summary>Kod başarıyla doğrulandı</summary>
        Verified = 1,

        /// <summary>Kodun süresi doldu</summary>
        Expired = 2,

        /// <summary>Maksimum yanlış deneme aşıldı</summary>
        MaxAttemptsExceeded = 3,

        /// <summary>İptal edildi</summary>
        Cancelled = 4
    }
}
