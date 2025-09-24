using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<Order?> GetByOrderNumberAsync(string orderNumber);
    }
}