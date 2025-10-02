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

        public async Task<IEnumerable<Favorite>> GetFavoritesByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(f => f.Product)
                .Where(f => f.UserId == userId && f.IsActive)
                .ToListAsync();
        }

        public async Task<Favorite> GetFavoriteAsync(Guid userId, int productId)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId && f.IsActive);
        }

        public async Task RemoveFavoriteAsync(Guid userId, int productId)
        {
            var favorite = await GetFavoriteAsync(userId, productId);
            if (favorite != null)
            {
                Delete(favorite);
            }
        }
    }
}
