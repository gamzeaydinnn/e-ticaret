using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Favorite
using ECommerce.Core.DTOs.Product;   // ProductListDto


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
