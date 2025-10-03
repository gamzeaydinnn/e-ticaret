using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Business.Services.Interfaces;

public class AuthManager : IAuthService
{
    private readonly ECommerceDbContext _context;
    private readonly IConfiguration _config;

    public AuthManager(ECommerceDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    // Yeni kullanıcı kaydı
    public async Task<string> RegisterAsync(RegisterDto dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
            throw new Exception("User already exists");

        var user = new User
        {
            Email = dto.Email,
            UserName = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User",
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateJwtToken(user);
    }

    // Login
    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        return GenerateJwtToken(user);
    }

    // Kullanıcıyı Id ile getir
    public async Task<UserLoginDto> GetUserByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
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

    // JWT token üret
    private string GenerateJwtToken(User user)
    {
        return JwtTokenHelper.GenerateToken(
            user.Id, // int olarak gönder
            user.Email,
            user.Role,
            _config["Jwt:Key"],
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            expiresMinutes: 120
        );
    }

    // Refresh token üretme
    public async Task<string> RefreshTokenAsync(string token, string refreshToken)
    {
        // Burada token doğrulama, kullanıcı kontrolü ve yeni JWT üret
        throw new NotImplementedException();
    }

    // Refresh token iptal et
    public async Task RevokeRefreshTokenAsync(int userId)
    {
        // Kullanıcının refresh tokenlarını iptal et
        throw new NotImplementedException();
    }

    public Task RevokeRefreshTokenAsync(Guid userId)
    {
        throw new NotImplementedException();
    }
}
