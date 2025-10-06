using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/brands")]
    [Authorize(Roles = "Admin")]
    public class AdminBrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;
        public AdminBrandsController(IBrandService brandService) => _brandService = brandService;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _brandService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var b = await _brandService.GetByIdAsync(id);
            if (b == null) return NotFound();
            return Ok(b);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Brand brand)
        {
            await _brandService.AddAsync(brand);
            return CreatedAtAction(nameof(Get), new { id = brand.Id }, brand);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Brand brand)
        {
            var existing = await _brandService.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Name = brand.Name;
            existing.Description = brand.Description;
            existing.LogoUrl = brand.LogoUrl;
            await _brandService.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _brandService.DeleteAsync(id);
            return NoContent();
        }
    }
}
