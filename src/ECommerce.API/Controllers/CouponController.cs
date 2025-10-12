using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using System.Threading.Tasks;


namespace ECommerce.API.Controllers
{
    // Admin yetkisi yok, genel kullanıma açık
    [ApiController]
    [Route("api/[controller]")] // api/coupon
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;

        public CouponController(ICouponService couponService)
        {
            _couponService = couponService;
        }

        /// <summary>
        /// Kullanıcının sepette girdiği kupon kodunu doğrular ve uygular.
        /// </summary>
        [HttpPost("apply/{code}")]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            // CouponService içindeki ValidateCouponAsync(string code) metodu kullanılmalı
            bool isValid = await _couponService.ValidateCouponAsync(code);
            
            if (!isValid)
                return BadRequest(new { message = "Girdiğiniz kupon kodu geçersiz veya süresi dolmuş." });

            // Burada kupon bilgilerini sepet/oturum servisine kaydetme işlemi yapılabilir
            
            return Ok(new { message = "Kupon başarıyla uygulandı." });
        }
    }
}