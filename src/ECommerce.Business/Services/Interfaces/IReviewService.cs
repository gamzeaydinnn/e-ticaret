using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;  // ProductReview
using ECommerce.Core.DTOs.ProductReview;   // Review DTO


namespace ECommerce.Business.Services.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<ProductReview>> GetAllAsync();
        Task<ProductReview?> GetByIdAsync(int id);
        Task<ProductReview> AddAsync(ProductReview reviewDto, int userId);
        Task UpdateAsync(int id, ProductReview reviewDto);
        Task DeleteAsync(int id);
        Task<double> GetAverageRatingAsync(int productId);
        Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId);
    }
}
