using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ECommerce.Data.Context;
using ECommerce.Infrastructure;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Infrastructure.Services.Email;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace ECommerce.Business.Services.Managers
{
    public class AuthManager : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;
        private readonly EmailSender _emailSender;

        public AuthManager(UserManager<User> userManager, IConfiguration config, EmailSender emailSender)
        {
            _userManager = userManager;
            _config = config;
            _emailSender = emailSender;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new Exception("User already exists");
            }

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.Email,
                Role = "User",
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kayıt işlemi başarısız: {errors}");
            }

            // E-posta doğrulama token'ı üret ve gönder
            await SendEmailConfirmationAsync(user);

            // Not: E-posta doğrulaması zorunlu ise JWT döndürmeyelim
            return string.Empty;
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            if (!user.EmailConfirmed)
            {
                throw new Exception("E-posta adresiniz doğrulanmamış. Lütfen e-postanızı doğrulayın.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                throw new Exception("Şifre yanlış");
            }

            return GenerateJwtToken(user);
        }

        public async Task<UserLoginDto> GetUserByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            return new UserLoginDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
            };
        }

        public async Task<UserLoginDto> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
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
            {
                throw new Exception("Invalid token");
            }

            var userEmail = principal.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                throw new Exception("Invalid token payload");
            }

            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return GenerateJwtToken(user);
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            await Task.CompletedTask;
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return;
            }

            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _userManager.UpdateAsync(user);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || user.PasswordResetToken != dto.Token || user.ResetTokenExpires < DateTime.UtcNow)
            {
                throw new Exception("Invalid or expired password reset token.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception("Passwords do not match.");
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _userManager.UpdateAsync(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new Exception("New passwords do not match.");
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                throw new Exception(errors);
            }
        }

        private async Task SendEmailConfirmationAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var baseUrl = _config["AppSettings:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5001";

            // URL-safe base64 encode
            var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            var confirmUrl = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&token={encodedToken}";

            var subject = "E-posta Doğrulama";
            var body = $"<p>Merhaba {WebUtility.HtmlEncode(user.FirstName)} {WebUtility.HtmlEncode(user.LastName)},</p>" +
                       $"<p>Hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın:</p>" +
                       $"<p><a href=\"{confirmUrl}\">E-posta adresimi doğrula</a></p>" +
                       $"<p>Bağlantı çalışmıyorsa: {WebUtility.HtmlEncode(confirmUrl)}</p>";

            await _emailSender.SendEmailAsync(user.Email!, subject, body, isHtml: true);
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;
            if (string.IsNullOrWhiteSpace(token)) return false;

            // token URL decode
            string decodedToken;
            try
            {
                var bytes = WebEncoders.Base64UrlDecode(token);
                decodedToken = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                decodedToken = WebUtility.UrlDecode(token);
            }
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Email confirm failed: {errors}");
            }
            return result.Succeeded;
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return true; // bilgi sızdırmamak için true dön
            if (user.EmailConfirmed) return true;
            await SendEmailConfirmationAsync(user);
            return true;
        }
    }
}
