using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IDiscountService
    {
        Task<IEnumerable<Discount>> GetAllAsync();
        Task<Discount?> GetByIdAsync(int id);
        Task AddAsync(Discount discount);
        Task UpdateAsync(Discount discount);
        Task DeleteAsync(int id);
    }
}
