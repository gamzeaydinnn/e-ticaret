using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Courier;
using ECommerce.Core.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kurye authentication işlemlerini yöneten servis implementasyonu.
    /// 
    /// Güvenlik Özellikleri:
    /// - JWT access token + refresh token authentication
    /// - Refresh token rotation (her yenilemede yeni token)
    /// - Token deny list entegrasyonu (logout sonrası token geçersizleştirme)
    /// - Rol doğrulaması (sadece "Courier" rolündeki kullanıcılar)
    /// - IP adresi loglama
    /// - Kurye entity bağlantı kontrolü
    /// 
    /// Mimari Notlar:
    /// - Mevcut AuthManager'dan bağımsız, kurye'ye özel authentication
    /// - Aynı RefreshToken repository kullanılır (User bazlı)
    /// - ITokenDenyList ile logout güvenliği sağlanır
    /// </summary>
    public class CourierAuthManager : ICourierAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenDenyList _tokenDenyList;
        private readonly ECommerceDbContext _context;
        private readonly ILogger<CourierAuthManager> _logger;

        // Kurye rol adı sabiti
        private const string COURIER_ROLE = "Courier";

        public CourierAuthManager(
            UserManager<User> userManager,
            IConfiguration config,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenDenyList tokenDenyList,
            ECommerceDbContext context,
            ILogger<CourierAuthManager> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _tokenDenyList = tokenDenyList ?? throw new ArgumentNullException(nameof(tokenDenyList));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Public Methods

        /// <summary>
        /// Kurye giriş işlemi.
        /// E-posta + şifre doğrulaması yapılır, ardından rol ve Courier kaydı kontrol edilir.
        /// </summary>
        public async Task<CourierLoginResponseDto> LoginAsync(CourierLoginDto dto, string? ipAddress = null)
        {
            try
            {
                _logger.LogInformation("Kurye giriş denemesi: {Email}, IP: {IpAddress}", dto.Email, ipAddress ?? "unknown");

                // 1. Kullanıcıyı e-posta ile bul
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Kurye giriş başarısız - kullanıcı bulunamadı: {Email}", dto.Email);
                    return CreateFailResponse("Geçersiz e-posta veya şifre.");
                }

                // 2. Kullanıcının aktif olduğunu kontrol et
                if (!user.IsActive)
                {
                    _logger.LogWarning("Kurye giriş başarısız - hesap pasif: {Email}", dto.Email);
                    return CreateFailResponse("Hesabınız pasif durumda. Lütfen yönetici ile iletişime geçin.");
                }

                // 3. Şifreyi doğrula
                var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
                if (!passwordValid)
                {
                    _logger.LogWarning("Kurye giriş başarısız - yanlış şifre: {Email}", dto.Email);
                    return CreateFailResponse("Geçersiz e-posta veya şifre.");
                }

                // 4. Rol kontrolü - Sadece "Courier" rolündeki kullanıcılar girebilir
                if (!string.Equals(user.Role, COURIER_ROLE, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Kurye giriş başarısız - yetkisiz rol: {Email}, Role: {Role}", dto.Email, user.Role);
                    return CreateFailResponse("Bu hesap kurye yetkisine sahip değil.");
                }

                // 5. Courier entity kaydını kontrol et
                var courier = await _context.Couriers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (courier == null)
                {
                    _logger.LogWarning("Kurye giriş başarısız - Courier kaydı bulunamadı: UserId={UserId}", user.Id);
                    return CreateFailResponse("Kurye kaydı bulunamadı. Lütfen yönetici ile iletişime geçin.");
                }

                // 6. Token çifti oluştur
                var (accessToken, refreshToken, expiresIn) = await CreateTokenPairAsync(user, ipAddress, dto.RememberMe);

                // 7. Son giriş tarihini güncelle
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // 8. Kurye durumunu online yap
                courier.Status = "active";
                courier.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Kurye giriş başarılı: {Email}, CourierId: {CourierId}", dto.Email, courier.Id);

                return new CourierLoginResponseDto
                {
                    Success = true,
                    Message = "Giriş başarılı.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = expiresIn,
                    Courier = MapToCourierInfo(courier, user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye giriş hatası: {Email}", dto.Email);
                return CreateFailResponse("Giriş sırasında bir hata oluştu. Lütfen tekrar deneyin.");
            }
        }

        /// <summary>
        /// Token yenileme işlemi.
        /// Access token (süresi dolmuş olabilir) + refresh token ile yeni token çifti alınır.
        /// </summary>
        public async Task<CourierTokenRefreshResponseDto> RefreshTokenAsync(CourierTokenRefreshDto dto, string? ipAddress = null)
        {
            try
            {
                _logger.LogDebug("Kurye token yenileme isteği, IP: {IpAddress}", ipAddress ?? "unknown");

                // 1. Access token'dan kullanıcı bilgilerini çıkar (süresi dolmuş olsa bile)
                var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT anahtarı tanımlı değil.");
                var principal = JwtTokenHelper.GetPrincipalFromExpiredToken(dto.AccessToken, key);

                if (principal == null)
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - geçersiz access token");
                    return CreateRefreshFailResponse("Geçersiz access token.");
                }

                // 2. User ID'yi claim'lerden al
                var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                                  ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - geçersiz user id claim");
                    return CreateRefreshFailResponse("Geçersiz token payload.");
                }

                // 3. JTI'yi al
                var jti = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - JTI bulunamadı");
                    return CreateRefreshFailResponse("Geçersiz token payload.");
                }

                // 4. Refresh token'ı veritabanından doğrula
                var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(dto.RefreshToken);
                if (storedRefreshToken == null || storedRefreshToken.UserId != userId)
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - refresh token bulunamadı, UserId: {UserId}", userId);
                    return CreateRefreshFailResponse("Refresh token bulunamadı.");
                }

                // 5. Refresh token tekrar kullanım kontrolü (security)
                if (storedRefreshToken.RevokedAt.HasValue)
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - refresh token tekrar kullanım denemesi, UserId: {UserId}", userId);
                    // Güvenlik: Tekrar kullanım tespit edildiğinde tüm token'ları invalidate et
                    await InvalidateAllUserTokensAsync(userId);
                    return CreateRefreshFailResponse("Güvenlik ihlali tespit edildi. Lütfen tekrar giriş yapın.");
                }

                // 6. Refresh token süre kontrolü
                if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - refresh token süresi dolmuş, UserId: {UserId}", userId);
                    return CreateRefreshFailResponse("Oturum süresi dolmuş. Lütfen tekrar giriş yapın.");
                }

                // 7. JTI eşleşme kontrolü
                if (!string.Equals(storedRefreshToken.JwtId, jti, StringComparison.Ordinal))
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - JTI eşleşmiyor, UserId: {UserId}", userId);
                    return CreateRefreshFailResponse("Token eşleşmiyor.");
                }

                // 8. Kullanıcıyı bul ve Courier rolü doğrula
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - kullanıcı bulunamadı, UserId: {UserId}", userId);
                    return CreateRefreshFailResponse("Kullanıcı bulunamadı.");
                }

                // 9. Hala Courier rolünde mi kontrol et
                if (!string.Equals(user.Role, COURIER_ROLE, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Kurye token yenileme başarısız - rol değişmiş, UserId: {UserId}, Role: {Role}", userId, user.Role);
                    return CreateRefreshFailResponse("Kurye yetkiniz kaldırılmış. Lütfen yönetici ile iletişime geçin.");
                }

                // 10. Eski refresh token'ı revoke et (rotation)
                storedRefreshToken.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

                // 11. Yeni token çifti oluştur
                var (newAccessToken, newRefreshToken, expiresIn) = await CreateTokenPairAsync(user, ipAddress, false);

                _logger.LogInformation("Kurye token yenileme başarılı, UserId: {UserId}", userId);

                return new CourierTokenRefreshResponseDto
                {
                    Success = true,
                    Message = "Token yenileme başarılı.",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = expiresIn
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye token yenileme hatası");
                return CreateRefreshFailResponse("Token yenileme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kurye çıkış işlemi.
        /// Mevcut token deny list'e eklenir ve tüm refresh token'lar revoke edilir.
        /// </summary>
        public async Task<bool> LogoutAsync(int userId, string currentJti, DateTimeOffset tokenExpiration)
        {
            try
            {
                _logger.LogInformation("Kurye çıkış işlemi başlatıldı, UserId: {UserId}", userId);

                // 1. Mevcut access token'ı deny list'e ekle
                if (!string.IsNullOrWhiteSpace(currentJti))
                {
                    await _tokenDenyList.AddAsync(currentJti, tokenExpiration);
                }

                // 2. Tüm refresh token'ları revoke et
                await InvalidateAllUserTokensAsync(userId);

                // 3. Kurye durumunu offline yap
                var courier = await _context.Couriers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (courier != null)
                {
                    courier.Status = "offline";
                    courier.LastActiveAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Kurye çıkış başarılı, UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye çıkış hatası, UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Kurye şifre değiştirme işlemi.
        /// </summary>
        public async Task<(bool success, string message)> ChangePasswordAsync(int userId, CourierChangePasswordDto dto)
        {
            try
            {
                _logger.LogInformation("Kurye şifre değiştirme isteği, UserId: {UserId}", userId);

                // 1. Kullanıcıyı bul
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Şifre değiştirme başarısız - kullanıcı bulunamadı, UserId: {UserId}", userId);
                    return (false, "Kullanıcı bulunamadı.");
                }

                // 2. Rol kontrolü
                if (!string.Equals(user.Role, COURIER_ROLE, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Şifre değiştirme başarısız - yetkisiz rol, UserId: {UserId}", userId);
                    return (false, "Bu işlem için yetkiniz yok.");
                }

                // 3. Şifre değiştir (Identity framework mevcut şifreyi doğrular)
                var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Şifre değiştirme başarısız - Identity hatası, UserId: {UserId}, Errors: {Errors}", userId, errors);
                    return (false, errors);
                }

                // 4. Tüm oturumları sonlandır (güvenlik için)
                await InvalidateAllUserTokensAsync(userId);

                _logger.LogInformation("Kurye şifre değiştirme başarılı, UserId: {UserId}", userId);
                return (true, "Şifreniz başarıyla değiştirildi. Lütfen tekrar giriş yapın.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye şifre değiştirme hatası, UserId: {UserId}", userId);
                return (false, "Şifre değiştirme sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Admin tarafından kurye şifresi sıfırlama.
        /// </summary>
        public async Task<(bool success, string message)> AdminResetPasswordAsync(AdminResetCourierPasswordDto dto)
        {
            try
            {
                _logger.LogInformation("Admin kurye şifre sıfırlama isteği, CourierId: {CourierId}", dto.CourierId);

                // 1. Courier kaydını bul
                var courier = await _context.Couriers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == dto.CourierId);

                if (courier == null)
                {
                    _logger.LogWarning("Admin şifre sıfırlama başarısız - kurye bulunamadı, CourierId: {CourierId}", dto.CourierId);
                    return (false, "Kurye bulunamadı.");
                }

                var user = courier.User;
                if (user == null)
                {
                    _logger.LogWarning("Admin şifre sıfırlama başarısız - kullanıcı bulunamadı, CourierId: {CourierId}", dto.CourierId);
                    return (false, "Kurye kullanıcı kaydı bulunamadı.");
                }

                // 2. Şifreyi sıfırla
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Admin şifre sıfırlama başarısız - Identity hatası, CourierId: {CourierId}, Errors: {Errors}", dto.CourierId, errors);
                    return (false, errors);
                }

                // 3. Tüm oturumları sonlandır
                await InvalidateAllUserTokensAsync(user.Id);

                // 4. MustChangePasswordOnLogin flag'i (opsiyonel - User entity'ye eklenebilir)
                // TODO: User entity'ye MustChangePasswordOnNextLogin property eklenebilir

                _logger.LogInformation("Admin kurye şifre sıfırlama başarılı, CourierId: {CourierId}, UserId: {UserId}", dto.CourierId, user.Id);
                return (true, "Kurye şifresi başarıyla sıfırlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin kurye şifre sıfırlama hatası, CourierId: {CourierId}", dto.CourierId);
                return (false, "Şifre sıfırlama sırasında bir hata oluştu.");
            }
        }

        /// <summary>
        /// Kurye bilgilerini user ID ile getirir.
        /// </summary>
        public async Task<CourierInfoDto?> GetCourierByUserIdAsync(int userId)
        {
            try
            {
                var courier = await _context.Couriers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (courier == null || courier.User == null)
                {
                    return null;
                }

                return MapToCourierInfo(courier, courier.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye bilgileri getirme hatası, UserId: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Kullanıcının geçerli bir Courier olup olmadığını kontrol eder.
        /// </summary>
        public async Task<bool> ValidateCourierAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null || !user.IsActive)
                {
                    return false;
                }

                if (!string.Equals(user.Role, COURIER_ROLE, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var courierExists = await _context.Couriers.AnyAsync(c => c.UserId == userId);
                return courierExists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye doğrulama hatası, UserId: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// JWT access token ve refresh token çifti oluşturur.
        /// </summary>
        private async Task<(string accessToken, string refreshToken, int expiresIn)> CreateTokenPairAsync(
            User user, 
            string? ipAddress, 
            bool extendedRefresh)
        {
            // JWT ayarlarını oku
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT anahtarı tanımlı değil.");
            var issuer = _config["Jwt:Issuer"] ?? string.Empty;
            var audience = _config["Jwt:Audience"] ?? string.Empty;

            // Access token süresi
            var accessTokenMinutes = GetAccessTokenLifetimeMinutes();

            // Refresh token süresi (RememberMe seçiliyse uzat)
            var refreshTokenDays = extendedRefresh 
                ? GetRefreshTokenLifetimeDays() * 2  // RememberMe: 2x süre
                : GetRefreshTokenLifetimeDays();

            // Unique JTI oluştur
            var jti = Guid.NewGuid().ToString();

            // JWT access token oluştur
            var accessToken = JwtTokenHelper.GenerateToken(
                user.Id,
                user.Email ?? string.Empty,
                user.Role ?? COURIER_ROLE,
                key,
                issuer,
                audience,
                accessTokenMinutes,
                jti
            );

            // Refresh token oluştur
            var refreshTokenValue = GenerateSecureRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                JwtId = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays),
                CreatedIp = ipAddress
            };

            await _refreshTokenRepository.AddAsync(refreshToken);

            return (accessToken, refreshTokenValue, accessTokenMinutes * 60); // saniye cinsinden
        }

        /// <summary>
        /// Güvenli refresh token üretir (cryptographically secure).
        /// </summary>
        private static string GenerateSecureRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Kullanıcının tüm refresh token'larını revoke eder.
        /// </summary>
        private async Task InvalidateAllUserTokensAsync(int userId)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);
            var now = DateTime.UtcNow;

            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                await _refreshTokenRepository.UpdateAsync(token);
            }
        }

        /// <summary>
        /// Access token süresini dakika cinsinden alır.
        /// </summary>
        private int GetAccessTokenLifetimeMinutes()
        {
            var minutes = _config.GetValue<int?>("AppSettings:JwtExpirationInMinutes")
                         ?? _config.GetValue<int?>("Jwt:AccessTokenMinutes");
            return minutes.HasValue && minutes.Value > 0 ? minutes.Value : 120; // Default: 2 saat
        }

        /// <summary>
        /// Refresh token süresini gün cinsinden alır.
        /// </summary>
        private int GetRefreshTokenLifetimeDays()
        {
            var days = _config.GetValue<int?>("Jwt:RefreshTokenDays")
                      ?? _config.GetValue<int?>("AppSettings:RefreshTokenDays");
            return days.HasValue && days.Value > 0 ? days.Value : 14; // Default: 14 gün
        }

        /// <summary>
        /// Courier entity'yi CourierInfoDto'ya dönüştürür.
        /// </summary>
        private static CourierInfoDto MapToCourierInfo(Courier courier, User user)
        {
            return new CourierInfoDto
            {
                CourierId = courier.Id,
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = !string.IsNullOrEmpty(user.FullName) 
                    ? user.FullName 
                    : $"{user.FirstName} {user.LastName}".Trim(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = courier.Phone ?? user.PhoneNumber,
                Vehicle = courier.Vehicle,
                Status = courier.Status,
                Location = courier.Location,
                Rating = courier.Rating,
                ActiveOrders = courier.ActiveOrders,
                CompletedToday = courier.CompletedToday,
                LastActiveAt = courier.LastActiveAt
            };
        }

        /// <summary>
        /// Başarısız login yanıtı oluşturur.
        /// </summary>
        private static CourierLoginResponseDto CreateFailResponse(string message)
        {
            return new CourierLoginResponseDto
            {
                Success = false,
                Message = message,
                AccessToken = null,
                RefreshToken = null,
                ExpiresIn = 0,
                Courier = null
            };
        }

        /// <summary>
        /// Başarısız token refresh yanıtı oluşturur.
        /// </summary>
        private static CourierTokenRefreshResponseDto CreateRefreshFailResponse(string message)
        {
            return new CourierTokenRefreshResponseDto
            {
                Success = false,
                Message = message,
                AccessToken = null,
                RefreshToken = null,
                ExpiresIn = 0
            };
        }

        #endregion
    }
}
