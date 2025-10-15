using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface ICourierRepository : IRepository<Courier>
    {
        Task<Courier?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Courier>> GetActiveCouriersAsync();
        Task<IEnumerable<Courier>> GetByStatusAsync(string status);
        Task<Courier?> GetByEmailAsync(string email);
        Task<IEnumerable<Order>> GetCourierOrdersAsync(int courierId);
        Task UpdateLocationAsync(int courierId, string location);
        Task UpdateStatusAsync(int courierId, string status);
    }
}