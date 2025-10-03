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
    [HttpGet("me")]
public async Task<IActionResult> Me()
{
    // Token'dan user id veya email claim çek
    var userId = User.FindFirst("sub")?.Value; // sub claim genelde userId olur
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    var user = await _authService.GetUserByIdAsync(Guid.Parse(userId));
    if (user == null) return NotFound();

    return Ok(user); // burada istersen DTO dönebilirsin
}

}
