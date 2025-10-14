using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IFavoriteRepository : IRepository<Favorite>
    {
        Task<IEnumerable<Favorite>> GetFavoritesByUserAsync(int userId);
        Task<Favorite?> GetFavoriteAsync(int userId, int productId);
        Task RemoveFavoriteAsync(int userId, int productId);
        Task<int> GetFavoriteCountAsync(int userId);
    }
}
