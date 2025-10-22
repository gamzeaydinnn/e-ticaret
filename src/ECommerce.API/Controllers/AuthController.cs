using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;

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

        public AuthController(IAuthService authService, IConfiguration config, UserManager<User> userManager)
        {
            _authService = authService;
            _config = config;
            _userManager = userManager;
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
                var token = JwtTokenHelper.GenerateToken(
                    user.Id,
                    user.Email!,
                    user.Role ?? "User",
                    _config["Jwt:Key"],
                    _config["Jwt:Issuer"],
                    _config["Jwt:Audience"],
                    120);

                var respUser = new { id = user.Id, email = user.Email, firstName = user.FirstName, lastName = user.LastName, name = user.FullName, role = user.Role };

                return Ok(new { token, user = respUser, success = true });
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
                var token = await _authService.LoginAsync(dto);
                return Ok(new { 
                    Token = token,
                    Message = "Giriş başarılı!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRefreshDto dto)
        {
            var newToken = await _authService.RefreshTokenAsync(dto.Token, dto.RefreshToken);
            return Ok(new { Token = newToken });
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
        public IActionResult Logout()
        {
            try
            {
                // JWT token'ı geçersiz kılma işlemi
                // Gerçek projede token blacklist'e eklenebilir veya refresh token silinebilir
                var userIdString = User.FindFirst("sub")?.Value;

                if (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out var userId))
                {
                    // Kullanıcının tüm aktif sessionlarını sonlandırabilirsiniz
                    // await _authService.InvalidateUserTokensAsync(userId);
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
