using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                var userList = users.Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    FullName = $"{u.FirstName} {u.LastName}",
                    u.IsActive,
                    u.CreatedAt,
                    u.Role
                }).ToList();

                return Ok(new { success = true, data = userList, count = userList.Count });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });

                var userData = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    user.IsActive,
                    user.CreatedAt,
                    user.Role,
                    user.Address,
                    user.City
                };

                return Ok(new { success = true, data = userData });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}