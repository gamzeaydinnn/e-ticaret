using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Auth
{
    /// <summary>
    /// Telefon numarası ile şifre sıfırlama isteği DTO'su.
    /// </summary>
    public class ForgotPasswordByPhoneDto
    {
        /// <summary>
        /// Telefon numarası (05XXXXXXXXX veya 5XXXXXXXXX formatında)
        /// </summary>
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^(0?5\d{9})$", 
            ErrorMessage = "Geçerli bir Türkiye cep telefonu numarası giriniz.")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
