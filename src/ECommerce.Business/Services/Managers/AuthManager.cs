using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Data.Context;
using ECommerce.Infrastructure;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Business.Services.Interfaces;

namespace ECommerce.Business.Services.Managers
{
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
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new Exception("User already exists");

        var user = new User
        {
            Email = dto.Email,
            UserName = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "User",
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateJwtToken(user);
    }

    public async Task<string> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            throw new Exception("Kullanıcı bulunamadı");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Şifre yanlış");

        return GenerateJwtToken(user);
    }

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

    private string GenerateJwtToken(User user)
    {
        return JwtTokenHelper.GenerateToken(
            user.Id,
            user.Email,
            user.Role,
            _config["Jwt:Key"],
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            expiresMinutes: 120
        );
    }

    public async Task<string> RefreshTokenAsync(string token, string refreshToken)
    {
        var principal = JwtTokenHelper.GetPrincipalFromExpiredToken(token, _config["Jwt:Key"]);
        if (principal == null)
            throw new Exception("Invalid token");

        var userEmail = principal.Identity?.Name;
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
            throw new Exception("User not found");

        return GenerateJwtToken(user);
    }

    public async Task RevokeRefreshTokenAsync(int userId)
    {
        // Refresh token yönetimi eklenecekse burada invalidate edilir
        await Task.CompletedTask;
    }
    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            // Güvenlik nedeniyle, kullanıcı bulunamasa bile hata fırlatmıyoruz.
            // Sadece işlem yapmadan dönüyoruz.
            return;
        }

        // Güvenli bir token oluşturuyoruz (örn: GUID)
        user.PasswordResetToken = Guid.NewGuid().ToString();
        // Bu token'ı 1 saat geçerli kılıyoruz
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        // TODO: E-posta gönderme servisi burada çağrılacak.
        // Kullanıcıya user.PasswordResetToken'ı içeren bir link gönderilmelidir.
        // Örn: emailService.SendPasswordResetEmail(user.Email, user.PasswordResetToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || user.PasswordResetToken != dto.Token || user.ResetTokenExpires < DateTime.UtcNow)
        {
            throw new Exception("Invalid or expired password reset token.");
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new Exception("Passwords do not match.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        // Token'ı kullandıktan sonra temizliyoruz.
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;

        await _context.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
        {
            throw new Exception("Invalid old password.");
        }
        
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new Exception("New passwords do not match.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
    }
}
}