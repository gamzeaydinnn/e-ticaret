using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using ECommerce.Entities.Concrete;
using System.Security.Claims;

//		○ Auth: JWT + refresh token. UsersController ve AuthController.
//		○ CORS, Rate limiting, HSTS, HTTPS redirection.
/*AuthController
•	POST /api/auth/login -> { email, password } -> { token }
•	POST /api/auth/register -> register user
*/
namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly ECommerce.Core.Interfaces.ITokenDenyList _denyList;
        private readonly ECommerce.Business.Services.Interfaces.ILoginRateLimitService _loginRateLimitService;

        public AuthController(IAuthService authService, IConfiguration config, UserManager<User> userManager, ECommerce.Core.Interfaces.ITokenDenyList denyList, ECommerce.Business.Services.Interfaces.ILoginRateLimitService loginRateLimitService)
        {
            _authService = authService;
            _config = config;
            _userManager = userManager;
            _denyList = denyList;
            _loginRateLimitService = loginRateLimitService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok(new {
                    Message = "Kayıt başarılı! E-posta doğrulama linki gönderildi. Lütfen e-postanızı doğrulayın.",
                    EmailVerificationRequired = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("social-login")]
        [AllowAnonymous]
        public async Task<IActionResult> SocialLogin([FromBody] SocialLoginRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Provider))
                return BadRequest(new { Message = "Invalid provider" });

            var provider = dto.Provider.Trim().ToLowerInvariant();
            string? email = null;
            string? name = null;

            try
            {
                if (provider == "google")
                {
                    var allowDev = _config.GetValue<bool>("OAuth:AllowDevSocialLogin");
                    var clientId = _config["OAuth:GoogleClientId"];
                    if (!string.IsNullOrWhiteSpace(dto.IdToken) && !string.IsNullOrWhiteSpace(clientId))
                    {
                        var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, new GoogleJsonWebSignature.ValidationSettings
                        {
                            Audience = new[] { clientId }
                        });
                        email = payload.Email;
                        name = payload.Name;
                    }
                    else if (allowDev)
                    {
                        email = dto.Email ?? $"google_user_{Guid.NewGuid():N}@local";
                        name = dto.Name ?? "Google User";
                    }
                    else
                    {
                        return BadRequest(new { Message = "Google OAuth not configured" });
                    }
                }
                else if (provider == "facebook")
                {
                    // Basit/dev: yapılandırma yoksa e-posta ile devam et
                    var allowDev = _config.GetValue<bool>("OAuth:AllowDevSocialLogin");
                    if (allowDev && !string.IsNullOrWhiteSpace(dto.Email))
                    {
                        email = dto.Email;
                        name = dto.Name ?? "Facebook User";
                    }
                    else
                    {
                        return BadRequest(new { Message = "Facebook OAuth not configured" });
                    }
                }
                else
                {
                    return BadRequest(new { Message = "Unsupported provider" });
                }

                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { Message = "Email could not be resolved" });

                // Kullanıcıyı bul veya oluştur
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    var first = name?.Split(' ').FirstOrDefault() ?? "User";
                    var last = string.Join(' ', (name?.Split(' ').Skip(1) ?? Array.Empty<string>()));
                    user = new User
                    {
                        Email = email,
                        UserName = email,
                        FirstName = first,
                        LastName = last,
                        FullName = name ?? (first + " " + last).Trim(),
                        Role = "User",
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };
                    var create = await _userManager.CreateAsync(user);
                    if (!create.Succeeded)
                    {
                        var err = string.Join(", ", create.Errors.Select(e => e.Description));
                        return BadRequest(new { Message = err });
                    }
                }

                // JWT üret
                var (token, refreshToken) = await _authService.IssueTokensForUserAsync(user, HttpContext.Connection.RemoteIpAddress?.ToString());

                var respUser = new { id = user.Id, email = user.Email, firstName = user.FirstName, lastName = user.LastName, name = user.FullName, role = user.Role };

                return Ok(new { token, refreshToken, user = respUser, success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(new { Message = "Geçersiz istek" });

                if (_loginRateLimitService != null && _loginRateLimitService.IsBlocked(dto.Email, out var remaining))
                {
                    var mins = Math.Ceiling(remaining.TotalMinutes);
                    return BadRequest(new { Message = $"Çok fazla başarısız deneme. Lütfen {mins} dakika sonra tekrar deneyin." });
                }

                var (token, refreshToken) = await _authService.LoginAsync(dto);

                try
                {
                    // Başarılı girişte sayaç sıfırlanmalı
                    _loginRateLimitService?.ResetAttempts(dto.Email);
                }
                catch
                {
                    // ignore cache errors
                }

                // Kullanıcı bilgilerini de döndür (frontend için gerekli)
                var user = await _userManager.FindByEmailAsync(dto.Email);
                var isAdmin = user?.Role == "Admin" || user?.Role == "SuperAdmin";
                var userResponse = user != null ? new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    name = !string.IsNullOrEmpty(user.FullName) ? user.FullName : $"{user.FirstName} {user.LastName}".Trim(),
                    role = user.Role,
                    isAdmin = isAdmin
                } : null;

                return Ok(new { 
                    Token = token,
                    RefreshToken = refreshToken,
                    User = userResponse,
                    Success = true,
                    Message = "Giriş başarılı!"
                });
            }
            catch (Exception ex)
            {
                try
                {
                    if (dto != null && !string.IsNullOrWhiteSpace(dto.Email) && _loginRateLimitService != null)
                    {
                        var attempts = _loginRateLimitService.IncrementFailedAttempt(dto.Email);
                        if (attempts >= 5)
                        {
                            return BadRequest(new { Message = "Çok fazla başarısız deneme. 15 dakika boyunca bloke edildiniz." });
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh(TokenRefreshDto dto)
        {
            try
            {
                var (newToken, newRefreshToken) = await _authService.RefreshTokenAsync(dto.Token, dto.RefreshToken);
                return Ok(new { Token = newToken, RefreshToken = newRefreshToken });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!int.TryParse(userId, out var parsedUserId))
                return BadRequest("Invalid user id in token");

            var user = await _authService.GetUserByIdAsync(parsedUserId);
            if (user == null) return NotFound();

            return Ok(user);
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            await _authService.ForgotPasswordAsync(dto);
            // Kullanıcı e-postasının sistemde olup olmadığı bilgisini sızdırmamak için her zaman başarılı mesajı dönüyoruz.
            return Ok(new { Message = "Eğer bu e-posta adresi sistemimizde kayıtlıysa, şifre sıfırlama talimatları gönderilmiştir." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { Message = "Şifreniz başarıyla güncellenmiştir." });
        }

        [HttpPost("change-password")]
        [Authorize] // Bu endpoint'e sadece giriş yapmış kullanıcılar erişebilmeli
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdString = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new { Message = "Şifreniz başarıyla değiştirilmiştir." });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // JWT token'ı geçersiz kılma işlemi
                // Gerçek projede token blacklist'e eklenebilir veya refresh token silinebilir
                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
                {
                    await _authService.InvalidateUserTokensAsync(userId);
                }

                // Add current access token jti to deny list so subsequent requests with same token are rejected
                try
                {
                    var jti = User.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrWhiteSpace(jti) && _denyList != null)
                    {
                        var expClaim = User.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(5);
                        if (!string.IsNullOrWhiteSpace(expClaim) && long.TryParse(expClaim, out var expSeconds))
                        {
                            expiration = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
                        }

                        await _denyList.AddAsync(jti, expiration);
                    }
                }
                catch
                {
                    // ignore deny-list errors during logout to avoid blocking sign-out
                }

                return Ok(new { success = true, message = "Başarıyla çıkış yapıldı!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Çıkış sırasında bir hata oluştu!", error = ex.Message });
            }
        }


        // E-posta doğrulama linki (GET, e-posta ile gönderilen link)
        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromQuery] int userId, [FromQuery] string token)
        {
            var ok = await _authService.ConfirmEmailAsync(userId, token);
            if (!ok) return BadRequest(new { Message = "Doğrulama başarısız veya token geçersiz." });
            return Ok(new { Message = "E-posta başarıyla doğrulandı. Artık giriş yapabilirsiniz." });
        }

        // E-posta doğrulama mailini tekrar gönder
        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ECommerce.Core.DTOs.Auth.ForgotPasswordDto dto)
        {
            // Mevcut ForgotPasswordDto sadece Email içeriyor; burada yeniden kullanıyoruz
            var ok = await _authService.ResendConfirmationEmailAsync(dto.Email);
            return Ok(new { Message = "Eğer e-posta sistemde kayıtlı ve doğrulanmamışsa, doğrulama linki gönderildi." });
        }

        // Sadece Development ortamında test kolaylığı için doğrulama linki üretir
        [HttpGet("dev/confirm-token")]
        [AllowAnonymous]
        public async Task<IActionResult> DevConfirmToken([FromQuery] string email)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();

            var user = await _authService.GetUserByEmailAsync(email);
            if (user == null) return NotFound(new { Message = "User not found" });

            // UserLoginDto dönüyor, bu yüzden tekrar çekelim
            // Basitçe Identity User Manager üzerinden token üretmek için service resolution yapalım
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ECommerce.Entities.Concrete.User>>();
            var fullUser = await userManager.FindByEmailAsync(email);
            if (fullUser == null) return NotFound(new { Message = "User not found" });

            var token = await userManager.GenerateEmailConfirmationTokenAsync(fullUser);
            var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            var baseUrl = HttpContext.RequestServices.GetRequiredService<IConfiguration>()["AppSettings:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var confirmUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? $"/api/auth/confirm-email?userId={fullUser.Id}&token={encoded}"
                : $"{baseUrl}/api/auth/confirm-email?userId={fullUser.Id}&token={encoded}";
            return Ok(new { confirmUrl });
        }

        #region SMS Doğrulama ile Kayıt

        /// <summary>
        /// Telefon numarası ile kayıt işlemi başlatır.
        /// 
        /// Kullanıcı oluşturulur (inactive) ve telefon numarasına SMS kodu gönderilir.
        /// </summary>
        /// <param name="dto">Kayıt bilgileri (Email, Password, FirstName, LastName, PhoneNumber)</param>
        /// <returns>Başarılı olursa userId döner</returns>
        /// <response code="200">Kayıt başarılı, SMS gönderildi</response>
        /// <response code="400">Geçersiz istek veya kullanıcı zaten var</response>
        [HttpPost("register-with-phone")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterWithPhone([FromBody] RegisterDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Geçersiz istek parametreleri." });
                }

                var (success, message, userId) = await _authService.RegisterWithPhoneAsync(dto);

                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                return Ok(new
                {
                    Success = true,
                    Message = message,
                    UserId = userId,
                    PhoneVerificationRequired = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Telefon doğrulama kodunu kontrol eder ve hesabı aktif eder.
        /// 
        /// Başarılı olursa JWT token döner ve kullanıcı giriş yapmış olur.
        /// </summary>
        /// <param name="dto">Doğrulama bilgileri (PhoneNumber, Code, Email)</param>
        /// <returns>JWT access token ve refresh token</returns>
        /// <response code="200">Doğrulama başarılı, token döndü</response>
        /// <response code="400">Yanlış kod veya geçersiz istek</response>
        [HttpPost("verify-phone-registration")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyPhoneRegistration([FromBody] VerifyPhoneRegistrationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Geçersiz istek parametreleri." });
                }

                var (success, message, accessToken, refreshToken) = 
                    await _authService.VerifyPhoneRegistrationAsync(dto);

                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                return Ok(new
                {
                    Success = true,
                    Message = message,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion

        #region Telefon ile Şifre Sıfırlama

        /// <summary>
        /// Telefon numarası ile şifre sıfırlama kodu gönderir.
        /// 
        /// Telefon numarasına SMS doğrulama kodu gönderilir.
        /// </summary>
        /// <param name="dto">Telefon numarası</param>
        /// <returns>Başarı durumu</returns>
        /// <response code="200">SMS gönderildi</response>
        /// <response code="429">Rate limit aşıldı</response>
        [HttpPost("forgot-password-by-phone")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPasswordByPhone([FromBody] ForgotPasswordByPhoneDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Geçersiz telefon numarası." });
                }

                var (success, message) = await _authService.ForgotPasswordByPhoneAsync(dto);

                if (!success)
                {
                    // Rate limit hatası 429 dönsün
                    if (message.Contains("fazla istek") || message.Contains("bekleyin"))
                    {
                        return StatusCode(StatusCodes.Status429TooManyRequests, new { Message = message });
                    }
                    return BadRequest(new { Message = message });
                }

                return Ok(new
                {
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// SMS doğrulama kodu ile şifre sıfırlar.
        /// 
        /// Doğrulama kodu kontrol edilir ve başarılıysa şifre güncellenir.
        /// </summary>
        /// <param name="dto">Telefon numarası, kod ve yeni şifre</param>
        /// <returns>Başarı durumu</returns>
        /// <response code="200">Şifre başarıyla değiştirildi</response>
        /// <response code="400">Yanlış kod veya geçersiz istek</response>
        [HttpPost("reset-password-by-phone")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPasswordByPhone([FromBody] ResetPasswordByPhoneDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "Geçersiz istek parametreleri." });
                }

                var (success, message) = await _authService.ResetPasswordByPhoneAsync(dto);

                if (!success)
                {
                    return BadRequest(new { Message = message });
                }

                return Ok(new
                {
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        #endregion

        // Development helper: generate and immediately confirm server-side
        [HttpPost("dev/confirm-direct")]
        [AllowAnonymous]
        public async Task<IActionResult> DevConfirmDirect([FromQuery] string email)
        {
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();

            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ECommerce.Entities.Concrete.User>>();
            var fullUser = await userManager.FindByEmailAsync(email);
            if (fullUser == null) return NotFound(new { Message = "User not found" });
            var token = await userManager.GenerateEmailConfirmationTokenAsync(fullUser);
            var result = await userManager.ConfirmEmailAsync(fullUser, token);
            return Ok(new { success = result.Succeeded });
        }

        // 🔧 GEÇICI DEVELOPMENT ENDPOINT - Production'da kaldırılmalı
        // Admin şifresini admin123 olarak ayarlamak için
        [HttpPost("dev-reset-admin-password")]
        [AllowAnonymous]
        public async Task<IActionResult> DevResetAdminPassword()
        {
            #if DEBUG
            try
            {
                var adminUser = await _userManager.FindByEmailAsync("admin@admin.com");
                if (adminUser == null)
                {
                    return NotFound(new { message = "Admin kullanıcısı bulunamadı" });
                }

                // Mevcut şifreyi kaldır
                await _userManager.RemovePasswordAsync(adminUser);
                
                // Yeni şifre: admin123
                var result = await _userManager.AddPasswordAsync(adminUser, "admin123");
                
                if (result.Succeeded)
                {
                    return Ok(new { 
                        message = "Admin şifresi başarıyla 'admin123' olarak ayarlandı",
                        email = "admin@admin.com",
                        password = "admin123"
                    });
                }
                
                return BadRequest(new { 
                    message = "Şifre güncellenemedi", 
                    errors = result.Errors.Select(e => e.Description) 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            #else
            return NotFound();
            #endif
        }
    }
}

public class SocialLoginRequest
{
    public string Provider { get; set; } = string.Empty; // google | facebook
    public string? IdToken { get; set; } // Google için
    public string? AccessToken { get; set; } // Facebook için
    public string? Email { get; set; } // Dev fallback
    public string? Name { get; set; } // Dev fallback
}
