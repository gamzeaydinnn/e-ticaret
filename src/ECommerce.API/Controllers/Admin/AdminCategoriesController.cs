/*using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Abstract;
using ECommerce.Entities.Concrete;
namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminCategoriesController : ControllerBase
    {
        private readonly IAdminService _adminService;
public AdminCategoriesController(IAdminService adminService)
        {
            _adminService = adminService;
        }
[HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_adminService.GetAllCategories());
        }
[HttpPost]
        public IActionResult Add(Category category)
        {
            var result = _adminService.AddCategory(category);
            return Ok(result);
        }
[HttpPut]
        public IActionResult Update(Category category)
        {
            var result = _adminService.UpdateCategory(category);
            return Ok(result);
        }
[HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var success = _adminService.DeleteCategory(id);
            if (!success) return NotFound();
            return Ok();
        }
    }
}
 
*/