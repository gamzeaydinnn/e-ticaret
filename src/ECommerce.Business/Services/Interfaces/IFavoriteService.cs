using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Favorite
using ECommerce.Core.DTOs.Product;   // ProductListDto


namespace ECommerce.Business.Services.Interfaces
{
    public interface IFavoriteService
    {
        Task<List<ProductListDto>> GetFavoritesAsync(int userId);
        Task<int> GetFavoriteCountAsync(int userId); 
        Task ToggleFavoriteAsync(int userId, int productId);
        Task RemoveFavoriteAsync(int userId, int productId);
    }
}
