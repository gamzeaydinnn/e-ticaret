//MikroController (senkron endpoint'leri).
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

// ... using'ler
namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MicroController : ControllerBase
    {
        // Sadece okuma/public işlemler için gerekiyorsa tutulur
        private readonly IMicroService _microService; 

        public MicroController(IMicroService microService)
        {
            _microService = microService;
        }

        /* Kaldırıldı: SyncProducts() 
        Kaldırıldı: ExportOrders() 
        */

        /// <summary>
        /// Mikro ERP’den ürünleri getir (Gerekirse burada kalır)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _microService.GetProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP’den stokları getir (Gerekirse burada kalır)
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks()
        {
            var stocks = await _microService.GetStocksAsync();
            return Ok(stocks);
        }
    }
}