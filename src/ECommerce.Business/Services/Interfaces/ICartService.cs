using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Sepet servisi interface'i
    /// Hem kayıtlı kullanıcı (UserId) hem misafir kullanıcı (CartToken) destekler
    /// </summary>
    public interface ICartService
    {
        #region Kayıtlı Kullanıcı Metodları (UserId bazlı)

        /// <summary>
        /// Kayıtlı kullanıcının sepetini getirir
        /// </summary>
        Task<CartSummaryDto> GetCartAsync(int userId);

        /// <summary>
        /// Kayıtlı kullanıcının sepetine ürün ekler
        /// </summary>
        Task AddItemToCartAsync(int userId, CartItemDto item);

        /// <summary>
        /// Kayıtlı kullanıcının sepet öğesinin miktarını günceller
        /// </summary>
        Task UpdateCartItemAsync(int userId, int cartItemId, int quantity);

        /// <summary>
        /// Kayıtlı kullanıcının sepetinden öğe siler
        /// </summary>
        Task RemoveCartItemAsync(int userId, int cartItemId);

        /// <summary>
        /// Kayıtlı kullanıcının sepetini temizler
        /// </summary>
        Task ClearCartAsync(int userId);

        #endregion

        #region Misafir Kullanıcı Metodları (CartToken bazlı)

        /// <summary>
        /// Misafir kullanıcının sepetini CartToken ile getirir
        /// </summary>
        Task<CartSummaryDto> GetCartByTokenAsync(string cartToken);

        /// <summary>
        /// Misafir kullanıcının sepetine ürün ekler
        /// </summary>
        Task<CartItemDto> AddItemToCartByTokenAsync(string cartToken, CartItemDto item);

        /// <summary>
        /// Misafir kullanıcının sepet öğesinin miktarını günceller
        /// </summary>
        Task UpdateCartItemByTokenAsync(string cartToken, int productId, int quantity, int? variantId = null);

        /// <summary>
        /// Misafir kullanıcının sepetinden öğe siler
        /// </summary>
        Task RemoveCartItemByTokenAsync(string cartToken, int productId, int? variantId = null);

        /// <summary>
        /// Misafir kullanıcının sepetini temizler
        /// </summary>
        Task ClearCartByTokenAsync(string cartToken);

        #endregion

        #region Merge Operasyonları

        /// <summary>
        /// Misafir sepetini kayıtlı kullanıcıya transfer eder (login sonrası)
        /// </summary>
        Task<int> MergeGuestCartToUserAsync(string cartToken, int userId);

        #endregion

        #region Ortak Metodlar

        /// <summary>
        /// Toplam sepet öğesi sayısı (admin için)
        /// </summary>
        Task<int> GetCartCountAsync();

        #endregion
    }
}
