using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    // DEĞİŞİKLİK: IRepository<Review> yerine IRepository<ProductReview> kullanıldı.
    public interface IReviewRepository : IRepository<ProductReview>
    {
        // DEĞİŞİKLİK: Dönüş tipi IEnumerable<Review> yerine IEnumerable<ProductReview> oldu.
        Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId);
        Task<double> GetAverageRatingAsync(int productId);
    }
}