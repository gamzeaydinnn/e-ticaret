using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.ProductReview;
using ECommerce.Entities.Concrete;

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

        [HttpGet("average")]
        public async Task<IActionResult> GetAverage(int productId)
        {
            var average = await _reviewService.GetAverageRatingAsync(productId);
            return Ok(new { productId, average });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(int productId, [FromBody] ProductReview reviewDto)
        {
            var sub = User.FindFirst("sub")?.Value;
            if (!int.TryParse(sub, out var uid)) return Unauthorized();

            reviewDto.ProductId = productId;
            try
            {
                var created = await _reviewService.AddAsync(reviewDto, uid);
                return CreatedAtAction(nameof(GetForProduct), new { productId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
