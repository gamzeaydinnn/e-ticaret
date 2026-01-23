using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // Favorite
using ECommerce.Core.DTOs.Product;   // ProductListDto


namespace ECommerce.Business.Services.Interfaces
{
    public interface IFavoriteService
    {
        // Kayıtlı kullanıcı metodları
        Task<List<ProductListDto>> GetFavoritesAsync(int userId);
        Task<int> GetFavoriteCountAsync(int userId); 
        Task ToggleFavoriteAsync(int userId, int productId);
        Task RemoveFavoriteAsync(int userId, int productId);
        
        // Misafir kullanıcı metodları
        Task<List<ProductListDto>> GetGuestFavoritesAsync(string guestToken);
        Task<int> GetGuestFavoriteCountAsync(string guestToken);
        Task<string> ToggleGuestFavoriteAsync(string guestToken, int productId);
        Task RemoveGuestFavoriteAsync(string guestToken, int productId);
        Task<int> MergeGuestFavoritesToUserAsync(string guestToken, int userId);
    }
}
