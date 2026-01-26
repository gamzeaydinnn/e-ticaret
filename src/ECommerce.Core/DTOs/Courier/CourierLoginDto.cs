using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kurye giriş isteği için DTO.
    /// Kurye, e-posta veya telefon numarası ile giriş yapabilir.
    /// </summary>
    public class CourierLoginDto
    {
        /// <summary>
        /// Kurye e-posta adresi (zorunlu).
        /// Telefon ile giriş eklendiğinde opsiyonel olabilir.
        /// </summary>
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Kurye şifresi (zorunlu).
        /// </summary>
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Beni hatırla seçeneği.
        /// True ise refresh token süresi uzatılır.
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}
