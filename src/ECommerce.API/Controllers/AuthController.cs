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
}
