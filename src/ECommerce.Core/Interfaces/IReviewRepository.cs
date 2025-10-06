using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    public interface IReviewRepository : IRepository<Review>
    {
        Task<IEnumerable<Review>> GetByProductIdAsync(int productId);
        Task<double> GetAverageRatingAsync(int productId);
    }
}
