using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Infrastructure;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Helpers;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

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

        public async Task RegisterAsync(RegisterDto dto)
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
            // Generate email confirmation token and send email
            await SendEmailConfirmationAsync(user);
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new Exception("Kullanıcı bulunamadı");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                throw new Exception("Şifre yanlış");
            }

            if (!user.EmailConfirmed)
            {
                throw new Exception("E-posta doğrulanmadı. Lütfen e-posta adresinizi doğrulayın.");
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

        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            // token comes as base64url encoded
            try
            {
                var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var result = await _userManager.ConfirmEmailAsync(user, decoded);
                return result.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        public async Task ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // do not reveal
                return;
            }
            if (user.EmailConfirmed) return;
            await SendEmailConfirmationAsync(user);
        }

        private async Task SendEmailConfirmationAsync(User user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var baseUrl = _config["AppSettings:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = _config["Jwt:Issuer"]?.TrimEnd('/');
            }
            var confirmUrl = $"{baseUrl}/api/auth/confirm-email?userId={user.Id}&token={encoded}";

            var body = $@"<p>Merhaba {System.Net.WebUtility.HtmlEncode(user.FirstName ?? user.Email)},</p>
                           <p>Hesabınızı doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
                           <p><a href='{confirmUrl}'>E-posta adresimi doğrula</a></p>
                           <p>Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.</p>";

            await _emailSender.SendEmailAsync(user.Email!, "E-posta Doğrulama", body, isHtml: true);
        }
    }
}
