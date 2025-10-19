using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class FavoriteRepository : BaseRepository<Favorite>, IFavoriteRepository
    {
        public FavoriteRepository(ECommerceDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Favorite>> GetFavoritesByUserAsync(int userId)
        {
            return await _dbSet
                .Include(f => f.Product)
                    .ThenInclude(p => p!.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p!.Brand)
                .Where(f => f.UserId == userId && f.IsActive)
                .ToListAsync();
        }

        public async Task<Favorite?> GetFavoriteAsync(int userId, int productId)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
        }

        public async Task RemoveFavoriteAsync(int userId, int productId)
        {
            var favorite = await GetFavoriteAsync(userId, productId);
            if (favorite != null)
            {
                Delete(favorite);
            }
        }

        public async Task<int> GetFavoriteCountAsync(int userId)
        {
            return await _dbSet.CountAsync(f => f.UserId == userId && f.IsActive);
        }
    }
}
