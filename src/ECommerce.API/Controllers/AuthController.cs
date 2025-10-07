using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
//		○ Auth: JWT + refresh token. UsersController ve AuthController.
//		○ CORS, Rate limiting, HSTS, HTTPS redirection.
/*AuthController
•	POST /api/auth/login -> { email, password } -> { token }
•	POST /api/auth/register -> register user
*/
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
        var token = await _authService.RegisterAsync(dto);
        return Ok(new { Token = token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var token = await _authService.LoginAsync(dto);
        return Ok(new { Token = token });
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

}
