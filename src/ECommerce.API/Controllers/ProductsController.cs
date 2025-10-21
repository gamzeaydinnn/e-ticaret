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

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // Arama sorgusu boşsa, mevcut tüm ürünleri (veya kategorili ürünleri) dönebilirsiniz.
                // Basitçe boş bir liste dönmek de bir seçenek. Burada tüm aktif ürünleri döneceğiz.
                var allProducts = await _productService.GetActiveProductsAsync(page, size);
                return Ok(allProducts);
            }

            var products = await _productService.SearchProductsAsync(query, page, size);
            return Ok(products);
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

        // Advanced filtering endpoint: supports query, categories, brands, price range, rating, stock and sorting
        [HttpGet("filter")]
        public async Task<IActionResult> Filter(
            [FromQuery] string? query,
            [FromQuery] int[]? categoryIds,
            [FromQuery] int[]? brandIds,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] bool? inStockOnly,
            [FromQuery] int? minRating,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDir,
            [FromQuery] int page = 1,
            [FromQuery] int size = 12)
        {
            var filter = new ECommerce.Core.DTOs.Product.ProductFilterDto
            {
                Query = query,
                CategoryIds = categoryIds?.ToList(),
                BrandIds = brandIds?.ToList(),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                InStockOnly = inStockOnly,
                MinRating = minRating,
                SortBy = sortBy,
                SortDir = sortDir,
                Page = page,
                Size = size
            };

            var products = await _productService.FilterProductsAsync(filter);
            return Ok(products);
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
