using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.User;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;

namespace ECommerce.API.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public AdminUsersController(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
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
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            var targetRole = string.IsNullOrWhiteSpace(dto.Role) ? Roles.User : dto.Role;
            if (!IsAllowedRole(targetRole))
            {
                return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
            }

            if (targetRole == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                return Forbid();
            }

            var user = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.Email,
                Role = targetRole
            };

            var passwordHasher = new PasswordHasher<User>();
            user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);

            await _userService.AddAsync(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var oldSnapshot = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.IsActive
            };

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (!IsAllowedRole(dto.Role))
                {
                    return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
                }

                if (dto.Role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
                {
                    return Forbid();
                }

                user.Role = dto.Role;
            }
            // şifre güncelleme opsiyonel olarak eklenebilir

            await _userService.UpdateAsync(user);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserUpdated",
                "User",
                id.ToString(),
                oldSnapshot,
                new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.IsActive
                });
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var oldSnapshot = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.IsActive
            };

            await _userService.DeleteAsync(user);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserDisabled",
                "User",
                id.ToString(),
                oldSnapshot,
                new
                {
                    user.IsActive
                });
            return NoContent();
        }

        [HttpPut("{id}/role")]
        [Authorize(Roles = Roles.AdminLike)]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRoleUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest(new { success = false, message = "Rol zorunludur." });
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            if (!IsAllowedRole(dto.Role))
            {
                return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
            }

            if (dto.Role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                return Forbid();
            }

            var oldRole = user.Role;
            user.Role = dto.Role;
            await _userService.UpdateAsync(user);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserRoleUpdated",
                "User",
                id.ToString(),
                new { OldRole = oldRole },
                new { NewRole = user.Role });
            return Ok(new { success = true, id = user.Id, role = user.Role });
        }

        private static bool IsAllowedRole(string? role) =>
            role == Roles.SuperAdmin ||
            role == Roles.Admin ||
            role == Roles.User;

        private int GetAdminUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
        }
    }
}
