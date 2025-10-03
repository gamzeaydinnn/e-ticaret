using ECommerce.Core.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<List<ProductListDto>> GetFavoritesAsync(Guid userId);
        Task<int> GetFavoriteCountAsync(); 
        Task ToggleFavoriteAsync(Guid userId, int productId);
        Task RemoveFavoriteAsync(Guid userId, int productId);
    }
}
