using System.Threading.Tasks;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<(string accessToken, string refreshToken)> LoginAsync(LoginDto dto);

        // JWT'den userId çekip kullanıcıyı döndürmek için
        Task<UserLoginDto> GetUserByIdAsync(int userId);
        Task<UserLoginDto> GetUserByEmailAsync(string email);

        // Refresh token ve revoke token işlemleri
        // Returns a new access token and a new refresh token (rotation)
        Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string token, string refreshToken);
        Task InvalidateUserTokensAsync(int userId);
        Task<(string accessToken, string refreshToken)> IssueTokensForUserAsync(User user, string? ipAddress = null);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);

        // Email doğrulama
        Task<bool> ConfirmEmailAsync(int userId, string token);
        Task<bool> ResendConfirmationEmailAsync(string email);
    }
}
