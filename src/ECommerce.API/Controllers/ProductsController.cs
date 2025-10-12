using ECommerce.Business.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Extensions;
using ECommerce.Core.DTOs.ProductReview; // <-- DTO namespace
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 10, [FromQuery] int? categoryId = null)
        {
            var products = await _productService.GetActiveProductsAsync(page, size, categoryId);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost("{id}/review")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] ProductReviewCreateDto reviewDto)
        {
            if (reviewDto == null) return BadRequest("Review body is required.");

            // Route'taki product id'yi DTO'ya ata (opsiyonel ama tutarlı olur)
            reviewDto.ProductId = id;

            // user id extension metodunu kullan (User.GetUserId())
            var userId = User.GetUserId();

            await _productService.AddProductReviewAsync(id, userId, reviewDto);
            return Ok();
        }

        [HttpPost("{id}/favorite")]
        [Authorize]
        public async Task<IActionResult> AddToFavorite(int id)
        {
            await _productService.AddFavoriteAsync(User.GetUserId(), id);
            return Ok();
        }
    }
}
