using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    public class ReviewRepository : BaseRepository<ProductReview>, IReviewRepository
    {
        public ReviewRepository(ECommerceDbContext context) : base(context) { }

        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            return await _dbSet
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingAsync(int productId)
        {
            var reviews = await _dbSet.Where(r => r.ProductId == productId).ToListAsync();
            return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
        }

        Task<IEnumerable<Review>> IReviewRepository.GetByProductIdAsync(int productId)
        {
            throw new NotImplementedException();
        }

        Task<Review?> IRepository<Review>.GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        Task<IEnumerable<Review>> IRepository<Review>.GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(Review entity)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(Review entity)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(Review entity)
        {
            throw new NotImplementedException();
        }

        public Task HardDeleteAsync(Review entity)
        {
            throw new NotImplementedException();
        }
    }
}
