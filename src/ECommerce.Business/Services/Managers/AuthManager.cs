using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Infrastructure.Services.Email;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using ECommerce.Core.Interfaces;

namespace ECommerce.Business.Services.Managers
{
    public class AuthManager : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly EmailSender _emailSender;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthManager(
            UserManager<User> userManager,
            IConfiguration config,
            EmailSender emailSender,
            IRefreshTokenRepository refreshTokenRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _config = config;
            _emailSender = emailSender;
            _refreshTokenRepository = refreshTokenRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new Exception("User already exists");
            }

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.Email,
                Role = "User",
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kayıt işlemi başarısız: {errors}");
            }

            // E-posta doğrulama token'ı üret ve gönder
            await SendEmailConfirmationAsync(user);

            // Not: E-posta doğrulaması zorunlu ise JWT döndürmeyelim
            return string.Empty;
        }

        public async Task<(string accessToken, string refreshToken)> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            if (!user.EmailConfirmed)
            {
                throw new Exception("E-posta adresiniz doğrulanmamış. Lütfen e-postanızı doğrulayın.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                throw new Exception("Şifre yanlış");
            }

            return await IssueTokensForUserAsync(user, GetClientIpAddress());
        }

        public async Task<UserLoginDto> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            return new UserLoginDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
            };
        }

        public async Task<UserLoginDto> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;
            return new UserLoginDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            };
        }

        public async Task<(string accessToken, string refreshToken)> IssueTokensForUserAsync(User user, string? ipAddress = null)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return await CreateTokenPairAsync(user, ipAddress);
        }

        private string GenerateJwtToken(User user, string? jti = null)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("JWT anahtarı tanımlı değil.");
            var issuer = _config["Jwt:Issuer"] ?? string.Empty;
            var audience = _config["Jwt:Audience"] ?? string.Empty;

            return JwtTokenHelper.GenerateToken(
                user.Id,
                user.Email ?? string.Empty,
                user.Role ?? "User",
                key,
                issuer,
                audience,
                expiresMinutes: GetAccessTokenLifetimeMinutes(),
                jti: jti
            );
        }

        private async Task<(string accessToken, string refreshToken)> CreateTokenPairAsync(User user, string? ipAddress)
        {
            var jti = Guid.NewGuid().ToString();
            var accessToken = GenerateJwtToken(user, jti);
            var refreshTokenValue = GenerateSecureRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                JwtId = jti,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()),
                CreatedIp = ipAddress
            };
            await _refreshTokenRepository.AddAsync(refreshToken);
            return (accessToken, refreshTokenValue);
        }

        private int GetAccessTokenLifetimeMinutes()
        {
            var minutes = _config.GetValue<int?>("AppSettings:JwtExpirationInMinutes");
            return minutes.HasValue && minutes.Value > 0 ? minutes.Value : 120;
        }

        private int GetRefreshTokenLifetimeDays()
        {
            var refreshDays = _config.GetValue<int?>("Jwt:RefreshTokenDays")
                             ?? _config.GetValue<int?>("AppSettings:RefreshTokenDays");
            return refreshDays.HasValue && refreshDays.Value > 0 ? refreshDays.Value : 14;
        }

        private static string GenerateSecureRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        private string? GetClientIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string token, string refreshToken)
        {
            var key = _config["Jwt:Key"] ?? throw new Exception("JWT anahtarı tanımlı değil.");
            var principal = JwtTokenHelper.GetPrincipalFromExpiredToken(token, key);
            if (principal == null)
            {
                throw new Exception("Invalid token");
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                              ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new Exception("Invalid token payload");
            }

            var jti = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrWhiteSpace(jti))
            {
                throw new Exception("Invalid token payload");
            }

            var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (storedRefreshToken == null || storedRefreshToken.UserId != userId)
            {
                throw new Exception("Refresh token bulunamadı.");
            }

            // If token is already revoked, it is a reuse attempt -> unauthorized
            if (storedRefreshToken.RevokedAt.HasValue)
            {
                throw new UnauthorizedAccessException("Refresh token tekrar kullanımı tespit edildi.");
            }

            if (storedRefreshToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new Exception("Refresh token süresi dolmuş.");
            }

            if (!string.Equals(storedRefreshToken.JwtId, jti, StringComparison.Ordinal))
            {
                throw new Exception("Token eşleşmiyor.");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Rotate: revoke the old refresh token and create a new one
            var now = DateTime.UtcNow;
            storedRefreshToken.RevokedAt = now;
            await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

            var newJti = Guid.NewGuid().ToString();
            var newAccessToken = GenerateJwtToken(user, newJti);

            var newRefreshTokenValue = GenerateSecureRefreshToken();
            var newRefreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenValue,
                JwtId = newJti,
                ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()),
                CreatedIp = GetClientIpAddress()
            };

            await _refreshTokenRepository.AddAsync(newRefreshToken);

            return (newAccessToken, newRefreshTokenValue);
        }

        public async Task InvalidateUserTokensAsync(int userId)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);
            var now = DateTime.UtcNow;
            foreach (var token in activeTokens)
            {
                token.RevokedAt = now;
                await _refreshTokenRepository.UpdateAsync(token);
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return;
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _userManager.UpdateAsync(user);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || user.PasswordResetToken != dto.Token || user.ResetTokenExpires < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired password reset token.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _userManager.UpdateAsync(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception("New passwords do not match.");
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                throw new Exception(errors);
            }
        }

        private async Task SendEmailConfirmationAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = _config["AppSettings:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";

            // URL-safe base64 encode
            var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            var confirmUrl = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

            var subject = "E-posta Doğrulama";
            var body = $"<p>Merhaba {WebUtility.HtmlEncode(user.FirstName)} {WebUtility.HtmlEncode(user.LastName)},</p>" +
                       $"<p>Hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın:</p>" +
                       $"<p><a href=\"{confirmUrl}\">E-posta adresimi doğrula</a></p>" +
                       $"<p>Bağlantı çalışmıyorsa: {WebUtility.HtmlEncode(confirmUrl)}</p>";

            await _emailSender.SendEmailAsync(user.Email!, subject, body, isHtml: true);
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;
            if (string.IsNullOrWhiteSpace(token)) return false;

            // token URL decode
            string decodedToken;
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(token);
                decodedToken = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                decodedToken = WebUtility.UrlDecode(token);
            }
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Email confirm failed: {errors}");
            }
            return result.Succeeded;
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return true; // bilgi sızdırmamak için true dön
            if (user.EmailConfirmed) return true;
            await SendEmailConfirmationAsync(user);
            return true;
        }
    }
}
