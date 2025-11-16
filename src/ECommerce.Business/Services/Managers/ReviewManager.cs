using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces; // IReviewService
using ECommerce.Entities.Concrete;            // ProductReview
using ECommerce.Core.Interfaces;              // IReviewRepository
using ECommerce.Core.DTOs.ProductReview;             // Review DTO


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
            if (await _reviewRepository.HasUserReviewAsync(reviewDto.ProductId, userId))
            {
                throw new InvalidOperationException("Bu ürün için zaten bir yorumunuz bulunuyor.");
            }

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

        public Task<IEnumerable<ProductReview>> GetApprovedReviewsByProductAsync(int productId) =>
            GetByProductIdAsync(productId);

        public async Task<IEnumerable<ProductReview>> GetByProductIdAsync(int productId)
        {
            var entities = await _reviewRepository.GetByProductIdAsync(productId);
            return entities.Select(MapToDto);
        }

        public async Task<IEnumerable<ProductReview>> GetPendingReviewsAsync()
        {
            var entities = await _reviewRepository.GetPendingReviewsAsync();
            return entities.Select(MapToDto);
        }

        public async Task ApproveReviewAsync(int id)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null) throw new KeyNotFoundException("Review bulunamadı.");

            review.IsApproved = true;
            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateAsync(review);
        }

        public async Task RejectReviewAsync(int id)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null) throw new KeyNotFoundException("Review bulunamadı.");

            review.IsApproved = false;
            review.IsActive = false;
            review.UpdatedAt = DateTime.UtcNow;
            await _reviewRepository.UpdateAsync(review);
        }
    }
}
