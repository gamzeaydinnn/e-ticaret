using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/discounts")]
    [Authorize(Roles = "Admin")]
    public class AdminDiscountsController : ControllerBase
    {
        private readonly IDiscountService _discountService;
        public AdminDiscountsController(IDiscountService discountService) => _discountService = discountService;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _discountService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var d = await _discountService.GetByIdAsync(id);
            if (d == null) return NotFound();
            return Ok(d);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Discount discount)
        {
            await _discountService.AddAsync(discount);
            return CreatedAtAction(nameof(Get), new { id = discount.Id }, discount);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Discount discount)
        {
            var existing = await _discountService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Title = discount.Title;
            existing.Value = discount.Value;
            existing.IsPercentage = discount.IsPercentage;
            existing.StartDate = discount.StartDate;
            existing.EndDate = discount.EndDate;
            existing.IsActive = discount.IsActive;
            existing.ConditionsJson = discount.ConditionsJson;

            await _discountService.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _discountService.DeleteAsync(id);
            return NoContent();
        }
    }
}
