using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Entities.Concrete;  // CartItem, Cart


namespace ECommerce.Business.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSummaryDto> GetCartAsync(int userId);
        Task AddItemToCartAsync(int userId, CartItemDto item);
        Task UpdateCartItemAsync(int userId, int cartItemId, int quantity);
        Task RemoveCartItemAsync(int userId, int cartItemId);
        Task ClearCartAsync(int userId);
        
        Task<int> GetCartCountAsync(); // Yeni overload


    }
}
