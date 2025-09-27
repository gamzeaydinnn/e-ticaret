//using ECommerce.Core.Entities.Concrete;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using ECommerce.Data.Context;
using BCrypt.Net;
using ECommerce.Entities.Concrete;
using ECommerce.Business.Helpers;

public class AuthManager : IAuthService
{
    private readonly ECommerceDbContext _context;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AuthManager(ECommerceDbContext context, JwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
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

        return _jwtTokenGenerator.GenerateToken(user);
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        return _jwtTokenGenerator.GenerateToken(user);
    }
}
