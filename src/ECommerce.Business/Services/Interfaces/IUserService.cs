using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // User
using ECommerce.Core.DTOs.User;
using ECommerce.Core.DTOs.Auth;     // User DTO


namespace ECommerce.Business.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> UserExistsAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);

        void Update(User user);
        void Delete(User user);
        Task<int> GetUserCountAsync();

        // Yeni eklenen Auth işlemleri
        Task<bool> ChangePasswordAsync(ChangePasswordDto dto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    }
}
