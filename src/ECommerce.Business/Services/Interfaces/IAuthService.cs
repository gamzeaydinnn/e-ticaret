using System.Threading.Tasks;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);

        // JWT'den userId çekip kullanıcıyı döndürmek için
        Task<UserLoginDto> GetUserByIdAsync(int userId);

        // Refresh token ve revoke token işlemleri
        Task<string> RefreshTokenAsync(string token, string refreshToken);
        Task RevokeRefreshTokenAsync(int userId);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}
