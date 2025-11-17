using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Validators;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;


namespace ECommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IPricingEngine _pricingEngine;

        public CartItemsController(ECommerceDbContext context, IPricingEngine pricingEngine)
        {
            _context = context;
            _pricingEngine = pricingEngine;
        }

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

            // Stok kontrolü: istenen miktar mevcut stoktan fazla olamaz
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = "Ürün bulunamadı." });
            }

            if (dto.Quantity > product.StockQuantity)
            {
                return BadRequest(new
                {
                    message = $"Yetersiz stok. Maksimum {product.StockQuantity} adet ekleyebilirsiniz."
                });
            }

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

        public class CartPricePreviewRequest
        {
            public List<Core.DTOs.Pricing.CartItemInputDto> Items { get; set; } = new();
            public string? CouponCode { get; set; }
        }

        [HttpPost("price-preview")]
        public async Task<IActionResult> PricePreview([FromBody] CartPricePreviewRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { message = "Geçersiz sepet verisi." });
            }

            int? userId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsed))
            {
                userId = parsed;
            }

            var pricingResult = await _pricingEngine.CalculateCartAsync(userId, request.Items, request.CouponCode);
            return Ok(pricingResult);
        }
    }
}
