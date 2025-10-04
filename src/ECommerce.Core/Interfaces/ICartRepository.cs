using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    public interface ICartRepository : IRepository<CartItem>
    {
        Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId);
        Task<CartItem?> GetByUserAndProductAsync(int userId, int productId);
        Task RemoveByUserAndProductAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
        Task RemoveCartItemAsync(int cartItemId);
        Task<int> GetCartCountAsync();


    }
}