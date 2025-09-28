using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers; // <- Yeni JwtTokenHelper burada
using ECommerce.Core.DTOs.Auth;
using ECommerce.Business.Services.Interfaces; // <- LoginDto ve RegisterDto için

public class AuthManager : IAuthService
{
    private readonly ECommerceDbContext _context;
    private readonly IConfiguration _config;

    public AuthManager(ECommerceDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<string> RegisterAsync(RegisterDto dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
            throw new Exception("User already exists");

        var user = new User
        {
            Email = dto.Email,
            UserName = dto.Email, // Identity için gerekli
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateJwtToken(user);
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        return JwtTokenHelper.GenerateToken(
            user.Id,
            user.Email,
            user.Role,
            _config["Jwt:Key"],
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            expiresMinutes: 120 // İstersen süresini değiştirebilirsin
        );
    }
}
