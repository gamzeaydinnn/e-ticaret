using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Managers;
using ECommerce.Core.DTOs.Product;

namespace ECommerce.API.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class AdminProductController : ControllerBase
    {
        private readonly ProductManager _productManager;

        public AdminProductController(ProductManager productManager)
        {
            _productManager = productManager;
        }

        [HttpPost("create")]
    public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto dto)
    {
    var product = await _productManager.CreateAsync(dto);
    return Ok(product);
    }

    }
}
