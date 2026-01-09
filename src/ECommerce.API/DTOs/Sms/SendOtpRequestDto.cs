using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.API.DTOs.Sms
{
    /// <summary>
    /// OTP gönderme isteği DTO
    /// </summary>
    public class SendOtpRequestDto
    {
        /// <summary>
        /// Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX formatında)
        /// Örnek: "5331234567" veya "05331234567"
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^(0?5\d{9})$", 
            ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz. (05XXXXXXXXX)")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Doğrulama amacı
        /// - Registration: Kayıt doğrulama
        /// - PasswordReset: Şifre sıfırlama
        /// - TwoFactorAuth: İki faktörlü kimlik doğrulama
        /// - PhoneChange: Telefon numarası değişikliği
        /// </summary>
        [Required(ErrorMessage = "Doğrulama amacı gereklidir.")]
        [EnumDataType(typeof(SmsVerificationPurpose), 
            ErrorMessage = "Geçerli bir doğrulama amacı seçiniz.")]
        public SmsVerificationPurpose Purpose { get; set; } = SmsVerificationPurpose.Registration;
    }
}
