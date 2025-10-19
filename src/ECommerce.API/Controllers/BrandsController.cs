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
            // ✅ DTO döndüren yeni metot kullanıldı.
            return Ok(await _brandService.GetAllBrandsDtoAsync());
        }

        // GET: api/brands/slug-ismi (Örn: api/brands/ulker)
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            // Slug uzunluğu genellikle ID'den daha uzun olacağı için
            // bu metodu ID yerine slug ile arama için kullanıyoruz.
            
            var brand = await _brandService.GetBrandBySlugAsync(slug);
            if (brand == null)
            {
                // Ek olarak, eğer slug numeric ise ID ile arama yapma yedek mekanizması (opsiyonel)
                if (int.TryParse(slug, out int id))
                {
                    brand = await _brandService.GetBrandDtoByIdAsync(id);
                }
            }
            
            if (brand == null) return NotFound();
            return Ok(brand);
        }
        
        // **Not:** Eğer sadece ID ile arama isterseniz, önceki Get metodunuzu
        // GetBrandDtoByIdAsync metodunu kullanacak şekilde de güncelleyebilirdiniz. 
        // Ancak web sitelerinde marka linkleri genellikle slug kullanır.
    

        

        // GET: api/brands/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var brand = await _brandService.GetByIdAsync(id);
            if (brand == null) return NotFound();
            return Ok(brand);
        }
    }
}
