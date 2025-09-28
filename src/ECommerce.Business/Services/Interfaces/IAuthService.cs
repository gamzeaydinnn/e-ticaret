using System.Threading.Tasks;
using ECommerce.Core.DTOs.Auth;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
    }
}
