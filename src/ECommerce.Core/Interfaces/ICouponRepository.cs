// =============================================================================
// ICouponRepository - Kupon Repository Arayüzü
// =============================================================================
// Bu interface, kupon ve kupon kullanım verilerine erişim için metodları tanımlar.
// Repository Pattern ile veri erişim katmanını soyutlar.
// =============================================================================

using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Kupon repository arayüzü.
    /// Kupon ve CouponUsage veritabanı işlemlerini tanımlar.
    /// </summary>
    public interface ICouponRepository : IRepository<Coupon>
    {
        // =============================================================================
        // Kupon Sorgulama
        // =============================================================================

        /// <summary>
        /// Kupon koduna göre kupon getirir (case-insensitive)
        /// </summary>
        /// <param name="code">Kupon kodu</param>
        /// <returns>Bulunan kupon veya null</returns>
        Task<Coupon?> GetByCodeAsync(string code);

        /// <summary>
        /// Kupon koduna göre kupon getirir, ilişkili verileri de yükler
        /// (Category, CouponProducts, CouponUsages)
        /// </summary>
        /// <param name="code">Kupon kodu</param>
        /// <returns>Bulunan kupon veya null</returns>
        Task<Coupon?> GetByCodeWithDetailsAsync(string code);

        /// <summary>
        /// Aktif kuponları getirir (IsActive = true ve süresi dolmamış)
        /// </summary>
        Task<IEnumerable<Coupon>> GetActiveCouponsAsync();

        /// <summary>
        /// Belirli kategoriye ait kuponları getirir
        /// </summary>
        Task<IEnumerable<Coupon>> GetByCategoryIdAsync(int categoryId);

        /// <summary>
        /// Belirli ürüne uygulanabilir kuponları getirir
        /// </summary>
        Task<IEnumerable<Coupon>> GetByProductIdAsync(int productId);

        // =============================================================================
        // Kupon Kullanım İşlemleri
        // =============================================================================

        /// <summary>
        /// Kupon kullanım kaydı ekler
        /// </summary>
        /// <param name="usage">Kullanım kaydı</param>
        Task AddCouponUsageAsync(CouponUsage usage);

        /// <summary>
        /// Kullanıcının belirli kuponu kaç kez kullandığını döndürür
        /// </summary>
        /// <param name="couponId">Kupon ID</param>
        /// <param name="userId">Kullanıcı ID (null ise 0 döner)</param>
        /// <returns>Kullanım sayısı</returns>
        Task<int> GetUserUsageCountAsync(int couponId, int? userId);

        /// <summary>
        /// Kuponun toplam kullanım sayısını döndürür
        /// </summary>
        /// <param name="couponId">Kupon ID</param>
        /// <returns>Toplam kullanım sayısı</returns>
        Task<int> GetTotalUsageCountAsync(int couponId);

        /// <summary>
        /// Belirli siparişte kullanılan kupon kullanım kaydını getirir
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Kullanım kaydı veya null</returns>
        Task<CouponUsage?> GetUsageByOrderIdAsync(int orderId);

        /// <summary>
        /// Kupon kullanım geçmişini getirir (admin raporlama için)
        /// </summary>
        /// <param name="couponId">Kupon ID</param>
        /// <param name="take">Alınacak kayıt sayısı</param>
        /// <returns>Kullanım kayıtları</returns>
        Task<IEnumerable<CouponUsage>> GetUsageHistoryAsync(int couponId, int take = 50);
    }
}
