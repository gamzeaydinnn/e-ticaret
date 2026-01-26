using System;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kurye giriş başarılı olduğunda dönen yanıt DTO'su.
    /// JWT access token, refresh token ve kurye bilgilerini içerir.
    /// </summary>
    public class CourierLoginResponseDto
    {
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// İşlem sonuç mesajı (Türkçe).
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// JWT Access Token.
        /// Bearer token olarak kullanılır.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Refresh Token.
        /// Access token yenilemek için kullanılır.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Token geçerlilik süresi (saniye).
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Kurye detay bilgileri.
        /// </summary>
        public CourierInfoDto? Courier { get; set; }
    }

    /// <summary>
    /// Kurye detay bilgileri DTO'su.
    /// Login response içinde ve kurye profil görüntülemede kullanılır.
    /// </summary>
    public class CourierInfoDto
    {
        /// <summary>
        /// Kurye ID (Courier tablosundaki ID).
        /// </summary>
        public int CourierId { get; set; }

        /// <summary>
        /// Kullanıcı ID (User tablosundaki ID).
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Kurye e-posta adresi.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Kurye tam adı.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Kurye adı.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Kurye soyadı.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Kurye telefon numarası.
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Kurye araç tipi (Motosiklet, Bisiklet, Araba).
        /// </summary>
        public string? Vehicle { get; set; }

        /// <summary>
        /// Kurye durumu (active, busy, offline, break).
        /// </summary>
        public string Status { get; set; } = "offline";

        /// <summary>
        /// Kurye son konum bilgisi.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Kurye değerlendirme puanı (0-5).
        /// </summary>
        public decimal Rating { get; set; }

        /// <summary>
        /// Aktif sipariş sayısı.
        /// </summary>
        public int ActiveOrders { get; set; }

        /// <summary>
        /// Bugün tamamlanan sipariş sayısı.
        /// </summary>
        public int CompletedToday { get; set; }

        /// <summary>
        /// Son aktif olduğu tarih/saat.
        /// </summary>
        public DateTime? LastActiveAt { get; set; }
    }
}
