using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kurye token yenileme isteği için DTO.
    /// Mevcut access token + refresh token ile yeni token çifti alınır.
    /// </summary>
    public class CourierTokenRefreshDto
    {
        /// <summary>
        /// Mevcut (süresi dolmuş olabilir) Access Token.
        /// Token payload'ından kullanıcı bilgileri çekilir.
        /// </summary>
        [Required(ErrorMessage = "Access token zorunludur.")]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Refresh Token.
        /// Veritabanında kayıtlı ve geçerli olmalıdır.
        /// </summary>
        [Required(ErrorMessage = "Refresh token zorunludur.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token yenileme yanıtı DTO'su.
    /// </summary>
    public class CourierTokenRefreshResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem sonuç mesajı.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Yeni JWT Access Token.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Yeni Refresh Token (rotation).
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token geçerlilik süresi (saniye).
        /// </summary>
        public int ExpiresIn { get; set; }
    }
}
