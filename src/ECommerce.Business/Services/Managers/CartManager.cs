using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Cart;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class CartManager : ICartService
    {
        public Task<CartSummaryDto> GetCartAsync(int userId)
        {
            // Örnek: boş bir cart döndür
            return Task.FromResult(new CartSummaryDto());
        }

        public Task AddItemToCartAsync(int userId, CartItemDto item)
        {
            // Örnek: simülasyon
            return Task.CompletedTask;
        }

        public Task RemoveItemFromCartAsync(int userId, int productId)
        {
            return Task.CompletedTask;
        }

        public Task ClearCartAsync(int userId)
        {
            return Task.CompletedTask;
        }
    }
}
