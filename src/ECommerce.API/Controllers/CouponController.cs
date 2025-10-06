using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/coupons")]
    [Authorize(Roles = "Admin")]
    public class AdminCouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;
        public AdminCouponsController(ICouponService couponService) => _couponService = couponService;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _couponService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var c = await _couponService.GetByIdAsync(id);
            if (c == null) return NotFound();
            return Ok(c);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Coupon coupon)
        {
            await _couponService.AddAsync(coupon);
            return CreatedAtAction(nameof(Get), new { id = coupon.Id }, coupon);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Coupon coupon)
        {
            var existing = await _couponService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Code = coupon.Code;
            existing.IsPercentage = coupon.IsPercentage;
            existing.Value = coupon.Value;
            existing.ExpirationDate = coupon.ExpirationDate;
            existing.MinOrderAmount = coupon.MinOrderAmount;
            existing.UsageLimit = coupon.UsageLimit;
            existing.IsActive = coupon.IsActive;

            await _couponService.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _couponService.DeleteAsync(id);
            return NoContent();
        }
    }
}
