using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/[controller]")] // api/admin/coupons
    public class AdminCouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public AdminCouponsController(ICouponService couponService)
        {
            _couponService = couponService;
        }
        
        // GET, GET(id), POST metodları doğru görünüyor, onları koruyoruz.
        
        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            var coupons = await _couponService.GetAllAsync();
            return Ok(coupons);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCouponById(int id)
        {
            var coupon = await _couponService.GetByIdAsync(id);
            if (coupon == null)
                return NotFound();
            return Ok(coupon);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            coupon.Id = 0;
            await _couponService.AddAsync(coupon);
            return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);
        }

        // 🛠️ PUT Metodu Güncellemesi
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon coupon)
        {
            if (id != coupon.Id)
                return BadRequest(new { message = "Rota ID'si ile kupon ID'si eşleşmiyor." });

            var existingCoupon = await _couponService.GetByIdAsync(id);
            if (existingCoupon == null)
                return NotFound();

            // Tüm güncellenebilir alanları kopyala
            existingCoupon.Code = coupon.Code;
            
            // Entity'deki ad: Value, Kontrolcüdeki ad: Value (DiscountAmount yerine)
            existingCoupon.Value = coupon.Value; 
            existingCoupon.IsPercentage = coupon.IsPercentage; 
            
            existingCoupon.ExpirationDate = coupon.ExpirationDate;
            
            // Eksik olan önemli alanlar eklendi
            existingCoupon.MinOrderAmount = coupon.MinOrderAmount;
            existingCoupon.UsageLimit = coupon.UsageLimit;
            
            existingCoupon.IsActive = coupon.IsActive; 
            // BaseEntity'den gelen güncellemeler de yapılabilir (örneğin UpdateDate)

            await _couponService.UpdateAsync(existingCoupon);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var existingCoupon = await _couponService.GetByIdAsync(id);
            if (existingCoupon == null)
                return NotFound();

            await _couponService.DeleteAsync(id);
            return NoContent();
        }
    }
}