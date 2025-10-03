using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Auth;
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

}
