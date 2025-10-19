using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    // Sadece "Admin" rolüne sahip kullanıcıların erişimine izin verir
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    // Rota: api/admin/micro
    [Route("api/admin/micro")] 
    public class AdminMicroController : ControllerBase
    {
        private readonly MicroSyncManager _microSyncManager;
        private readonly IMicroService _microService; // MicroService'i de burada kullanmak mantıklı olabilir.

        public AdminMicroController(MicroSyncManager microSyncManager, IMicroService microService)
        {
            _microSyncManager = microSyncManager;
            _microService = microService;
        }

        //--- Yönetici Yetkisi Gerektiren İşlemler (Mutating/Triggering) ---
        
        /// <summary>
        /// Mikro ERP’ye tüm ürünleri senkronize et (Yönetici yetkisi gereklidir)
        /// </summary>
        [HttpPost("sync-products")]
        public IActionResult SyncProducts()
        {
            // Bu kritik işlem sadece AdminController altında olmalı
            _microSyncManager.SyncProductsToMikro(); 
            return Ok(new { message = "Ürünler Mikro ERP ile senkronize edildi." });
        }
        
        /// <summary>
        /// Siparişleri Mikro ERP’ye gönder (Yönetici yetkisi gereklidir)
        /// </summary>
        [HttpPost("export-orders")]
        public async Task<IActionResult> ExportOrders([FromBody] IEnumerable<Order> orders)
        {
            // Bu kritik işlem sadece AdminController altında olmalı
            var success = await _microService.ExportOrdersToERPAsync(orders); 
            
            if (!success)
                return BadRequest(new { message = "Siparişler ERP'ye aktarılamadı." });

            return Ok(new { message = "Siparişler ERP'ye aktarıldı." });
        }
        
        //--- Yönetici Sayfasında Görüntüleme Amaçlı Endpoint'ler (Opsiyonel) ---
        // Eğer bu verileri sadece adminler görüyorsa buraya taşınabilirler. 
        // Ancak herkesin görmesi gerekiyorsa MicroController'da kalmalıdırlar.
        
        /// <summary>
        /// Mikro ERP’den ürünleri getir (Admin sayfasında gösterim için)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _microService.GetProductsAsync();
            return Ok(products);
        }

        /// <summary>
        /// Mikro ERP’den stokları getir (Admin sayfasında gösterim için)
        /// </summary>
        [HttpGet("stocks")]
        public async Task<IActionResult> GetStocks()
        {
            var stocks = await _microService.GetStocksAsync();
            return Ok(stocks);
        }
    }
}
