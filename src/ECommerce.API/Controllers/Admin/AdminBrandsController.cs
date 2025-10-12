using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;

namespace ECommerce.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/[controller]")] // api/admin/brands
    public class AdminBrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public AdminBrandsController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        // GET: api/admin/brands
        [HttpGet]
        public async Task<IActionResult> GetAllBrands()
        {
            var brands = await _brandService.GetAllAsync();
            return Ok(brands);
        }

        // GET: api/admin/brands/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBrandById(int id)
        {
            var brand = await _brandService.GetByIdAsync(id);
            if (brand == null)
                return NotFound();
            return Ok(brand);
        }

        // POST: api/admin/brands
        [HttpPost]
        public async Task<IActionResult> CreateBrand([FromBody] Brand brand)
        {
            // ID genellikle veritabanı tarafından atanır, bu yüzden sıfırlanabilir veya DTO kullanılabilir.
            brand.Id = 0; 
            await _brandService.AddAsync(brand);
            // CreatedAtAction ile 201 Created döndürülür
            return CreatedAtAction(nameof(GetBrandById), new { id = brand.Id }, brand);
        }

        // PUT: api/admin/brands/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] Brand brand)
        {
            if (id != brand.Id)
                return BadRequest(new { message = "Rota ID'si ile marka ID'si eşleşmiyor." });
            
            var existingBrand = await _brandService.GetByIdAsync(id);
            if (existingBrand == null)
                return NotFound();
            
            // Güncelleme alanlarını kopyala
            existingBrand.Name = brand.Name;
            // Diğer alanlar güncellenecekse buraya eklenebilir.

            await _brandService.UpdateAsync(existingBrand);
            return NoContent(); // 204 No Content
        }

        // DELETE: api/admin/brands/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var existingBrand = await _brandService.GetByIdAsync(id);
            if (existingBrand == null)
                return NotFound();

            await _brandService.DeleteAsync(id);
            return NoContent(); // 204 No Content
        }
    }
}