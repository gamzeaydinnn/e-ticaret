using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using System.Threading.Tasks;



namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // api/discounts
    public class DiscountsController : ControllerBase
    {
        private readonly IDiscountService _discountService;

        public DiscountsController(IDiscountService discountService)
        {
            _discountService = discountService;
        }

        /// <summary>
        /// Etkin ve geçerli tüm indirimleri listeler (ör. Migros indirimleri gibi).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllActive()
        {
            var discounts = await _discountService.GetActiveDiscountsAsync(); 
            return Ok(discounts);
        }

        // Örnek: Belirli bir ürüne ait aktif indirimleri getir
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetDiscountsByProduct(int productId)
        {
            var discounts = await _discountService.GetByProductIdAsync(productId);
            return Ok(discounts);
        }
    }
}
