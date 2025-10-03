using ECommerce.Core.DTOs.Cart;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSummaryDto> GetCartAsync(int userId);
        Task AddItemToCartAsync(int userId, CartItemDto item);
        Task RemoveItemFromCartAsync(int userId, int productId);
        Task ClearCartAsync(int userId);
        Task RemoveCartItemAsync(int cartItemId);
        //
        Task<CartSummaryDto> GetCartAsync(Guid userId);
        Task<int> GetCartCountAsync();
        Task AddToCartAsync(Guid userId, int productVariantId, int quantity);
        Task UpdateCartItemAsync(Guid userId, int cartItemId, int quantity);
        Task RemoveCartItemAsync(Guid userId, int cartItemId);
        Task ClearCartAsync(Guid userId);

    }
}
