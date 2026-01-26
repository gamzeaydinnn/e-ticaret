using System.Threading.Tasks;
using ECommerce.Core.DTOs.Courier;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kurye authentication işlemlerini yöneten servis interface'i.
    /// 
    /// Güvenlik Özellikleri:
    /// - JWT token tabanlı authentication
    /// - Refresh token rotation (her yenilemede yeni refresh token)
    /// - Token deny list desteği (logout sonrası token geçersizleştirme)
    /// - Rol kontrolü (sadece "Courier" rolündeki kullanıcılar)
    /// - Rate limiting desteği için attempt tracking
    /// 
    /// Kullanım Senaryoları:
    /// 1. Kurye mobil uygulama girişi
    /// 2. Token yenileme (arka planda otomatik)
    /// 3. Güvenli çıkış (tüm tokenların invalidate edilmesi)
    /// 4. Şifre değiştirme
    /// </summary>
    public interface ICourierAuthService
    {
        /// <summary>
        /// Kurye giriş işlemi.
        /// 
        /// İşlem Akışı:
        /// 1. E-posta ile kullanıcı bulunur
        /// 2. Şifre doğrulanır
        /// 3. Kullanıcının "Courier" rolünde olduğu kontrol edilir
        /// 4. Kullanıcının aktif Courier kaydı olduğu kontrol edilir
        /// 5. JWT access token ve refresh token üretilir
        /// 6. LastLoginAt güncellenir
        /// 7. Kurye bilgileri ile birlikte token döndürülür
        /// 
        /// Güvenlik:
        /// - Rate limiting için failed attempt tracking yapılmalı (controller'da)
        /// - IP adresi loglama
        /// - Son giriş tarihi güncelleme
        /// </summary>
        /// <param name="dto">Giriş bilgileri (email, password, rememberMe)</param>
        /// <param name="ipAddress">İstemci IP adresi (loglama için)</param>
        /// <returns>Login yanıtı (success, tokens, courier info)</returns>
        Task<CourierLoginResponseDto> LoginAsync(CourierLoginDto dto, string? ipAddress = null);

        /// <summary>
        /// Token yenileme işlemi.
        /// 
        /// İşlem Akışı:
        /// 1. Access token'dan kullanıcı bilgileri çıkarılır (expired olsa bile)
        /// 2. Refresh token veritabanından doğrulanır
        /// 3. Kullanıcının hala Courier olduğu kontrol edilir
        /// 4. Eski refresh token revoke edilir
        /// 5. Yeni access + refresh token çifti üretilir
        /// 
        /// Güvenlik:
        /// - Refresh token rotation (her kullanımda yeni token)
        /// - Refresh token reuse detection (tekrar kullanım engellenir)
        /// </summary>
        /// <param name="dto">Mevcut access token ve refresh token</param>
        /// <param name="ipAddress">İstemci IP adresi</param>
        /// <returns>Yeni token çifti</returns>
        Task<CourierTokenRefreshResponseDto> RefreshTokenAsync(CourierTokenRefreshDto dto, string? ipAddress = null);

        /// <summary>
        /// Kurye çıkış işlemi.
        /// 
        /// İşlem Akışı:
        /// 1. Mevcut access token'ın JTI'si deny list'e eklenir
        /// 2. Kullanıcının tüm refresh token'ları revoke edilir
        /// 3. Kurye status'u "offline" olarak güncellenir
        /// 
        /// Güvenlik:
        /// - Token deny list'e eklenir (kalan süre boyunca geçersiz)
        /// - Tüm cihazlardan çıkış için tüm refresh token'lar iptal edilir
        /// </summary>
        /// <param name="userId">Çıkış yapan kullanıcının ID'si</param>
        /// <param name="currentJti">Mevcut access token'ın JTI değeri</param>
        /// <param name="tokenExpiration">Token'ın geçerlilik bitiş zamanı</param>
        /// <returns>İşlem başarılı mı?</returns>
        Task<bool> LogoutAsync(int userId, string currentJti, System.DateTimeOffset tokenExpiration);

        /// <summary>
        /// Kurye şifre değiştirme işlemi.
        /// 
        /// İşlem Akışı:
        /// 1. Mevcut şifre doğrulanır
        /// 2. Yeni şifre ve tekrarı eşleştirilir
        /// 3. Şifre güncellenir
        /// 4. (Opsiyonel) Tüm mevcut oturumlar sonlandırılır
        /// </summary>
        /// <param name="userId">Şifre değiştiren kullanıcının ID'si</param>
        /// <param name="dto">Mevcut ve yeni şifre bilgileri</param>
        /// <returns>İşlem sonucu (success, message)</returns>
        Task<(bool success, string message)> ChangePasswordAsync(int userId, CourierChangePasswordDto dto);

        /// <summary>
        /// Admin tarafından kurye şifresi sıfırlama.
        /// 
        /// İşlem Akışı:
        /// 1. Kurye kaydı bulunur
        /// 2. Şifre yeni değerle güncellenir
        /// 3. MustChangePasswordOnLogin flag'i set edilebilir
        /// 4. Tüm mevcut oturumlar sonlandırılır
        /// </summary>
        /// <param name="dto">Admin reset bilgileri</param>
        /// <returns>İşlem sonucu (success, message)</returns>
        Task<(bool success, string message)> AdminResetPasswordAsync(AdminResetCourierPasswordDto dto);

        /// <summary>
        /// Kurye bilgilerini ID ile getirir.
        /// JWT token'dan alınan userId ile kurye bilgileri sorgulanır.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Kurye bilgileri veya null</returns>
        Task<CourierInfoDto?> GetCourierByUserIdAsync(int userId);

        /// <summary>
        /// Kullanıcının geçerli bir Courier olup olmadığını kontrol eder.
        /// Login ve token refresh işlemlerinde kullanılır.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Geçerli Courier ise true</returns>
        Task<bool> ValidateCourierAsync(int userId);
    }
}
