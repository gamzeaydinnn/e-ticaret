using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Entities.Concrete;
using ECommerce.Business.Services.Interfaces;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Hesap yönetimi controller'ı.
    /// Giriş yapmış kullanıcının kendi bilgilerini yönetmesi için endpoint'ler sağlar.
    /// </summary>
    [ApiController]
    [Route("api/account")]
    [Authorize]
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

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

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
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (dto == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
                return BadRequest(new { success = false, message = "Ad ve soyad zorunludur." });

            if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email))
                return BadRequest(new { success = false, message = "Geçerli bir e-posta adresi giriniz." });

            var user = await _userManager.FindByIdAsync(userId.ToString());
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

            var newEmail = dto.Email.Trim();
            var emailChanged = !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);

            if (emailChanged)
            {
                var existing = await _userManager.FindByEmailAsync(newEmail);
                if (existing != null && existing.Id != user.Id)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bu e-posta adresi başka bir hesap tarafından kullanılıyor."
                    });
                }

                var emailResult = await _userManager.SetEmailAsync(user, newEmail);
                if (!emailResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "E-posta güncellenemedi: " + FormatIdentityErrors(emailResult.Errors)
                    });
                }

                // Giriş UserName=Email ile yapıldığı için UserName'i de senkron tut
                var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
                if (!userNameResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Kullanıcı adı güncellenemedi: " + FormatIdentityErrors(userNameResult.Errors)
                    });
                }

                // Oturum açık kullanıcı e-postasını değiştirdiği için onaylı bırak
                user.EmailConfirmed = true;
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.FullName = $"{user.FirstName} {user.LastName}";
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            user.City = string.IsNullOrWhiteSpace(dto.City) ? null : dto.City.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Profil güncellenemedi: " + FormatIdentityErrors(updateResult.Errors)
                });
            }

            await _auditLogService.WriteAsync(
                userId,
                "ProfileUpdated",
                "User",
                userId.ToString(),
                oldSnapshot,
                new
                {
                    message = $"Profil güncellendi: {user.Email}",
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
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] AccountChangePasswordDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized();

            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { success = false, message = "Mevcut şifre ve yeni şifre zorunludur." });
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            var checkPasswordResult = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
            if (!checkPasswordResult)
            {
                return BadRequest(new { success = false, message = "Mevcut şifre hatalı." });
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Şifre değiştirilemedi: " + FormatIdentityErrors(changePasswordResult.Errors)
                });
            }

            await _userManager.UpdateSecurityStampAsync(user);

            await _auditLogService.WriteAsync(
                userId,
                "PasswordChanged",
                "User",
                userId.ToString(),
                null,
                new { message = "Kullanıcı kendi şifresini değiştirdi." });

            _logger.LogInformation("Kullanıcı şifresini değiştirdi: {UserId}", userId);

            return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
        }

        private int GetCurrentUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value
                              ?? User.FindFirst("nameid")?.Value;

            return int.TryParse(userIdValue, out var userId) ? userId : 0;
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(
                email.Trim(),
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
        {
            return string.Join(" ", errors.Select(TranslateIdentityError));
        }

        private static string TranslateIdentityError(IdentityError error)
        {
            var code = error.Code ?? string.Empty;
            return code switch
            {
                "PasswordTooShort" => "Yeni şifre en az 8 karakter olmalıdır.",
                "PasswordRequiresDigit" => "Yeni şifrede en az bir rakam olmalıdır.",
                "PasswordRequiresUpper" => "Yeni şifrede en az bir büyük harf olmalıdır.",
                "PasswordRequiresLower" => "Yeni şifrede en az bir küçük harf olmalıdır.",
                "PasswordRequiresNonAlphanumeric" => "Yeni şifrede en az bir özel karakter olmalıdır.",
                "PasswordMismatch" => "Mevcut şifre hatalı.",
                "DuplicateEmail" => "Bu e-posta adresi zaten kullanılıyor.",
                "DuplicateUserName" => "Bu kullanıcı adı zaten kullanılıyor.",
                "InvalidEmail" => "Geçersiz e-posta adresi.",
                _ => string.IsNullOrWhiteSpace(error.Description)
                    ? "İşlem başarısız oldu."
                    : error.Description
            };
        }
    }

    public class ProfileUpdateDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
    }

    public class AccountChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
