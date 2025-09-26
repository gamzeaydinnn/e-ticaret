using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/products")]  // products controller için doğru route
    [Authorize(Roles = "Admin")]   // sadece admin erişebilir
    public class AdminProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public AdminProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetProductsAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
        {
            var product = await _productService.CreateAsync(dto);
            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
    }
}
