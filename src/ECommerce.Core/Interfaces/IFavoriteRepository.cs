using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IFavoriteRepository : IRepository<Favorite>
    {
        // Kayıtlı kullanıcı metodları
        Task<IEnumerable<Favorite>> GetFavoritesByUserAsync(int userId);
        Task<Favorite?> GetFavoriteAsync(int userId, int productId);
        Task RemoveFavoriteAsync(int userId, int productId);
        Task<int> GetFavoriteCountAsync(int userId);
        
        // Misafir kullanıcı metodları
        Task<IEnumerable<Favorite>> GetFavoritesByGuestTokenAsync(string guestToken);
        Task<Favorite?> GetFavoriteByTokenAsync(string guestToken, int productId);
        Task RemoveFavoriteByTokenAsync(string guestToken, int productId);
        Task<int> GetFavoriteCountByTokenAsync(string guestToken);
        Task ClearFavoritesByTokenAsync(string guestToken);
        Task MergeGuestFavoritesToUserAsync(string guestToken, int userId);
    }
}
