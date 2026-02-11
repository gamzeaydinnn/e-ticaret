using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Constants;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using System.Security.Claims;
using ECommerce.API.Authorization;

namespace ECommerce.API.Controllers.Admin
{
    [Authorize(Roles = Roles.AllStaff)]
    [ApiController]
    [Route("api/admin/coupons")]
    public class AdminCouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly IAuditLogService _auditLogService;

        public AdminCouponsController(ICouponService couponService, IAuditLogService auditLogService)
        {
            _couponService = couponService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        [HasPermission(Permissions.Coupons.View)]
        public async Task<IActionResult> GetAllCoupons()
        {
            var coupons = await _couponService.GetAllAsync();
            return Ok(coupons);
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.Coupons.View)]
        public async Task<IActionResult> GetCouponById(int id)
        {
            var coupon = await _couponService.GetByIdAsync(id);
            if (coupon == null)
                return NotFound();
            return Ok(coupon);
        }

        [HttpPost]
        [HasPermission(Permissions.Coupons.Create)]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            coupon.Id = 0;
            await _couponService.AddAsync(coupon);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "CouponCreated",
                "Coupon",
                coupon.Id.ToString(),
                null,
                new
                {
                    coupon.Code,
                    coupon.Value,
                    coupon.IsPercentage,
                    coupon.MinOrderAmount,
                    coupon.UsageLimit,
                    coupon.ExpirationDate,
                    coupon.IsActive
                });
            return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.Coupons.Update)]
        public async Task<IActionResult> UpdateCoupon(int id, [FromBody] Coupon coupon)
        {
            if (id != coupon.Id)
                return BadRequest(new { message = "Rota ID'si ile kupon ID'si eşleşmiyor." });

            var existingCoupon = await _couponService.GetByIdAsync(id);
            if (existingCoupon == null)
                return NotFound();

            var oldSnapshot = new
            {
                existingCoupon.Code,
                existingCoupon.Value,
                existingCoupon.IsPercentage,
                existingCoupon.MinOrderAmount,
                existingCoupon.UsageLimit,
                existingCoupon.ExpirationDate,
                existingCoupon.IsActive
            };

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
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "CouponUpdated",
                "Coupon",
                id.ToString(),
                oldSnapshot,
                new
                {
                    existingCoupon.Code,
                    existingCoupon.Value,
                    existingCoupon.IsPercentage,
                    existingCoupon.MinOrderAmount,
                    existingCoupon.UsageLimit,
                    existingCoupon.ExpirationDate,
                    existingCoupon.IsActive
                });
            return NoContent();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Coupons.Delete)]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var existingCoupon = await _couponService.GetByIdAsync(id);
            if (existingCoupon == null)
                return NotFound();

            await _couponService.DeleteAsync(id);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "CouponDeleted",
                "Coupon",
                id.ToString(),
                new
                {
                    existingCoupon.Code,
                    existingCoupon.Value,
                    existingCoupon.IsPercentage,
                    existingCoupon.MinOrderAmount,
                    existingCoupon.UsageLimit,
                    existingCoupon.ExpirationDate,
                    existingCoupon.IsActive
                },
                null);
            return NoContent();
        }

        private int GetAdminUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
        }
    }
}
