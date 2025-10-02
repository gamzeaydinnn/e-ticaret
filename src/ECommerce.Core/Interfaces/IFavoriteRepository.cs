using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IFavoriteRepository : IRepository<Favorite>
    {
        Task<IEnumerable<Favorite>> GetFavoritesByUserAsync(Guid userId);
        Task<Favorite> GetFavoriteAsync(Guid userId, int productId);
        Task RemoveFavoriteAsync(Guid userId, int productId);
    }
}
