using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities;
using ECommerce.Core;
using ECommerce.Data;



namespace ECommerce.Business.Services.Interfaces
{
    public interface IAddressService
    {
        Task<IEnumerable<Address>> GetAllAsync();
        Task<Address?> GetByIdAsync(int id);
        Task AddAsync(Address address);
        Task UpdateAsync(Address address);
        Task DeleteAsync(int id);

        // ✅ Eksik olan metot eklendi
        Task<IEnumerable<Address>> GetByUserIdAsync(int userId);
    }
}
