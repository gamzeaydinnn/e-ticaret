using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var addresses = await _addressService.GetByUserIdAsync(userId);
            return Ok(new { success = true, data = addresses });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] Address address)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            address.UserId = userId;
            await _addressService.AddAsync(address);

            return Ok(new { success = true, data = address });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] Address updatedAddress)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var address = await _addressService.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return NotFound(new { success = false, message = "Adres bulunamadı" });

            address.Title = updatedAddress.Title;
            address.FullName = updatedAddress.FullName;
            address.Phone = updatedAddress.Phone;
            address.City = updatedAddress.City;
            address.District = updatedAddress.District;
            address.Street = updatedAddress.Street;
            address.PostalCode = updatedAddress.PostalCode;
            address.IsDefault = updatedAddress.IsDefault;

            await _addressService.UpdateAsync(address);

            return Ok(new { success = true, data = address });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var address = await _addressService.GetByIdAsync(id);
            if (address == null || address.UserId != userId)
                return NotFound(new { success = false, message = "Adres bulunamadı" });

            await _addressService.DeleteAsync(id);

            return Ok(new { success = true, message = "Adres silindi" });
        }
    }
}
