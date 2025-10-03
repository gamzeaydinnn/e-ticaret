//MikroController (senkron endpoint'leri).
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MicroController : ControllerBase
    {
        private readonly MicroSyncManager _microSyncManager;

        public MicroController(MicroSyncManager microSyncManager)
        {
            _microSyncManager = microSyncManager;
        }

        /// <summary>
        /// Mikro ERP’ye tüm ürünleri senkronize et
        /// </summary>
        [HttpPost("sync-products")]
        public IActionResult SyncProducts()
        {
            _microSyncManager.SyncProductsToMikro();
            return Ok(new { message = "Ürünler Mikro ERP ile senkronize edildi." });
        }

        /// <summary>
        /// Mikro ERP’den ürünleri getir
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromServices] IMicroService microService)
        {
            var products = await microService.GetProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP’den stokları getir
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks([FromServices] IMicroService microService)
        {
            var stocks = await microService.GetStocksAsync();
            return Ok(stocks);
        }

        /// <summary>
        /// Siparişleri Mikro ERP’ye gönder
        /// </summary>
        [HttpPost("export-orders")]
        public async Task<IActionResult> ExportOrders([FromBody] IEnumerable<ECommerce.Entities.Concrete.Order> orders, [FromServices] IMicroService microService)
        {
            var success = await microService.ExportOrdersToERPAsync(orders);
            if (!success)
                return BadRequest(new { message = "Siparişler ERP'ye aktarılamadı." });

            return Ok(new { message = "Siparişler ERP'ye aktarıldı." });
        }
    }
}
