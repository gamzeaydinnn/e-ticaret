using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ağırlık fark yönetimi repository interface'i
    /// WeightAdjustment entity'si için veri erişim operasyonlarını tanımlar
    /// </summary>
    public interface IWeightAdjustmentRepository : IRepository<WeightAdjustment>
    {
        #region Temel Sorgular

        /// <summary>
        /// Sipariş ID'sine göre tüm ağırlık fark kayıtlarını getirir
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Ağırlık fark kayıtları listesi</returns>
        Task<IEnumerable<WeightAdjustment>> GetByOrderIdAsync(int orderId);

        /// <summary>
        /// Sipariş kalemi ID'sine göre ağırlık fark kaydını getirir
        /// Her sipariş kalemi için tek bir kayıt olmalı
        /// </summary>
        /// <param name="orderItemId">Sipariş kalemi ID</param>
        /// <returns>Ağırlık fark kaydı veya null</returns>
        Task<WeightAdjustment?> GetByOrderItemIdAsync(int orderItemId);

        /// <summary>
        /// Kurye ID'sine göre tartı kayıtlarını getirir
        /// Kurye performans raporları için kullanılır
        /// </summary>
        /// <param name="courierId">Kurye ID</param>
        /// <returns>Ağırlık fark kayıtları listesi</returns>
        Task<IEnumerable<WeightAdjustment>> GetByCourierIdAsync(int courierId);

        #endregion

        #region Durum Bazlı Sorgular

        /// <summary>
        /// Belirli bir duruma sahip kayıtları getirir
        /// </summary>
        /// <param name="status">Fark durumu</param>
        /// <returns>Ağırlık fark kayıtları listesi</returns>
        Task<IEnumerable<WeightAdjustment>> GetByStatusAsync(WeightAdjustmentStatus status);

        /// <summary>
        /// Admin onayı bekleyen kayıtları getirir
        /// Admin panel için kritik - hızlı erişim gerekli
        /// </summary>
        /// <returns>Onay bekleyen kayıtlar</returns>
        Task<IEnumerable<WeightAdjustment>> GetPendingAdminApprovalAsync();

        /// <summary>
        /// Ödeme/iade bekleyen kayıtları getirir
        /// Otomatik işlem için kullanılır
        /// </summary>
        /// <returns>Ödeme bekleyen kayıtlar</returns>
        Task<IEnumerable<WeightAdjustment>> GetPendingSettlementAsync();

        /// <summary>
        /// Tartı bekleyen sipariş kalemlerini getirir
        /// Kurye paneli için kullanılır
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Tartı bekleyen kayıtlar</returns>
        Task<IEnumerable<WeightAdjustment>> GetPendingWeighingByOrderIdAsync(int orderId);

        #endregion

        #region Filtreleme ve Sayfalama

        /// <summary>
        /// Filtreleme kriterleriyle sayfalanmış liste döner
        /// Admin panel listeleme için kullanılır
        /// </summary>
        /// <param name="filter">Filtre kriterleri</param>
        /// <returns>Sayfalanmış sonuç</returns>
        Task<PagedResult<WeightAdjustment>> GetFilteredAsync(WeightAdjustmentFilterDto filter);

        #endregion

        #region İstatistikler

        /// <summary>
        /// Dashboard için istatistikleri hesaplar
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi (opsiyonel)</param>
        /// <param name="endDate">Bitiş tarihi (opsiyonel)</param>
        /// <returns>İstatistik verisi</returns>
        Task<WeightAdjustmentStatsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Belirli tarih aralığındaki toplam fark tutarını hesaplar
        /// </summary>
        /// <param name="startDate">Başlangıç tarihi</param>
        /// <param name="endDate">Bitiş tarihi</param>
        /// <returns>Toplam fark tutarı</returns>
        Task<decimal> GetTotalDifferenceAmountAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Sipariş için toplam fark tutarını hesaplar
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Toplam fark tutarı</returns>
        Task<decimal> GetOrderTotalDifferenceAsync(int orderId);

        #endregion

        #region Toplu İşlemler

        /// <summary>
        /// Toplu kayıt ekleme
        /// Sipariş oluşturulduğunda tüm ağırlık bazlı ürünler için kayıt oluşturulur
        /// </summary>
        /// <param name="adjustments">Eklenecek kayıtlar</param>
        Task AddRangeAsync(IEnumerable<WeightAdjustment> adjustments);

        /// <summary>
        /// Toplu durum güncelleme
        /// Sipariş tamamlandığında tüm kayıtlar güncellenir
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <param name="status">Yeni durum</param>
        Task UpdateStatusByOrderIdAsync(int orderId, WeightAdjustmentStatus status);

        #endregion

        #region Doğrulama

        /// <summary>
        /// Siparişin tüm ağırlık bazlı ürünlerinin tartılıp tartılmadığını kontrol eder
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Tümü tartıldıysa true</returns>
        Task<bool> AreAllItemsWeighedAsync(int orderId);

        /// <summary>
        /// Siparişte admin onayı bekleyen kayıt var mı kontrol eder
        /// </summary>
        /// <param name="orderId">Sipariş ID</param>
        /// <returns>Onay bekleyen varsa true</returns>
        Task<bool> HasPendingAdminApprovalAsync(int orderId);

        #endregion
    }
}
