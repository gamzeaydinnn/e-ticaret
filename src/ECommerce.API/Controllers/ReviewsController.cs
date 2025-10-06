using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        public ReviewsController(IReviewService reviewService) => _reviewService = reviewService;

        [HttpGet]
        public async Task<IActionResult> GetForProduct(int productId)
        {
            var reviews = await _reviewService.GetByProductIdAsync(productId);
            return Ok(reviews);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(int productId, [FromBody] ECommerce.Core.DTOs.Review.ReviewCreateDto reviewDto)
        {
            var sub = User.FindFirst("sub")?.Value;
            if (!int.TryParse(sub, out var uid)) return Unauthorized();

            var review = new Review
            {
                ProductId = productId,
                UserId = uid,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                IsApproved = false
            };

            await _reviewService.AddAsync(review);
            return CreatedAtAction(nameof(GetForProduct), new { productId }, review);
        }
    }
}
