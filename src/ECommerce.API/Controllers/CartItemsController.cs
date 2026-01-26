using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Validators;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ECommerce.API.Controllers
{
    /// <summary>
    /// Sepet API Controller
    /// Hem kayıtlı kullanıcı (JWT) hem misafir kullanıcı (CartToken) destekler
    /// 
    /// Endpoint Yapısı:
    /// - /api/cartitems/guest/* → Misafir kullanıcılar (AllowAnonymous)
    /// - /api/cartitems/* → Kayıtlı kullanıcılar veya genel
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IPricingEngine _pricingEngine;
        private readonly ICartService _cartService;
        private readonly ILogger<CartItemsController> _logger;

        public CartItemsController(
            ECommerceDbContext context, 
            IPricingEngine pricingEngine,
            ICartService cartService,
            ILogger<CartItemsController> logger)
        {
            _context = context;
            _pricingEngine = pricingEngine;
            _cartService = cartService;
            _logger = logger;
        }

        #region Misafir Kullanıcı Endpoint'leri (CartToken bazlı)

        /// <summary>
        /// Misafir kullanıcının sepetini getirir
        /// CartToken header veya query string'den alınır
        /// </summary>
        [HttpGet("guest", Order = 0)]
        [AllowAnonymous]
        public async Task<ActionResult<CartSummaryDto>> GetGuestCart(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromQuery] string? cartToken)
        {
            var token = headerToken ?? cartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("[CART-API] Boş token ile guest sepet istendi");
                return Ok(new CartSummaryDto { Items = new List<CartItemDto>(), Total = 0 });
            }

            var cart = await _cartService.GetCartByTokenAsync(token);
            return Ok(cart);
        }

        /// <summary>
        /// Misafir kullanıcının sepetine ürün ekler
        /// Aynı ürün varsa miktarı artırır
        /// </summary>
        [HttpPost("guest", Order = 0)]
        [AllowAnonymous]
        public async Task<ActionResult<CartItemDto>> AddToGuestCart(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromBody] GuestCartItemRequest request)
        {
            // Token: Header > Body > Query sıralamasıyla al
            var token = headerToken ?? request?.CartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "CartToken gerekli. Header'da X-Cart-Token veya body'de cartToken gönderin." });
            }

            // Validasyon
            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { message = "Geçersiz ürün bilgisi." });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new { message = "Miktar 0'dan büyük olmalı." });
            }

            // Ürün ve stok kontrolü
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null || !product.IsActive)
            {
                return NotFound(new { message = "Ürün bulunamadı." });
            }

            // Varyant kontrolü (varsa)
            ProductVariant? variant = null;
            int availableStock = product.StockQuantity;
            decimal unitPrice = product.SpecialPrice ?? product.Price;

            if (request.VariantId.HasValue)
            {
                variant = await _context.ProductVariants.FindAsync(request.VariantId.Value);
                if (variant == null || !variant.IsActive || variant.ProductId != product.Id)
                {
                    return BadRequest(new { message = "Geçersiz varyant." });
                }
                availableStock = variant.Stock;
                unitPrice = variant.Price;
            }

            // Stok kontrolü
            if (request.Quantity > availableStock)
            {
                return BadRequest(new { 
                    message = $"Yetersiz stok. Maksimum {availableStock} adet ekleyebilirsiniz.",
                    availableStock = availableStock
                });
            }

            // Sepete ekle
            var cartItem = await _cartService.AddItemToCartByTokenAsync(token, new CartItemDto
            {
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                UnitPrice = unitPrice,
                ProductName = product.Name,
                ProductImage = product.ImageUrl
            });

            _logger.LogInformation(
                "[CART-API] Guest sepete eklendi. Token: {Token}, ProductId: {ProductId}, Qty: {Qty}", 
                token[..Math.Min(8, token.Length)] + "...", request.ProductId, request.Quantity);

            return Ok(cartItem);
        }

        /// <summary>
        /// Misafir kullanıcının sepet öğesini günceller
        /// </summary>
        [HttpPut("guest")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateGuestCartItem(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromBody] GuestCartUpdateRequest request)
        {
            var token = headerToken ?? request?.CartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "CartToken gerekli." });
            }

            if (request == null || request.ProductId <= 0)
            {
                return BadRequest(new { message = "Geçersiz ürün bilgisi." });
            }

            // Stok kontrolü (miktar > 0 ise)
            if (request.Quantity > 0)
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Ürün bulunamadı." });
                }

                int availableStock = product.StockQuantity;
                if (request.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(request.VariantId.Value);
                    if (variant != null)
                    {
                        availableStock = variant.Stock;
                    }
                }

                if (request.Quantity > availableStock)
                {
                    return BadRequest(new { 
                        message = $"Yetersiz stok. Maksimum {availableStock} adet.",
                        availableStock = availableStock
                    });
                }
            }

            await _cartService.UpdateCartItemByTokenAsync(
                token, request.ProductId, request.Quantity, request.VariantId);

            return NoContent();
        }

        /// <summary>
        /// Misafir kullanıcının sepetinden ürün siler
        /// </summary>
        [HttpDelete("guest/{productId}", Order = 0)]
        [AllowAnonymous]
        public async Task<ActionResult> RemoveFromGuestCart(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromQuery] string? cartToken,
            int productId,
            [FromQuery] int? variantId = null)
        {
            var token = headerToken ?? cartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "CartToken gerekli." });
            }

            await _cartService.RemoveCartItemByTokenAsync(token, productId, variantId);
            return NoContent();
        }

        /// <summary>
        /// Misafir kullanıcının sepetini temizler
        /// </summary>
        [HttpDelete("guest/clear", Order = 0)]
        [AllowAnonymous]
        public async Task<ActionResult> ClearGuestCart(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromQuery] string? cartToken)
        {
            var token = headerToken ?? cartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "CartToken gerekli." });
            }

            await _cartService.ClearCartByTokenAsync(token);
            return NoContent();
        }

        #endregion

        #region Merge Endpoint (Login Sonrası)

        /// <summary>
        /// Misafir sepetini kayıtlı kullanıcıya transfer eder
        /// Login sonrası frontend tarafından çağrılır
        /// </summary>
        [HttpPost("merge")]
        [Authorize]
        public async Task<ActionResult<MergeCartResponse>> MergeCart(
            [FromHeader(Name = "X-Cart-Token")] string? headerToken,
            [FromBody] MergeCartRequest? request)
        {
            var token = headerToken ?? request?.CartToken;
            
            if (string.IsNullOrWhiteSpace(token))
            {
                return Ok(new MergeCartResponse { MergedCount = 0, Message = "Token yok, merge yapılmadı." });
            }

            // Kullanıcı ID'sini JWT'den al
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı." });
            }

            var mergedCount = await _cartService.MergeGuestCartToUserAsync(token, userId);

            _logger.LogInformation(
                "[CART-API] Sepet merge tamamlandı. UserId: {UserId}, Merged: {Count}", 
                userId, mergedCount);

            return Ok(new MergeCartResponse 
            { 
                MergedCount = mergedCount, 
                Message = mergedCount > 0 
                    ? $"{mergedCount} ürün hesabınıza aktarıldı." 
                    : "Misafir sepet boş, aktarılacak ürün yok."
            });
        }

        #endregion

        #region Mevcut Endpoint'ler (Backward Compatibility)

        [HttpGet]
        public async Task<IActionResult> GetCartItems()
        {
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.IsActive)
                .ToListAsync();

            // DTO'ya çevir - JSON cycle hatasını önlemek için
            var result = items.Select(c => new
            {
                c.Id,
                c.UserId,
                c.ProductId,
                c.Quantity,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt,
                Product = c.Product == null ? null : new
                {
                    c.Product.Id,
                    c.Product.Name,
                    c.Product.Description,
                    c.Product.Price,
                    c.Product.StockQuantity,
                    c.Product.ImageUrl,
                    c.Product.IsActive
                }
            });

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCartItem(int id)
        {
            var item = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            
            if (item == null) return NotFound();

            // DTO'ya çevir - JSON cycle hatasını önlemek için
            var result = new
            {
                item.Id,
                item.UserId,
                item.ProductId,
                item.Quantity,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt,
                Product = item.Product == null ? null : new
                {
                    item.Product.Id,
                    item.Product.Name,
                    item.Product.Description,
                    item.Product.Price,
                    item.Product.StockQuantity,
                    item.Product.ImageUrl,
                    item.Product.IsActive
                }
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CartItem>> CreateCartItem([FromBody] CartItemDto dto)
        {
            // DTO validasyonu
            if (!CartValidator.Validate(dto, out string error))
                return BadRequest(new { message = error });

            // Ürün ve stok kontrolü
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Ürün bulunamadı." });
            }

            if (dto.Quantity > product.StockQuantity)
            {
                return BadRequest(new { message = $"Yetersiz stok. Maksimum {product.StockQuantity} adet." });
            }

            // Kullanıcı ID veya CartToken kontrolü
            var userId = GetCurrentUserId();
            var cartToken = GetCartToken();

            if (!userId.HasValue && string.IsNullOrEmpty(cartToken))
            {
                return BadRequest(new { message = "Kullanıcı veya sepet bilgisi bulunamadı." });
            }

            // Entity oluşturma
            var item = new CartItem
            {
                ProductId = dto.ProductId,
                ProductVariantId = dto.VariantId,
                Quantity = dto.Quantity,
                UserId = userId, // Nullable - null olabilir
                CartToken = cartToken,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.CartItems.Add(item);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCartItem), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] CartItemDto dto)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null || !item.IsActive) return NotFound();

            // DTO validasyonu
            if (!CartValidator.Validate(dto, out string error))
                return BadRequest(new { message = error });

            // Stok kontrolü
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return BadRequest(new { message = "Ürün bulunamadı." });
            }

            if (dto.Quantity > product.StockQuantity)
            {
                return BadRequest(new { message = $"Yetersiz stok. Maksimum {product.StockQuantity} adet." });
            }

            // Entity güncelleme
            item.ProductId = dto.ProductId;
            item.Quantity = dto.Quantity;
            item.UpdatedAt = DateTime.UtcNow;

            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null) return NotFound();
            
            // Soft delete
            item.IsActive = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        #endregion

        #region Fiyat Hesaplama

        public class CartPricePreviewRequest
        {
            public List<Core.DTOs.Pricing.CartItemInputDto> Items { get; set; } = new();
            public string? CouponCode { get; set; }
        }

        [HttpPost("price-preview")]
        [AllowAnonymous] // Misafirler de fiyat hesaplama yapabilmeli
        public async Task<IActionResult> PricePreview([FromBody] CartPricePreviewRequest request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { message = "Geçersiz sepet verisi." });
            }

            int? userId = GetCurrentUserId();
            var pricingResult = await _pricingEngine.CalculateCartAsync(userId, request.Items, request.CouponCode);
            return Ok(pricingResult);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// JWT'den kullanıcı ID'sini alır (yoksa null)
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Header veya Query'den CartToken alır
        /// </summary>
        private string? GetCartToken()
        {
            // Önce header'dan dene
            if (Request.Headers.TryGetValue("X-Cart-Token", out var headerToken))
            {
                return headerToken.ToString();
            }
            
            // Sonra query string'den dene
            if (Request.Query.TryGetValue("cartToken", out var queryToken))
            {
                return queryToken.ToString();
            }

            return null;
        }

        #endregion
    }

    #region Request/Response Models

    /// <summary>
    /// Misafir sepete ekleme isteği
    /// </summary>
    public class GuestCartItemRequest
    {
        public string? CartToken { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Misafir sepet güncelleme isteği
    /// </summary>
    public class GuestCartUpdateRequest
    {
        public string? CartToken { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Sepet merge isteği
    /// </summary>
    public class MergeCartRequest
    {
        public string? CartToken { get; set; }
    }

    /// <summary>
    /// Sepet merge yanıtı
    /// </summary>
    public class MergeCartResponse
    {
        public int MergedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion
}
