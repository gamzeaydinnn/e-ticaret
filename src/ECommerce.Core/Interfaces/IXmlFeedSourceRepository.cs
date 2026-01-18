// IXmlFeedSourceRepository: XML feed kaynakları için repository arayüzü.
// Feed tanımları, senkronizasyon durumu ve zamanlama bilgilerini yönetir.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// XML feed kaynakları için repository arayüzü.
    /// Tedarikçi XML entegrasyonlarının yönetimi için kullanılır.
    /// </summary>
    public interface IXmlFeedSourceRepository : IRepository<XmlFeedSource>
    {
        #region Sorgular

        /// <summary>
        /// Feed kaynağını adına göre getirir.
        /// Ad benzersiz olduğu için tek sonuç.
        /// </summary>
        /// <param name="name">Feed adı</param>
        /// <returns>Feed kaynağı veya null</returns>
        Task<XmlFeedSource?> GetByNameAsync(string name);

        /// <summary>
        /// Aktif feed kaynaklarını getirir.
        /// </summary>
        Task<IEnumerable<XmlFeedSource>> GetActiveSourcesAsync();

        /// <summary>
        /// Otomatik senkronizasyon aktif olan kaynakları getirir.
        /// Background job için.
        /// </summary>
        Task<IEnumerable<XmlFeedSource>> GetAutoSyncEnabledSourcesAsync();

        /// <summary>
        /// Senkronizasyon zamanı gelmiş kaynakları getirir.
        /// NextSyncAt <= now olan kaynaklar.
        /// </summary>
        Task<IEnumerable<XmlFeedSource>> GetSourcesDueForSyncAsync();

        /// <summary>
        /// Tedarikçi adına göre kaynakları getirir.
        /// </summary>
        /// <param name="supplierName">Tedarikçi adı</param>
        Task<IEnumerable<XmlFeedSource>> GetBySupplierNameAsync(string supplierName);

        #endregion

        #region Senkronizasyon Durumu

        /// <summary>
        /// Senkronizasyon başarılı olarak tamamlandığında çağrılır.
        /// LastSyncAt, NextSyncAt, istatistikler güncellenir.
        /// </summary>
        /// <param name="sourceId">Feed kaynak ID</param>
        /// <param name="createdCount">Eklenen kayıt sayısı</param>
        /// <param name="updatedCount">Güncellenen kayıt sayısı</param>
        /// <param name="failedCount">Hatalı kayıt sayısı</param>
        Task UpdateSyncSuccessAsync(int sourceId, int createdCount, int updatedCount, int failedCount);

        /// <summary>
        /// Senkronizasyon hata ile sonuçlandığında çağrılır.
        /// </summary>
        /// <param name="sourceId">Feed kaynak ID</param>
        /// <param name="errorMessage">Hata mesajı</param>
        Task UpdateSyncFailureAsync(int sourceId, string errorMessage);

        /// <summary>
        /// Bir sonraki senkronizasyon zamanını hesaplar ve günceller.
        /// SyncIntervalMinutes'a göre NextSyncAt ayarlanır.
        /// </summary>
        /// <param name="sourceId">Feed kaynak ID</param>
        Task ScheduleNextSyncAsync(int sourceId);

        #endregion

        #region Benzersizlik Kontrolleri

        /// <summary>
        /// Feed adının benzersiz olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="name">Feed adı</param>
        /// <param name="excludeId">Hariç tutulacak ID (güncelleme için)</param>
        Task<bool> NameExistsAsync(string name, int? excludeId = null);

        /// <summary>
        /// Aynı URL'ye sahip aktif kaynak var mı kontrol eder.
        /// Mükerrer kaynak önleme için.
        /// </summary>
        /// <param name="url">Feed URL'si</param>
        /// <param name="excludeId">Hariç tutulacak ID</param>
        Task<bool> UrlExistsAsync(string url, int? excludeId = null);

        #endregion

        #region İstatistikler

        /// <summary>
        /// Aktif kaynak sayısını döndürür.
        /// </summary>
        Task<int> GetActiveSourceCountAsync();

        /// <summary>
        /// Toplam senkronizasyon sayısını döndürür (tüm kaynaklar).
        /// </summary>
        Task<int> GetTotalSyncCountAsync();

        /// <summary>
        /// Son 24 saatte başarısız senkronizasyon sayısını döndürür.
        /// Monitoring için.
        /// </summary>
        Task<int> GetRecentFailureCountAsync();

        #endregion
    }
}
