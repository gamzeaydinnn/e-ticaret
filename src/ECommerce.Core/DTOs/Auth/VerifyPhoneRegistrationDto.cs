using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Auth
{
    /// <summary>
    /// Telefon doğrulama ile kayıt DTO'su.
    /// Kayıt sonrası SMS doğrulama için kullanılır.
    /// </summary>
    public class VerifyPhoneRegistrationDto
    {
        /// <summary>
        /// Telefon numarası
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
        /// Email adresi (kullanıcıyı bulmak için)
        /// </summary>
        [Required(ErrorMessage = "Email adresi gereklidir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
        public string Email { get; set; } = string.Empty;
    }
}
