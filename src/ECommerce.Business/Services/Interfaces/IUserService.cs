using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<bool> UserExistsAsync(string email);
        Task AddAsync(User user);
        void Update(User user);
        void Delete(User user);
        Task<int> GetUserCountAsync();
    }
}
