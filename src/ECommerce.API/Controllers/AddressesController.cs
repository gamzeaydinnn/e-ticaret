using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/addresses")]
    [Authorize] // user must be logged in
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;
        public AddressesController(IAddressService addressService) => _addressService = addressService;

        private int GetUserIdFromToken()
        {
            var sub = User.FindFirst("sub")?.Value;
            return int.TryParse(sub, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAddresses()
        {
            var userId = GetUserIdFromToken();
            var list = await _addressService.GetByUserIdAsync(userId);
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Address address)
        {
            address.UserId = GetUserIdFromToken();
            await _addressService.AddAsync(address);
            return CreatedAtAction(nameof(GetMyAddresses), null, address);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Address address)
        {
            var existing = await _addressService.GetByIdAsync(id);
            if (existing == null) return NotFound();
            if (existing.UserId != GetUserIdFromToken()) return Forbid();

            existing.FullName = address.FullName;
            existing.City = address.City;
            existing.District = address.District;
            existing.Street = address.Street;
            existing.PostalCode = address.PostalCode;
            existing.PhoneNumber = address.PhoneNumber;
            existing.IsDefault = address.IsDefault;
            await _addressService.UpdateAsync(existing);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _addressService.GetByIdAsync(id);
            if (item == null) return NotFound();
            if (item.UserId != GetUserIdFromToken()) return Forbid();
            await _addressService.DeleteAsync(id);
            return NoContent();
        }
    }
}
