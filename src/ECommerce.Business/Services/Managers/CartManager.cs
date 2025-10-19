using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ECommerce.Business.Services.Interfaces; // ICartService
using ECommerce.Entities.Concrete;            // CartItem, Cart
using ECommerce.Core.Interfaces;              // ICartRepository
using ECommerce.Core.DTOs.Cart;


namespace ECommerce.Business.Services.Managers
{
    public class CartManager : ICartService
    {
        private readonly ICartRepository _cartRepository;

        public CartManager(ICartRepository cartRepository)
        {
            _cartRepository = cartRepository;
        }

        // ICartService uygulaması (int userId)
        public async Task<CartSummaryDto> GetCartAsync(int userId)
        {
            var items = await _cartRepository.GetByUserIdAsync(userId);
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

        public async Task AddItemToCartAsync(int userId, CartItemDto item)
        {
            var existing = await _cartRepository.GetByUserAndProductAsync(userId, item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                await _cartRepository.UpdateAsync(existing);
            }
            else
            {
                await _cartRepository.AddAsync(new CartItem
                {
                    UserId = userId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    CartToken = userId.ToString()
                });
            }
        }

        public async Task UpdateCartItemAsync(int userId, int cartItemId, int quantity)
        {
            var item = await _cartRepository.GetByIdAsync(cartItemId);
            if (item == null || item.UserId != userId) return;

            item.Quantity = quantity;
            await _cartRepository.UpdateAsync(item);
        }

        public async Task RemoveCartItemAsync(int userId, int cartItemId)
        {
            var item = await _cartRepository.GetByIdAsync(cartItemId);
            if (item == null || item.UserId != userId) return;

            await _cartRepository.RemoveCartItemAsync(cartItemId);
        }

        public async Task ClearCartAsync(int userId)
        {
            await _cartRepository.ClearCartAsync(userId);
        }

        public async Task<int> GetCartCountAsync()
        {
            return await _cartRepository.GetCartCountAsync();
        }

    }
}
