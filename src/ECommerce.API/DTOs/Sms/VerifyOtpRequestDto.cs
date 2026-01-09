using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.API.DTOs.Sms
{
    /// <summary>
    /// OTP doğrulama isteği DTO
    /// </summary>
    public class VerifyOtpRequestDto
    {
        /// <summary>
        /// Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX formatında)
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^(0?5\d{9})$", 
            ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz.")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// SMS ile gelen 6 haneli doğrulama kodu
        /// </summary>
        [Required(ErrorMessage = "Doğrulama kodu gereklidir.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Kod 6 haneli olmalıdır.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Kod sadece rakamlardan oluşmalıdır.")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Doğrulama amacı (gönderimle aynı olmalıdır)
        /// </summary>
        [Required(ErrorMessage = "Doğrulama amacı gereklidir.")]
        [EnumDataType(typeof(SmsVerificationPurpose))]
        public SmsVerificationPurpose Purpose { get; set; } = SmsVerificationPurpose.Registration;
    }
}
