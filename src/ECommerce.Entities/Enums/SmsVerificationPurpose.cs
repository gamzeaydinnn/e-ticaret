using System;

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// SMS doğrulama amacını belirten enum.
    /// Her doğrulama işlemi için ayrı bir amaç tanımlanmalıdır.
    /// </summary>
    public enum SmsVerificationPurpose
    {
        /// <summary>Yeni kullanıcı kaydı telefon doğrulama</summary>
        Registration = 1,

        /// <summary>Şifre sıfırlama doğrulama</summary>
        PasswordReset = 2,

        /// <summary>İki faktörlü kimlik doğrulama (2FA)</summary>
        TwoFactorAuth = 3,

        /// <summary>Telefon numarası değişikliği doğrulama</summary>
        PhoneChange = 4,

        /// <summary>Sipariş onayı doğrulama</summary>
        OrderConfirmation = 5,

        /// <summary>Hesap silme onayı</summary>
        AccountDeletion = 6
    }
}
