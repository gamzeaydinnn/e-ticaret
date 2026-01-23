using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Sepet repository interface'i
    /// Hem kayıtlı kullanıcı (UserId) hem misafir kullanıcı (CartToken) destekler
    /// </summary>
    public interface ICartRepository : IRepository<CartItem>
    {
        #region Kayıtlı Kullanıcı Metodları (UserId bazlı)

        /// <summary>
        /// Kayıtlı kullanıcının sepet öğelerini getirir
        /// </summary>
        Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Kullanıcı ve ürün ID'sine göre sepet öğesi bulur
        /// </summary>
        Task<CartItem?> GetByUserAndProductAsync(int userId, int productId);

        /// <summary>
        /// Kullanıcı ve ürün ID'sine göre sepet öğesini siler
        /// </summary>
        Task RemoveByUserAndProductAsync(int userId, int productId);

        /// <summary>
        /// Kayıtlı kullanıcının sepetini tamamen temizler
        /// </summary>
        Task ClearCartAsync(int userId);

        #endregion

        #region Misafir Kullanıcı Metodları (CartToken bazlı)

        /// <summary>
        /// Misafir kullanıcının sepet öğelerini CartToken ile getirir
        /// </summary>
        /// <param name="cartToken">Misafir kullanıcı benzersiz token'ı (UUID)</param>
        Task<IEnumerable<CartItem>> GetByCartTokenAsync(string cartToken);

        /// <summary>
        /// Misafir kullanıcının belirli bir ürününü bulur
        /// Varyant desteği için productVariantId parametresi eklendi
        /// </summary>
        Task<CartItem?> GetByTokenAndProductAsync(string cartToken, int productId, int? productVariantId = null);

        /// <summary>
        /// Misafir kullanıcının sepetini tamamen temizler
        /// </summary>
        Task ClearCartByTokenAsync(string cartToken);

        #endregion

        #region Merge Operasyonları (Guest → User Transferi)

        /// <summary>
        /// Misafir sepetini kayıtlı kullanıcıya transfer eder
        /// Login sonrası çağrılır - çakışan ürünlerde miktarlar toplanır
        /// </summary>
        /// <param name="cartToken">Misafir token</param>
        /// <param name="userId">Hedef kullanıcı ID</param>
        /// <returns>Transfer edilen öğe sayısı</returns>
        Task<int> MergeGuestCartToUserAsync(string cartToken, int userId);

        #endregion

        #region Ortak Metodlar

        /// <summary>
        /// Sepet öğesini ID ile siler
        /// </summary>
        Task RemoveCartItemAsync(int cartItemId);

        /// <summary>
        /// Toplam sepet öğesi sayısını döner (admin istatistik için)
        /// </summary>
        Task<int> GetCartCountAsync();

        #endregion
    }
}