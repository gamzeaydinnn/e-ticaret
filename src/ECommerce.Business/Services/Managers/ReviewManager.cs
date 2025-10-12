using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IReviewService
using ECommerce.Entities.Concrete;            // ProductReview
using ECommerce.Core.Interfaces;              // IReviewRepository
using ECommerce.Core.DTOs.ProductReview;             // Review DTO

<<<<<<< HEAD

=======
>>>>>>> origin/main
namespace ECommerce.Business.Services.Managers
{
    public class ReviewManager : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewManager(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        private static ProductReview MapToDto(ProductReview r) =>
            new ProductReview
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                CreatedAt = r.CreatedAt
            };

        public async Task<IEnumerable<ProductReview>> GetAllAsync()
        {
            var entities = await _reviewRepository.GetAllAsync();
            return entities.Select(MapToDto);
        }

        public async Task<ProductReview?> GetByIdAsync(int id)
        {
            var e = await _reviewRepository.GetByIdAsync(id);
            return e == null ? null : MapToDto(e);
        }

        public async Task<ProductReview> AddAsync(ProductReview reviewDto, int userId)
        {
            var entity = new ProductReview
            {
                ProductId = reviewDto.ProductId,
                UserId = userId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(entity);
            return MapToDto(entity);
        }

        public async Task UpdateAsync(int id, ProductReview reviewDto)
        {
            var existing = await _reviewRepository.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Review with id {id} not found.");

            existing.Rating = reviewDto.Rating;
            existing.Comment = reviewDto.Comment;
            await _reviewRepository.UpdateAsync(existing);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _reviewRepository.GetByIdAsync(id);
            if (existing == null) return;
            await _reviewRepository.DeleteAsync(existing);
        }

        public Task<double> GetAverageRatingAsync(int productId) =>
            _reviewRepository.GetAverageRatingAsync(productId);

        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            var entities = await _reviewRepository.GetByProductIdAsync(productId);
            return entities.Select(MapToDto);
        }
    }
}
