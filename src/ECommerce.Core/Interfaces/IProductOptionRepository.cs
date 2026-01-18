// IProductOptionRepository: Ürün seçenekleri için repository arayüzü.
// GetOrCreate pattern ile seçenek ve değer yönetimi sağlar.
// XML import'ta dinamik seçenek oluşturma için kritik.

using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Ürün seçenekleri (Option) ve değerleri (OptionValue) için repository arayüzü.
    /// GetOrCreate pattern kullanarak, olmayan seçenekleri otomatik oluşturur.
    /// </summary>
    public interface IProductOptionRepository : IRepository<ProductOption>
    {
        #region Option Sorguları

        /// <summary>
        /// Seçenek adına göre getirir.
        /// Ad benzersiz olduğu için tek sonuç.
        /// </summary>
        /// <param name="name">Seçenek adı (örn: "Hacim", "Renk")</param>
        /// <returns>Seçenek veya null</returns>
        Task<ProductOption?> GetByNameAsync(string name);

        /// <summary>
        /// Tüm seçenekleri değerleri ile birlikte getirir.
        /// Admin panelinde seçenek yönetimi için.
        /// </summary>
        /// <param name="includeInactive">Pasif seçenekleri dahil et</param>
        /// <returns>Seçenek listesi (değerler dahil)</returns>
        Task<IEnumerable<ProductOption>> GetAllWithValuesAsync(bool includeInactive = false);

        /// <summary>
        /// Seçeneği değerleri ile birlikte getirir.
        /// </summary>
        /// <param name="optionId">Seçenek ID</param>
        /// <returns>Seçenek veya null</returns>
        Task<ProductOption?> GetByIdWithValuesAsync(int optionId);

        #endregion

        #region GetOrCreate Pattern

        /// <summary>
        /// Seçeneği getirir, yoksa oluşturur.
        /// XML import'ta kritik - yeni seçenek türleri otomatik oluşturulur.
        /// Thread-safe olmalı.
        /// </summary>
        /// <param name="name">Seçenek adı</param>
        /// <returns>Mevcut veya yeni oluşturulan seçenek</returns>
        Task<ProductOption> GetOrCreateOptionAsync(string name);

        /// <summary>
        /// Seçenek değerini getirir, yoksa oluşturur.
        /// XML import'ta kritik - yeni değerler otomatik oluşturulur.
        /// </summary>
        /// <param name="optionId">Seçenek ID</param>
        /// <param name="value">Değer metni (örn: "330ml", "Kırmızı")</param>
        /// <returns>Mevcut veya yeni oluşturulan değer</returns>
        Task<ProductOptionValue> GetOrCreateValueAsync(int optionId, string value);

        /// <summary>
        /// Seçenek adı ve değer metni ile değeri getirir/oluşturur.
        /// Tek adımda hem seçenek hem değer oluşturma.
        /// </summary>
        /// <param name="optionName">Seçenek adı</param>
        /// <param name="value">Değer metni</param>
        /// <returns>Seçenek değeri</returns>
        Task<ProductOptionValue> GetOrCreateOptionValueAsync(string optionName, string value);

        #endregion

        #region OptionValue Sorguları

        /// <summary>
        /// Belirli bir seçeneğe ait tüm değerleri getirir.
        /// </summary>
        /// <param name="optionId">Seçenek ID</param>
        /// <param name="includeInactive">Pasif değerleri dahil et</param>
        /// <returns>Değer listesi</returns>
        Task<IEnumerable<ProductOptionValue>> GetValuesByOptionIdAsync(int optionId, bool includeInactive = false);

        /// <summary>
        /// Değer ID'si ile ProductOptionValue getirir.
        /// </summary>
        Task<ProductOptionValue?> GetValueByIdAsync(int valueId);

        /// <summary>
        /// Birden fazla ID ile değerleri getirir.
        /// Varyant oluşturmada seçilen değerleri doğrulamak için.
        /// </summary>
        /// <param name="valueIds">Değer ID'leri</param>
        /// <returns>Bulunan değerler</returns>
        Task<IEnumerable<ProductOptionValue>> GetValuesByIdsAsync(IEnumerable<int> valueIds);

        /// <summary>
        /// Değer metnine göre arama yapar.
        /// Autocomplete için.
        /// </summary>
        /// <param name="searchTerm">Arama terimi</param>
        /// <param name="optionId">Belirli bir seçenekte ara (opsiyonel)</param>
        /// <param name="limit">Maksimum sonuç</param>
        Task<IEnumerable<ProductOptionValue>> SearchValuesAsync(string searchTerm, int? optionId = null, int limit = 10);

        #endregion

        #region OptionValue CRUD

        /// <summary>
        /// Yeni değer ekler.
        /// </summary>
        Task<ProductOptionValue> AddValueAsync(ProductOptionValue value);

        /// <summary>
        /// Değeri günceller.
        /// </summary>
        Task UpdateValueAsync(ProductOptionValue value);

        /// <summary>
        /// Değeri siler (soft delete).
        /// </summary>
        Task DeleteValueAsync(int valueId);

        #endregion

        #region VariantOptionValue İşlemleri

        /// <summary>
        /// Varyanta seçenek değeri atar.
        /// </summary>
        /// <param name="variantId">Varyant ID</param>
        /// <param name="optionValueId">Seçenek değer ID</param>
        Task AssignOptionValueToVariantAsync(int variantId, int optionValueId);

        /// <summary>
        /// Varyanta birden fazla seçenek değeri atar.
        /// Mevcut atamaları temizleyip yenileriyle değiştirir.
        /// </summary>
        /// <param name="variantId">Varyant ID</param>
        /// <param name="optionValueIds">Seçenek değer ID'leri</param>
        Task SetVariantOptionValuesAsync(int variantId, IEnumerable<int> optionValueIds);

        /// <summary>
        /// Varyanttan seçenek değerini kaldırır.
        /// </summary>
        Task RemoveOptionValueFromVariantAsync(int variantId, int optionValueId);

        /// <summary>
        /// Varyantın tüm seçenek değerlerini getirir.
        /// </summary>
        Task<IEnumerable<ProductOptionValue>> GetVariantOptionValuesAsync(int variantId);

        #endregion

        #region İstatistikler

        /// <summary>
        /// Seçenek sayısını döndürür.
        /// </summary>
        Task<int> GetOptionCountAsync(bool includeInactive = false);

        /// <summary>
        /// Belirli bir seçeneğe ait değer sayısını döndürür.
        /// </summary>
        Task<int> GetValueCountByOptionIdAsync(int optionId, bool includeInactive = false);

        /// <summary>
        /// Bir değerin kaç varyantta kullanıldığını döndürür.
        /// Silme öncesi referans kontrolü için.
        /// </summary>
        Task<int> GetValueUsageCountAsync(int optionValueId);

        #endregion
    }
}
