using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using System.Threading.Tasks;


namespace ECommerce.API.Controllers // Ana namespace, Admin DEĞİL
{
    /*Halkın ve kullanıcıların markaları görmesi için ayrı bir BrandsController.
     Bu sınıf sadece GET metotlarını içermelidir ve yetkilendirme olamaz*/
    [ApiController]
    [Route("api/[controller]")] // Rota: api/brands
    // [Authorize] veya [Authorize(Roles="Admin")] YOK! Herkes erişebilir.
    public class BrandsController : ControllerBase
    {
        private readonly IBrandService _brandService;

        public BrandsController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        // GET: api/brands
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Kullanıcıların göreceği aktif markalar getirilir
            return Ok(await _brandService.GetAllAsync());
        }

        // GET: api/brands/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var brand = await _brandService.GetByIdAsync(id);
            if (brand == null) return NotFound();
            return Ok(brand);
        }
    }
}