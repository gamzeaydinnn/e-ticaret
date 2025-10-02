using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Repositories;
using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class FavoriteManager : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IProductRepository _productRepository;

        public FavoriteManager(IFavoriteRepository favoriteRepository, IProductRepository productRepository)
        {
            _favoriteRepository = favoriteRepository;
            _productRepository = productRepository;
        }

        public async Task<List<ProductListDto>> GetFavoritesAsync(Guid userId)
        {
            var favorites = await _favoriteRepository.GetFavoritesByUserAsync(userId);
            return favorites.Select(f => new ProductListDto
            {
                Id = f.Product.Id,
                Name = f.Product.Name,
                Price = f.Product.Price,
                ImageUrl = f.Product.ImageUrl
            }).ToList();
        }

        public async Task ToggleFavoriteAsync(Guid userId, int productId)
        {
            var favorite = await _favoriteRepository.GetFavoriteAsync(userId, productId);
            if (favorite == null)
            {
                await _favoriteRepository.AddAsync(new Favorite { UserId = userId, ProductId = productId, IsActive = true });
            }
            else
            {
                favorite.IsActive = !favorite.IsActive;
                _favoriteRepository.Update(favorite);
            }
        }

        public async Task RemoveFavoriteAsync(Guid userId, int productId)
        {
            await _favoriteRepository.RemoveFavoriteAsync(userId, productId);
        }
    }
}
