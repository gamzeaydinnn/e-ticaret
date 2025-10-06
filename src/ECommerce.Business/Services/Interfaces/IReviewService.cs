using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetAllAsync();
        Task<Review?> GetByIdAsync(int id);
        Task AddAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(int id);
        Task<double> GetAverageRatingAsync(int productId);
        Task<IEnumerable<Review>> GetByProductIdAsync(int productId);
    }
}
