using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.User
{
    /// <summary>
    /// Admin panelinden kullanıcı şifresi güncelleme için DTO
    /// Madde 8: Şifre güncelleme özelliği
    /// </summary>
    public class AdminPasswordUpdateDto
    {
        /// <summary>
        /// Yeni şifre - minimum 6 karakter
        /// </summary>
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = null!;
    }
}
