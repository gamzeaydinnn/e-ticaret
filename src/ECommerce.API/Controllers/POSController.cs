using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.DTOs.Inventory;
using ECommerce.Core.Interfaces;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin,Admin,Manager")] // Market personeli için ayrı rol eklenebilir
    public class POSController : ControllerBase
    {
        private readonly IProductRepository _products;
        private readonly IInventoryService _inventory;

        public POSController(IProductRepository products, IInventoryService inventory)
        {
            _products = products;
            _inventory = inventory;
        }

        [HttpPost("sale")]
        public async Task<IActionResult> Sale([FromBody] POSSaleDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Sku) || dto.Quantity <= 0)
                return BadRequest("Geçersiz parametre");

            var product = await _products.GetBySkuAsync(dto.Sku);
            if (product == null) return NotFound("Ürün bulunamadı");

            var ok = await _inventory.DecreaseStockAsync(
                product.Id,
                dto.Quantity,
                Entities.Concrete.InventoryChangeType.Sale,
                note: dto.Note ?? "POS Satış",
                performedByUserId: dto.PerformedByUserId
            );

            if (!ok) return BadRequest("Yetersiz stok");
            return Ok(new { success = true, productId = product.Id, remaining = product.StockQuantity });
        }

        [HttpPost("adjust")]
        public async Task<IActionResult> Adjust([FromBody] StockAdjustDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Sku) || dto.Quantity == 0)
                return BadRequest("Geçersiz parametre");

            var product = await _products.GetBySkuAsync(dto.Sku);
            if (product == null) return NotFound("Ürün bulunamadı");

            var reason = dto.Reason?.ToLowerInvariant();
            var type = Entities.Concrete.InventoryChangeType.Correction;
            if (reason == "purchase") type = Entities.Concrete.InventoryChangeType.Purchase;
            if (reason == "return") type = Entities.Concrete.InventoryChangeType.Return;

            bool ok;
            if (dto.Quantity > 0)
            {
                ok = await _inventory.IncreaseStockAsync(product.Id, dto.Quantity, type, dto.Note, dto.PerformedByUserId);
            }
            else
            {
                ok = await _inventory.DecreaseStockAsync(product.Id, Math.Abs(dto.Quantity), type, dto.Note, dto.PerformedByUserId);
            }

            if (!ok) return BadRequest("İşlem gerçekleştirilemedi");
            return Ok(new { success = true, productId = product.Id, remaining = product.StockQuantity });
        }
    }
}

