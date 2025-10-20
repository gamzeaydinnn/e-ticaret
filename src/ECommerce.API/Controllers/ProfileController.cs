using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Auth;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public ProfileController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber
                }
            });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { success = false, message = "Profil güncellenemedi" });

            return Ok(new { success = true, message = "Profil güncellendi" });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound(new { success = false, message = "Kullanıcı bulunamadı" });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { success = false, message = "Yeni şifreler eşleşmiyor" });

            var result = await _userManager.ChangePasswordAsync(user, dto.OldPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = errors });
            }

            return Ok(new { success = true, message = "Şifre değiştirildi" });
        }
    }
}
