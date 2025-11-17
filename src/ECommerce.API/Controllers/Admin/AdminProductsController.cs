using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Infrastructure.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using System.Security.Claims;
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
    private readonly IAuditLogService _auditLogService;

    public AdminProductsController(IProductService productService, StockSyncJob stockSyncJob, IAuditLogService auditLogService)
    {
        _productService = productService;
        _stockSyncJob = stockSyncJob;
        _auditLogService = auditLogService;
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
        var oldProduct = await _productService.GetByIdAsync(id);

        await _productService.UpdateProductAsync(id, productDto);
        if (oldProduct != null)
        {
            var updatedProduct = await _productService.GetByIdAsync(id);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "ProductUpdated",
                "Product",
                id.ToString(),
                new
                {
                    oldProduct.Name,
                    oldProduct.Price,
                    oldProduct.SpecialPrice,
                    oldProduct.StockQuantity,
                    oldProduct.CategoryName
                },
                updatedProduct != null
                    ? new
                    {
                        updatedProduct.Name,
                        updatedProduct.Price,
                        updatedProduct.SpecialPrice,
                        updatedProduct.StockQuantity,
                        updatedProduct.CategoryName
                    }
                    : null);
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var existingProduct = await _productService.GetByIdAsync(id);

        await _productService.DeleteProductAsync(id);
        if (existingProduct != null)
        {
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "ProductDeleted",
                "Product",
                id.ToString(),
                new
                {
                    existingProduct.Name,
                    existingProduct.Price,
                    existingProduct.SpecialPrice,
                    existingProduct.StockQuantity,
                    existingProduct.CategoryName
                },
                null);
        }
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

    private int GetAdminUserId()
    {
        var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
    }
}
