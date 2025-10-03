using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class CartManager : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartManager(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        public async Task<CartSummaryDto> GetCartAsync(Guid userId)
        {
            var items = await _cartRepository.GetByUserIdAsync(userId.GetHashCode()); // Guid -> int userId örnek
            return new CartSummaryDto
            {
                Items = items.Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                Total = items.Sum(i => (i.Product?.Price ?? 0) * i.Quantity)
            };
        }

        public async Task AddToCartAsync(Guid userId, int productVariantId, int quantity)
        {
            var existing = await _cartRepository.GetByUserAndProductAsync(userId.GetHashCode(), productVariantId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                await _cartRepository.AddAsync(new CartItem
                {
                    UserId = userId.GetHashCode(),
                    ProductId = productVariantId,
                    Quantity = quantity,
                    CartToken = userId.ToString()
                });
            }
        }

        public async Task UpdateCartItemAsync(Guid userId, int cartItemId, int quantity)
        {
            var item = await _cartRepository.GetByIdAsync(cartItemId);
            if (item != null) item.Quantity = quantity;
        }

        public async Task RemoveCartItemAsync(Guid userId, int cartItemId)
        {
            await _cartRepository.RemoveCartItemAsync(cartItemId);

        }

        public async Task ClearCartAsync(Guid userId)
        {
            await _cartRepository.ClearCartAsync(userId.GetHashCode());
        }

        // int userId versiyonu
        public async Task<CartSummaryDto> GetCartAsync(int userId) => await GetCartAsync(Guid.NewGuid()); // örnek
        public async Task AddItemToCartAsync(int userId, CartItemDto item) => await AddToCartAsync(Guid.NewGuid(), item.ProductId, item.Quantity);
        public async Task RemoveItemFromCartAsync(int userId, int productId) => await RemoveCartItemAsync(Guid.NewGuid(), productId);
        public async Task ClearCartAsync(int userId) => await ClearCartAsync(Guid.NewGuid());

        public Task RemoveCartItemAsync(int cartItemId)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCartCountAsync()
        {
            throw new NotImplementedException();
        }
    }
}
