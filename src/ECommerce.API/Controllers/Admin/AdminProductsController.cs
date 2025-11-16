using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Infrastructure.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
/*ProductsController
•	GET /api/products -> ürün listesi (kategori, pagination destekleyin)
•	GET /api/products/{id}
•	Admin: POST /api/products, PUT /api/products/{id}, DELETE /api/products/{id}
*/
namespace ECommerce.API.Controllers.Admin;

[ApiController]
[Route("api/admin/products")]
    [Authorize(Roles = Roles.AdminLike)]

public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly StockSyncJob _stockSyncJob;

    public AdminProductsController(IProductService productService, StockSyncJob stockSyncJob)
    {
        _productService = productService;
        _stockSyncJob = stockSyncJob;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var products = await _productService.GetAllProductsAsync(page, size);
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto productDto)
    {
        var result = await _productService.CreateProductAsync(productDto);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpdateDto productDto)
    {
        await _productService.UpdateProductAsync(id, productDto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        await _productService.DeleteProductAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        // Tercihen tekil get-by-id için belirli servis metodunu kullan
        var product = await _productService.GetByIdAsync(id) 
                      ?? await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int stock)
    {
        await _productService.UpdateStockAsync(id, stock);
        return NoContent();
    }

    [HttpPost("sync-stocks")]
    public async Task<IActionResult> SyncStocks()
    {
        await _stockSyncJob.RunOnce();
        return Ok("Stok senkronizasyonu başlatıldı.");
    }
}
