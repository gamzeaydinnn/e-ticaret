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

        // ================================================================
        // KAYITLI KULLANICI METODLARI
        // ================================================================
        
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
        
        // ================================================================
        // MİSAFİR KULLANICI METODLARI
        // ================================================================
        
        public async Task<IEnumerable<Favorite>> GetFavoritesByGuestTokenAsync(string guestToken)
        {
            return await _dbSet
                .Include(f => f.Product)
                    .ThenInclude(p => p!.Category)
                .Include(f => f.Product)
                    .ThenInclude(p => p!.Brand)
                .Where(f => f.GuestToken == guestToken && f.UserId == null && f.IsActive)
                .ToListAsync();
        }

        public async Task<Favorite?> GetFavoriteByTokenAsync(string guestToken, int productId)
        {
            return await _dbSet.FirstOrDefaultAsync(
                f => f.GuestToken == guestToken && f.ProductId == productId && f.UserId == null);
        }

        public async Task RemoveFavoriteByTokenAsync(string guestToken, int productId)
        {
            var favorite = await GetFavoriteByTokenAsync(guestToken, productId);
            if (favorite != null)
            {
                Delete(favorite);
            }
        }

        public async Task<int> GetFavoriteCountByTokenAsync(string guestToken)
        {
            return await _dbSet.CountAsync(
                f => f.GuestToken == guestToken && f.UserId == null && f.IsActive);
        }

        public async Task ClearFavoritesByTokenAsync(string guestToken)
        {
            var favorites = await _dbSet
                .Where(f => f.GuestToken == guestToken && f.UserId == null)
                .ToListAsync();

            foreach (var fav in favorites)
            {
                Delete(fav);
            }
        }

        public async Task MergeGuestFavoritesToUserAsync(string guestToken, int userId)
        {
            // Misafir favorilerini getir
            var guestFavorites = await _dbSet
                .Where(f => f.GuestToken == guestToken && f.UserId == null && f.IsActive)
                .ToListAsync();

            // Kullanıcının mevcut favorilerini kontrol et
            var userFavoriteProductIds = await _dbSet
                .Where(f => f.UserId == userId && f.IsActive)
                .Select(f => f.ProductId)
                .ToListAsync();

            foreach (var fav in guestFavorites)
            {
                // Kullanıcıda yoksa aktar
                if (!userFavoriteProductIds.Contains(fav.ProductId))
                {
                    var newFavorite = new Favorite
                    {
                        UserId = userId,
                        ProductId = fav.ProductId,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    await AddAsync(newFavorite);
                }

                // Misafir kaydını sil
                Delete(fav);
            }
        }
    }
}
