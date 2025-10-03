using System.Threading.Tasks;
using ECommerce.Core.DTOs.Auth;
using ECommerce.Core.DTOs.User;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
        //Yeni ekleme: JWT içindeki userId'den user bilgilerini döndürmek için
        Task<UserLoginDto> GetUserByIdAsync(int userId);
        Task<string> RefreshTokenAsync(string token, string refreshToken);
        Task RevokeRefreshTokenAsync(int userId);

    }
}
