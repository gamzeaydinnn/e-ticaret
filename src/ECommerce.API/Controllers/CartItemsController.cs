using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
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
        public async Task<ActionResult<CartItem>> CreateCartItem(CartItem item)
        {
            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCartItem), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, CartItem item)
        {
            if (id != item.Id) return BadRequest();
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
