using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Validators;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        public CartItemsController(ECommerceDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItem>>> GetCartItems()
            => await _context.CartItems.Include(c => c.Product).ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<CartItem>> GetCartItem(int id)
        {
            var item = await _context.CartItems.Include(c => c.Product)
                                               .FirstOrDefaultAsync(c => c.Id == id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<CartItem>> CreateCartItem([FromBody] CartItemDto dto)
        {
            // DTO validasyonu
            if (!CartValidator.Validate(dto, out string error))
                return BadRequest(new { message = error });

            // Entity oluşturma
            var item = new CartItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UserId = 0,          // giriş yoksa örnek değer
                CartToken = "guest"  // örnek guest token
            };

            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCartItem), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] CartItemDto dto)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();

            // DTO validasyonu
            if (!CartValidator.Validate(dto, out string error))
                return BadRequest(new { message = error });

            // Entity güncelleme
            item.ProductId = dto.ProductId;
            item.Quantity = dto.Quantity;

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
