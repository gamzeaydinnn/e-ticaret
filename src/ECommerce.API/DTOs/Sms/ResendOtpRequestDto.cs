using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.API.DTOs.Sms
{
    /// <summary>
    /// OTP tekrar gönderme isteği DTO
    /// Not: SendOtpRequestDto ile aynı yapıda olabilir, ancak semantik olarak ayrı tutuldu
    /// </summary>
    public class ResendOtpRequestDto
    {
        /// <summary>
        /// Telefon numarası
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^(0?5\d{9})$", 
            ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz.")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Doğrulama amacı (önceki gönderimle aynı olmalıdır)
        /// </summary>
        [Required(ErrorMessage = "Doğrulama amacı gereklidir.")]
        [EnumDataType(typeof(SmsVerificationPurpose))]
        public SmsVerificationPurpose Purpose { get; set; } = SmsVerificationPurpose.Registration;
    }
}
