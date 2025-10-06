using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Concrete
{
    public class ReviewManager : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewManager(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task<IEnumerable<Review>> GetAllAsync() => await _reviewRepository.GetAllAsync();
        public async Task<Review?> GetByIdAsync(int id) => await _reviewRepository.GetByIdAsync(id);
        public async Task AddAsync(Review review) => await _reviewRepository.AddAsync(review);
        public async Task UpdateAsync(Review review) => await _reviewRepository.UpdateAsync(review);
        public async Task DeleteAsync(int id)
        {
            var existing = await _reviewRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _reviewRepository.DeleteAsync(existing);
        }

        public async Task<double> GetAverageRatingAsync(int productId) => await _reviewRepository.GetAverageRatingAsync(productId);
        public async Task<IEnumerable<Review>> GetByProductIdAsync(int productId) => await _reviewRepository.GetByProductIdAsync(productId);
    }
}
