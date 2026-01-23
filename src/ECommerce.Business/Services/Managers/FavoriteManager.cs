using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IFavoriteService
using ECommerce.Entities.Concrete;            // Favorite, Product
using ECommerce.Core.DTOs.Product;            // ProductListDto
using ECommerce.Core.Interfaces;              // IFavoriteRepository


namespace ECommerce.Business.Services.Managers
{
    public class FavoriteManager : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;

        public FavoriteManager(IFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        // ================================================================
        // KAYITLI KULLANICI METODLARI
        // ================================================================

        public async Task<List<ProductListDto>> GetFavoritesAsync(int userId)
        {
            var favorites = await _favoriteRepository.GetFavoritesByUserAsync(userId);
            return favorites.Where(f => f.IsActive)
                .Select(MapFavoriteToDto)
                .ToList();
        }

        public async Task ToggleFavoriteAsync(int userId, int productId)
        {
            var favorite = await _favoriteRepository.GetFavoriteAsync(userId, productId);
            if (favorite == null)
            {
                await _favoriteRepository.AddAsync(new Favorite 
                { 
                    UserId = userId, 
                    ProductId = productId, 
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                favorite.IsActive = !favorite.IsActive;
                await _favoriteRepository.UpdateAsync(favorite); 
            }
        }

        public async Task RemoveFavoriteAsync(int userId, int productId)
        {
            await _favoriteRepository.RemoveFavoriteAsync(userId, productId);
        }

        public async Task<int> GetFavoriteCountAsync(int userId)
        {
            return await _favoriteRepository.GetFavoriteCountAsync(userId);
        }

        // ================================================================
        // MİSAFİR KULLANICI METODLARI
        // ================================================================

        public async Task<List<ProductListDto>> GetGuestFavoritesAsync(string guestToken)
        {
            if (string.IsNullOrEmpty(guestToken))
                return new List<ProductListDto>();

            var favorites = await _favoriteRepository.GetFavoritesByGuestTokenAsync(guestToken);
            return favorites.Where(f => f.IsActive)
                .Select(MapFavoriteToDto)
                .ToList();
        }

        public async Task<int> GetGuestFavoriteCountAsync(string guestToken)
        {
            if (string.IsNullOrEmpty(guestToken))
                return 0;

            return await _favoriteRepository.GetFavoriteCountByTokenAsync(guestToken);
        }

        public async Task<string> ToggleGuestFavoriteAsync(string guestToken, int productId)
        {
            if (string.IsNullOrEmpty(guestToken))
                throw new ArgumentException("Guest token gerekli");

            var favorite = await _favoriteRepository.GetFavoriteByTokenAsync(guestToken, productId);
            if (favorite == null)
            {
                // Yeni favori ekle
                await _favoriteRepository.AddAsync(new Favorite 
                { 
                    GuestToken = guestToken, 
                    ProductId = productId, 
                    UserId = null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                return "added";
            }
            else
            {
                // Toggle
                favorite.IsActive = !favorite.IsActive;
                await _favoriteRepository.UpdateAsync(favorite);
                return favorite.IsActive ? "added" : "removed";
            }
        }

        public async Task RemoveGuestFavoriteAsync(string guestToken, int productId)
        {
            if (string.IsNullOrEmpty(guestToken))
                return;

            await _favoriteRepository.RemoveFavoriteByTokenAsync(guestToken, productId);
        }

        public async Task<int> MergeGuestFavoritesToUserAsync(string guestToken, int userId)
        {
            if (string.IsNullOrEmpty(guestToken) || userId <= 0)
                return 0;

            // Misafir favorileri say
            var guestFavorites = await _favoriteRepository.GetFavoritesByGuestTokenAsync(guestToken);
            var count = guestFavorites.Count();

            // Merge işlemi repository'de yapılıyor
            await _favoriteRepository.MergeGuestFavoritesToUserAsync(guestToken, userId);

            return count;
        }

        // ================================================================
        // HELPER METOD
        // ================================================================

        private static ProductListDto MapFavoriteToDto(Favorite f)
        {
            return new ProductListDto
            {
                Id = f.Product.Id,
                Name = f.Product.Name,
                Price = f.Product.Price,
                SpecialPrice = f.Product.SpecialPrice,
                ImageUrl = f.Product.ImageUrl,
                CategoryName = f.Product.Category?.Name,
                StockQuantity = f.Product.StockQuantity,
                Brand = f.Product.Brand?.Name
            };
        }
    }
}
