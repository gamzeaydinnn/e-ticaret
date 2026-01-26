using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kurye şifre değiştirme isteği için DTO.
    /// Kurye kendi şifresini değiştirmek istediğinde kullanılır.
    /// </summary>
    public class CourierChangePasswordDto
    {
        /// <summary>
        /// Mevcut şifre (zorunlu).
        /// Doğrulama için kullanılır.
        /// </summary>
        [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Yeni şifre (zorunlu).
        /// En az 6 karakter olmalıdır.
        /// </summary>
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Yeni şifre tekrarı (zorunlu).
        /// NewPassword ile aynı olmalıdır.
        /// </summary>
        [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Admin tarafından kurye şifresi sıfırlama isteği DTO'su.
    /// Admin, herhangi bir kuryenin şifresini sıfırlayabilir.
    /// </summary>
    public class AdminResetCourierPasswordDto
    {
        /// <summary>
        /// Kurye ID (Courier tablosundaki ID).
        /// </summary>
        [Required(ErrorMessage = "Kurye ID zorunludur.")]
        public int CourierId { get; set; }

        /// <summary>
        /// Yeni şifre (zorunlu).
        /// En az 6 karakter olmalıdır.
        /// </summary>
        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Kurye bir sonraki girişinde şifre değiştirmek zorunda mı?
        /// True ise kurye giriş yaptığında şifre değiştirme ekranına yönlendirilir.
        /// </summary>
        public bool MustChangePasswordOnLogin { get; set; } = true;
    }
}
