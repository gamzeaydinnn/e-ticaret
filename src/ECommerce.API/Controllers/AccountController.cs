using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Entities.Concrete;
using ECommerce.Business.Services.Interfaces;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Hesap yönetimi controller'ı.
    /// Giriş yapmış kullanıcının kendi bilgilerini yönetmesi için endpoint'ler sağlar.
    /// </summary>
    [ApiController]
    [Route("api/account")]
    [Authorize] // Tüm endpoint'ler authenticate olmayı gerektirir
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            IUserService userService,
            IAuditLogService auditLogService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _userService = userService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        /// <summary>
        /// Giriş yapmış kullanıcının profil bilgilerini getirir.
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Address,
                    user.City,
                    user.Role,
                    user.IsActive,
                    user.CreatedAt,
                    user.LastLoginAt
                }
            });
        }

        /// <summary>
        /// Giriş yapmış kullanıcının kendi bilgilerini günceller.
        /// Ad, Soyad, Email, Telefon, Adres, Şehir güncellenebilir.
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            var oldSnapshot = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Address,
                user.City
            };

            // Profil bilgilerini güncelle
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.FullName = $"{dto.FirstName} {dto.LastName}";
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.City = dto.City;
            user.UpdatedAt = DateTime.UtcNow;

            // Email değiştiğinde Identity'deki email'i de güncelle
            if (user.Email != oldSnapshot.Email)
            {
                var emailUpdateResult = await _userManager.SetEmailAsync(user, dto.Email);
                if (!emailUpdateResult.Succeeded)
                {
                    _logger.LogWarning("Email güncelleme başarısız: {Errors}",
                        string.Join(", ", emailUpdateResult.Errors.Select(e => e.Description)));
                }
            }

            await _userService.UpdateAsync(user);

            await _auditLogService.WriteAsync(
                userId,
                "ProfileUpdated",
                "User",
                userId.ToString(),
                oldSnapshot,
                new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.Address,
                    user.City
                });

            return Ok(new
            {
                success = true,
                message = "Profil başarıyla güncellendi.",
                data = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.Address,
                    user.City
                }
            });
        }

        /// <summary>
        /// Giriş yapmış kullanıcının şifresini değiştirir.
        /// Mevcut şifre doğrulanır, ardından yeni şifre ayarlanır.
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] AccountChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            // Mevcut şifreyi doğrula
            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!checkPasswordResult)
            {
                return BadRequest(new { success = false, message = "Mevcut şifre hatalı." });
            }

            // Yeni şifreyi ayarla
            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = $"Şifre değiştirme başarısız: {errors}" });
            }

            // SecurityStamp güncelle - tüm oturumlar invalidate edilsin
            await _userManager.UpdateSecurityStampAsync(user);

            await _auditLogService.WriteAsync(
                userId,
                "PasswordChanged",
                "User",
                userId.ToString(),
                null,
                null);

            _logger.LogInformation("Kullanıcı şifresini değiştirdi: {UserId}", userId);

            return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
        }

        /// <summary>
        /// Token'dan kullanıcı ID'sini çıkarır.
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("nameid")?.Value;

            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }
    }

    /// <summary>
    /// Profil güncelleme DTO
    /// </summary>
    public class ProfileUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
    }

    /// <summary>
    /// Şifre değiştirme DTO (AccountController için özel)
    /// </summary>
    public class AccountChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
