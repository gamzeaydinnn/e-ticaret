using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Discount


namespace ECommerce.Business.Services.Interfaces
{
    public interface IDiscountService
    {
        Task<IEnumerable<Discount>> GetAllAsync();
        Task<Discount?> GetByIdAsync(int id);
        Task AddAsync(Discount discount);
        Task UpdateAsync(Discount discount);
        Task DeleteAsync(int id);

        // 👇 Eklenen metotlar:
        Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
        Task<IEnumerable<Discount>> GetByProductIdAsync(int productId);
    }
}
