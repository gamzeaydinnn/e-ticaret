// =============================================================================
// ICouponService - Kupon Servisi Arayüzü
// =============================================================================
// Bu interface, kupon yönetimi için gerekli tüm operasyonları tanımlar.
// SOLID - Interface Segregation: Sadece kupon işlemleri için metodlar içerir.
// Dependency Injection ile mock'lanabilir ve test edilebilir.
// =============================================================================

using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Coupon;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kupon yönetimi servisi arayüzü.
    /// CRUD operasyonları ve kupon doğrulama/uygulama işlemlerini tanımlar.
    /// </summary>
    public interface ICouponService
    {
        // =============================================================================
        // CRUD Operasyonları
        // =============================================================================

        /// <summary>
        /// Tüm kuponları getirir (admin için)
        /// </summary>
        Task<IEnumerable<Coupon>> GetAllAsync();

        /// <summary>
        /// ID'ye göre kupon getirir
        /// </summary>
        Task<Coupon?> GetByIdAsync(int id);

        /// <summary>
        /// Yeni kupon ekler
        /// </summary>
        Task AddAsync(Coupon coupon);

        /// <summary>
        /// Kuponu günceller
        /// </summary>
        Task UpdateAsync(Coupon coupon);

        /// <summary>
        /// Kuponu siler (soft delete)
        /// </summary>
        Task DeleteAsync(int id);

        // =============================================================================
        // Kupon Kodu İşlemleri
        // =============================================================================

        /// <summary>
        /// Kupon koduna göre kupon getirir
        /// Case-insensitive arama yapar
        /// </summary>
        /// <param name="code">Kupon kodu</param>
        /// <returns>Bulunan kupon veya null</returns>
        Task<Coupon?> GetByCodeAsync(string code);

        /// <summary>
        /// Kupon kodunun geçerli olup olmadığını basitçe kontrol eder
        /// Sadece aktiflik ve tarih kontrolü yapar
        /// </summary>
        /// <param name="code">Kupon kodu</param>
        /// <returns>Geçerli ise true</returns>
        Task<bool> ValidateCouponAsync(string code);

        // =============================================================================
        // Gelişmiş Kupon Doğrulama ve Uygulama
        // =============================================================================

        /// <summary>
        /// Kuponu tam kapsamlı doğrular ve indirim tutarını hesaplar.
        /// Kontrol edilen durumlar:
        /// - Kupon aktif mi ve süresi dolmamış mı
        /// - Kullanım limiti aşılmış mı
        /// - Kullanıcı bazlı limit kontrolü
        /// - Minimum sipariş tutarı sağlanıyor mu
        /// - Kategori/ürün bazlı kısıtlamalar
        /// - İlk sipariş kontrolü (FirstOrder türü için)
        /// </summary>
        /// <param name="code">Kupon kodu</param>
        /// <param name="userId">Kullanıcı ID (guest ise null)</param>
        /// <param name="request">Sepet bilgileri</param>
        /// <returns>Doğrulama sonucu ve hesaplanan indirim</returns>
        Task<CouponValidationResult> ValidateAndCalculateAsync(
            string code,
            int? userId,
            CouponValidateRequestDto request);

        /// <summary>
        /// Kupon kullanım sayısını artırır ve kullanım kaydı oluşturur.
        /// Sipariş tamamlandığında çağrılmalıdır.
        /// </summary>
        /// <param name="usageDto">Kullanım bilgileri</param>
        /// <returns>Başarılı ise true</returns>
        Task<bool> IncrementUsageAsync(CouponUsageCreateDto usageDto);

        /// <summary>
        /// Kullanıcının belirli bir kuponu kullanıp kullanamayacağını kontrol eder.
        /// Kullanıcı bazlı limit ve önceki kullanımları kontrol eder.
        /// </summary>
        /// <param name="couponId">Kupon ID</param>
        /// <param name="userId">Kullanıcı ID (guest ise null)</param>
        /// <returns>Kullanabilir ise true</returns>
        Task<bool> CanUserUseCouponAsync(int couponId, int? userId);

        /// <summary>
        /// Kullanıcının bu kuponu kaç kez kullandığını döndürür
        /// </summary>
        /// <param name="couponId">Kupon ID</param>
        /// <param name="userId">Kullanıcı ID</param>
        /// <returns>Kullanım sayısı</returns>
        Task<int> GetUserUsageCountAsync(int couponId, int? userId);

        // =============================================================================
        // Listeleme ve Filtreleme
        // =============================================================================

        /// <summary>
        /// Aktif kuponları getirir (süresi dolmamış ve aktif olanlar)
        /// </summary>
        Task<IEnumerable<CouponSummaryDto>> GetActiveCouponsAsync();

        /// <summary>
        /// Belirli bir kategoriye ait kuponları getirir
        /// </summary>
        Task<IEnumerable<CouponSummaryDto>> GetCouponsByCategoryAsync(int categoryId);

        /// <summary>
        /// Belirli bir ürüne uygulanabilir kuponları getirir
        /// </summary>
        Task<IEnumerable<CouponSummaryDto>> GetCouponsForProductAsync(int productId);

        /// <summary>
        /// Kupon detayını DTO olarak getirir
        /// </summary>
        Task<CouponDetailDto?> GetCouponDetailAsync(int id);

        // =============================================================================
        // İlk Sipariş Kontrolü
        // =============================================================================

        /// <summary>
        /// Kullanıcının ilk siparişi olup olmadığını kontrol eder
        /// FirstOrder türü kuponlar için kullanılır
        /// </summary>
        /// <param name="userId">Kullanıcı ID (guest ise null - false döner)</param>
        /// <returns>İlk sipariş ise true</returns>
        Task<bool> IsFirstOrderAsync(int? userId);
    }
}
