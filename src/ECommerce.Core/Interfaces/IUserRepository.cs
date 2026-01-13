using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Kullanıcı repository interface'i.
    /// Tüm CRUD operasyonları SaveChanges içerir.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<bool> UserExistsAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task AddAsync(User user);
        
        // Async versiyonlar (önerilen)
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
        
        // Senkron versiyonlar (geriye dönük uyumluluk)
        void Update(User user);
        void Delete(User user);
        
        Task<bool> ExistsAsync(string email);
    }
}