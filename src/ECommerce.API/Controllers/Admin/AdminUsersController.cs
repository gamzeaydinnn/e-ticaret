using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.User;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]  // sadece admin erişebilir
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminUsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            var user = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Password = dto.Password
                // Diğer alanları istersen ekleyebilirsin
            };

            await _userService.AddAsync(user);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userService.DeleteAsync(user);
            return NoContent();
        }
    }
}
