using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Auth
{
    /// <summary>
    /// Telefon numarası ile şifre sıfırlama DTO'su.
    /// SMS doğrulama kodu ile birlikte yeni şifre belirlenir.
    /// </summary>
    public class ResetPasswordByPhoneDto
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
        /// Yeni şifre (minimum 6 karakter)
        /// </summary>
        [Required(ErrorMessage = "Yeni şifre gereklidir.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Yeni şifre tekrar (doğrulama için)
        /// </summary>
        [Required(ErrorMessage = "Şifre tekrarı gereklidir.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
