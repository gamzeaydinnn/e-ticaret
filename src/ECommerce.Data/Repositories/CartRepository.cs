using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    public class CartRepository : BaseRepository<CartItem>, ICartRepository
    {
        public CartRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();
        }

        public async Task<CartItem?> GetByUserAndProductAsync(int userId, int productId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId && c.IsActive);
        }

        public async Task RemoveByUserAndProductAsync(int userId, int productId)
        {
            var cartItem = await GetByUserAndProductAsync(userId, productId);
            if (cartItem != null)
            {
                Delete(cartItem);
            }
        }

        public async Task ClearCartAsync(int userId)
        {
            var cartItems = await _dbSet
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                Delete(item);
            }
        }

        // ✅ Yeni eklenen metot (sadece id ile silme)
        public async Task RemoveCartItemAsync(int cartItemId)
        {
            var cartItem = await _dbSet.FindAsync(cartItemId);
            if (cartItem != null)
            {
                Delete(cartItem);
            }
        }
    }
}
