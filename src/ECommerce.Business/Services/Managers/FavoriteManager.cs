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

        public async Task<List<ProductListDto>> GetFavoritesAsync(int userId)
        {
            var favorites = await _favoriteRepository.GetFavoritesByUserAsync(userId);
            return favorites.Where(f => f.IsActive)
                .Select(f => new ProductListDto
                {
                    Id = f.Product.Id,
                    Name = f.Product.Name,
                    Price = f.Product.Price,
                    SpecialPrice = f.Product.SpecialPrice,
                    ImageUrl = f.Product.ImageUrl,
                    CategoryName = f.Product.Category?.Name,
                    StockQuantity = f.Product.StockQuantity,
                    Brand = f.Product.Brand?.Name
                }).ToList();
        }

        public async Task ToggleFavoriteAsync(int userId, int productId)
        {
            var favorite = await _favoriteRepository.GetFavoriteAsync(userId, productId);
            if (favorite == null)
            {
                await _favoriteRepository.AddAsync(new Favorite { UserId = userId, ProductId = productId, IsActive = true });
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
    }
}
