using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

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

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
