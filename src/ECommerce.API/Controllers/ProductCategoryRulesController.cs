using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/config/[controller]")]
    public class ProductCategoryRulesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ProductCategoryRulesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Config", "productCategoryRules.json");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var json = System.IO.File.ReadAllText(filePath);
            try
            {
                var doc = JsonSerializer.Deserialize<object>(json);
                return Ok(doc);
            }
            catch
            {
                return StatusCode(500);
            }
        }
    }
}
