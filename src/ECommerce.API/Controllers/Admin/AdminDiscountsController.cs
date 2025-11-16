using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Constants;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;

namespace ECommerce.API.Controllers.Admin
{
    [Authorize(Roles = Roles.AdminLike)]
    [ApiController]
    [Route("api/admin/discounts")] // api/admin/discounts
    public class AdminDiscountsController : ControllerBase
    {
        private readonly IDiscountService _discountService;

        public AdminDiscountsController(IDiscountService discountService)
        {
            _discountService = discountService;
        }

        // PUT: api/admin/discounts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] Discount discount)
        {
            if (id != discount.Id)
                return BadRequest(new { message = "Rota ID'si ile indirim ID'si eşleşmiyor." });

            if (discount.EndDate < discount.StartDate)
                return BadRequest(new { message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });

            var existingDiscount = await _discountService.GetByIdAsync(id);
            if (existingDiscount == null)
                return NotFound();

            existingDiscount.Title = discount.Title;
            existingDiscount.Value = discount.Value;
            existingDiscount.IsPercentage = discount.IsPercentage;
            existingDiscount.StartDate = discount.StartDate;
            existingDiscount.EndDate = discount.EndDate;
            existingDiscount.IsActive = discount.IsActive;
            existingDiscount.ConditionsJson = discount.ConditionsJson;

            await _discountService.UpdateAsync(existingDiscount);
            return NoContent();
        }
    }
}
