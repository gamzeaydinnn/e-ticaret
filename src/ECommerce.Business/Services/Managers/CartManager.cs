using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Core.DTOs.Cart;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Sepet iş mantığı servisi
    /// Hem kayıtlı kullanıcı (UserId) hem misafir kullanıcı (CartToken) destekler
    /// 
    /// SOLID Prensipleri:
    /// - Single Responsibility: Sadece sepet iş mantığı
    /// - Open/Closed: Yeni sepet tipleri (ör. session-based) extension ile eklenebilir
    /// - Dependency Inversion: Repository interface'ine bağımlı
    /// </summary>
    public class CartManager : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly ILogger<CartManager>? _logger;

        public CartManager(
            ICartRepository cartRepository, 
            ILogger<CartManager>? logger = null)
        {
            _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
            _logger = logger;
        }

        #region Kayıtlı Kullanıcı Metodları (UserId bazlı)

        /// <summary>
        /// Kayıtlı kullanıcının sepetini DTO olarak döner
        /// Fiyatlar ürün bilgisinden hesaplanır
        /// </summary>
        public async Task<CartSummaryDto> GetCartAsync(int userId)
        {
            var items = await _cartRepository.GetByUserIdAsync(userId);
            return MapToCartSummary(items);
        }

        /// <summary>
        /// Kayıtlı kullanıcının sepetine ürün ekler
        /// Aynı ürün varsa miktarı artırır (upsert)
        /// </summary>
        public async Task AddItemToCartAsync(int userId, CartItemDto item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var existing = await _cartRepository.GetByUserAndProductAsync(userId, item.ProductId);
            
            if (existing != null)
            {
                // Mevcut öğe - miktarı artır
                existing.Quantity += item.Quantity;
                existing.UpdatedAt = DateTime.UtcNow;
                await _cartRepository.UpdateAsync(existing);
                
                _logger?.LogDebug(
                    "[CART] Miktar güncellendi. UserId: {UserId}, ProductId: {ProductId}, Yeni: {Qty}", 
                    userId, item.ProductId, existing.Quantity);
            }
            else
            {
                // Yeni öğe ekle
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = item.ProductId,
                    ProductVariantId = item.VariantId,
                    Quantity = item.Quantity,
                    CartToken = null, // Kayıtlı kullanıcı, token yok
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _cartRepository.AddAsync(cartItem);
                
                _logger?.LogInformation(
                    "[CART] Ürün eklendi. UserId: {UserId}, ProductId: {ProductId}, Qty: {Qty}", 
                    userId, item.ProductId, item.Quantity);
            }
        }

        /// <summary>
        /// Kayıtlı kullanıcının sepet öğesinin miktarını günceller
        /// Miktar 0 veya altına düşerse öğe silinir
        /// </summary>
        public async Task UpdateCartItemAsync(int userId, int cartItemId, int quantity)
        {
            var item = await _cartRepository.GetByIdAsync(cartItemId);
            
            if (item == null || item.UserId != userId)
            {
                _logger?.LogWarning(
                    "[CART] Güncelleme başarısız - öğe bulunamadı. UserId: {UserId}, CartItemId: {CartItemId}", 
                    userId, cartItemId);
                return;
            }

            if (quantity <= 0)
            {
                // Miktar 0 veya altı = sil
                await RemoveCartItemAsync(userId, cartItemId);
                return;
            }

            item.Quantity = quantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(item);
        }

        /// <summary>
        /// Kayıtlı kullanıcının sepetinden öğe siler
        /// Yetki kontrolü: Sadece kendi öğesini silebilir
        /// </summary>
        public async Task RemoveCartItemAsync(int userId, int cartItemId)
        {
            var item = await _cartRepository.GetByIdAsync(cartItemId);
            
            if (item == null || item.UserId != userId)
            {
                _logger?.LogWarning(
                    "[CART] Silme başarısız - yetki yok. UserId: {UserId}, CartItemId: {CartItemId}", 
                    userId, cartItemId);
                return;
            }

            await _cartRepository.RemoveCartItemAsync(cartItemId);
        }

        /// <summary>
        /// Kayıtlı kullanıcının sepetini tamamen temizler
        /// </summary>
        public async Task ClearCartAsync(int userId)
        {
            await _cartRepository.ClearCartAsync(userId);
            _logger?.LogInformation("[CART] Sepet temizlendi. UserId: {UserId}", userId);
        }

        #endregion

        #region Misafir Kullanıcı Metodları (CartToken bazlı)

        /// <summary>
        /// Misafir kullanıcının sepetini CartToken ile getirir
        /// Token boşsa boş sepet döner (hata fırlatmaz)
        /// </summary>
        public async Task<CartSummaryDto> GetCartByTokenAsync(string cartToken)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                _logger?.LogDebug("[CART] Boş token ile sepet istendi - boş sepet dönüyor");
                return new CartSummaryDto { Items = new List<CartItemDto>(), Total = 0 };
            }

            var items = await _cartRepository.GetByCartTokenAsync(cartToken);
            return MapToCartSummary(items);
        }

        /// <summary>
        /// Misafir kullanıcının sepetine ürün ekler
        /// Aynı ürün+varyant varsa miktarı artırır (upsert)
        /// </summary>
        public async Task<CartItemDto> AddItemToCartByTokenAsync(string cartToken, CartItemDto item)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                throw new ArgumentException("CartToken boş olamaz", nameof(cartToken));
            
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var existing = await _cartRepository.GetByTokenAndProductAsync(
                cartToken, item.ProductId, item.VariantId);
            
            CartItem cartItem;
            
            if (existing != null)
            {
                // Mevcut öğe - miktarı artır
                existing.Quantity += item.Quantity;
                existing.UpdatedAt = DateTime.UtcNow;
                await _cartRepository.UpdateAsync(existing);
                cartItem = existing;
                
                _logger?.LogDebug(
                    "[CART-GUEST] Miktar güncellendi. Token: {Token}, ProductId: {ProductId}, Yeni: {Qty}", 
                    cartToken[..8] + "...", item.ProductId, existing.Quantity);
            }
            else
            {
                // Yeni öğe ekle
                cartItem = new CartItem
                {
                    UserId = null, // Misafir kullanıcı
                    ProductId = item.ProductId,
                    ProductVariantId = item.VariantId,
                    Quantity = item.Quantity,
                    CartToken = cartToken,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _cartRepository.AddAsync(cartItem);
                
                _logger?.LogInformation(
                    "[CART-GUEST] Ürün eklendi. Token: {Token}, ProductId: {ProductId}, Qty: {Qty}", 
                    cartToken[..8] + "...", item.ProductId, item.Quantity);
            }

            // Güncel bilgiyi DTO olarak dön
            return new CartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                VariantId = cartItem.ProductVariantId,
                Quantity = cartItem.Quantity,
                UnitPrice = item.UnitPrice // Fiyat frontend'den geldi (backend'de doğrulanmalı!)
            };
        }

        /// <summary>
        /// Misafir kullanıcının sepet öğesinin miktarını günceller
        /// ProductId + VariantId ile benzersiz öğe bulunur
        /// </summary>
        public async Task UpdateCartItemByTokenAsync(
            string cartToken, int productId, int quantity, int? variantId = null)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                return;

            if (quantity <= 0)
            {
                await RemoveCartItemByTokenAsync(cartToken, productId, variantId);
                return;
            }

            var item = await _cartRepository.GetByTokenAndProductAsync(cartToken, productId, variantId);
            
            if (item == null)
            {
                _logger?.LogWarning(
                    "[CART-GUEST] Güncelleme başarısız - öğe bulunamadı. Token: {Token}, ProductId: {ProductId}", 
                    cartToken[..8] + "...", productId);
                return;
            }

            item.Quantity = quantity;
            item.UpdatedAt = DateTime.UtcNow;
            await _cartRepository.UpdateAsync(item);
        }

        /// <summary>
        /// Misafir kullanıcının sepetinden öğe siler
        /// ProductId + VariantId ile benzersiz öğe bulunur
        /// </summary>
        public async Task RemoveCartItemByTokenAsync(string cartToken, int productId, int? variantId = null)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                return;

            var item = await _cartRepository.GetByTokenAndProductAsync(cartToken, productId, variantId);
            
            if (item != null)
            {
                await _cartRepository.RemoveCartItemAsync(item.Id);
                
                _logger?.LogInformation(
                    "[CART-GUEST] Ürün silindi. Token: {Token}, ProductId: {ProductId}", 
                    cartToken[..8] + "...", productId);
            }
        }

        /// <summary>
        /// Misafir kullanıcının sepetini tamamen temizler
        /// </summary>
        public async Task ClearCartByTokenAsync(string cartToken)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                return;

            await _cartRepository.ClearCartByTokenAsync(cartToken);
            _logger?.LogInformation("[CART-GUEST] Sepet temizlendi. Token: {Token}", cartToken[..8] + "...");
        }

        #endregion

        #region Merge Operasyonları

        /// <summary>
        /// Misafir sepetini kayıtlı kullanıcıya transfer eder
        /// Login sonrası frontend tarafından çağrılır
        /// </summary>
        public async Task<int> MergeGuestCartToUserAsync(string cartToken, int userId)
        {
            return await _cartRepository.MergeGuestCartToUserAsync(cartToken, userId);
        }

        #endregion

        #region Ortak Metodlar

        /// <summary>
        /// Toplam sepet öğesi sayısı (admin dashboard için)
        /// </summary>
        public async Task<int> GetCartCountAsync()
        {
            return await _cartRepository.GetCartCountAsync();
        }

        #endregion

        #region Private Helper Metodları

        /// <summary>
        /// CartItem listesini CartSummaryDto'ya dönüştürür
        /// Fiyatlar: SpecialPrice > Price sıralamasıyla hesaplanır
        /// </summary>
        private static CartSummaryDto MapToCartSummary(IEnumerable<CartItem> items)
        {
            var cartItems = items.Select(i =>
            {
                // Fiyat hesaplama: Önce varyant fiyatı, sonra ürün özel fiyatı, en son normal fiyat
                var unitPrice = i.ProductVariant?.Price 
                    ?? i.Product?.SpecialPrice 
                    ?? i.Product?.Price 
                    ?? 0m;

                return new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    VariantId = i.ProductVariantId,
                    Quantity = i.Quantity,
                    UnitPrice = unitPrice,
                    // Ek bilgiler (frontend için)
                    ProductName = i.Product?.Name,
                    ProductImage = i.Product?.ImageUrl,
                    VariantTitle = i.ProductVariant?.Title,
                    Sku = i.ProductVariant?.SKU
                };
            }).ToList();

            return new CartSummaryDto
            {
                Items = cartItems,
                Total = cartItems.Sum(i => i.UnitPrice * i.Quantity)
            };
        }

        #endregion
    }
}
