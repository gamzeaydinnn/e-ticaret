using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    // DEĞİŞİKLİK: IRepository<Review> yerine IRepository<ProductReview> kullanıldı.
    public interface IReviewRepository : IRepository<ProductReview>
    {
        Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId);
        Task<IEnumerable<ProductReview>> GetPendingReviewsAsync();
        Task<double> GetAverageRatingAsync(int productId);
        Task<bool> HasUserReviewAsync(int productId, int userId);
    }
}
