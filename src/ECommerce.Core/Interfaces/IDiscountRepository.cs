using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface IDiscountRepository : IRepository<Discount>
    {
        Task<IEnumerable<Discount>> GetActiveDiscountsAsync();
    }
}
