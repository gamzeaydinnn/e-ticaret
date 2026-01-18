using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.ProductOption;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Product Option servisi - Renk, Beden, Materyal gibi seçeneklerin yönetimi.
    /// GetOrCreate pattern ile option ve value'ların otomatik oluşturulmasını sağlar.
    /// </summary>
    public interface IProductOptionService
    {
        #region Option Management
        
        /// <summary>
        /// Tüm option'ları listeler (admin panel için).
        /// </summary>
        Task<IEnumerable<ProductOptionDto>> GetAllOptionsAsync();
        
        /// <summary>
        /// ID'ye göre option getirir.
        /// </summary>
        Task<ProductOptionDto?> GetOptionByIdAsync(int optionId);
        
        /// <summary>
        /// Yeni option oluşturur veya mevcut olanı getirir.
        /// </summary>
        Task<ProductOptionDto> GetOrCreateOptionAsync(string name);
        
        /// <summary>
        /// Option'ı günceller.
        /// </summary>
        Task<ProductOptionDto> UpdateOptionAsync(int optionId, ProductOptionCreateDto dto);
        
        /// <summary>
        /// Option'ı siler (kullanımda değilse).
        /// </summary>
        Task<bool> DeleteOptionAsync(int optionId);
        
        #endregion
        
        #region Option Value Management
        
        /// <summary>
        /// Bir option'ın tüm value'larını listeler.
        /// </summary>
        Task<IEnumerable<ProductOptionValueDto>> GetValuesByOptionIdAsync(int optionId);
        
        /// <summary>
        /// Yeni value oluşturur veya mevcut olanı getirir.
        /// </summary>
        Task<ProductOptionValueDto> GetOrCreateValueAsync(int optionId, string value);
        
        /// <summary>
        /// Aynı anda birden fazla value oluşturur veya getirir.
        /// </summary>
        Task<IEnumerable<ProductOptionValueDto>> GetOrCreateValuesAsync(int optionId, IEnumerable<string> values);
        
        /// <summary>
        /// Value'yu günceller.
        /// </summary>
        Task<ProductOptionValueDto> UpdateValueAsync(int valueId, string newValue);
        
        /// <summary>
        /// Value'yu siler (kullanımda değilse).
        /// </summary>
        Task<bool> DeleteValueAsync(int valueId);
        
        #endregion
        
        #region Product-Specific Operations
        
        /// <summary>
        /// Bir ürün için kullanılan tüm option'ları listeler.
        /// </summary>
        Task<IEnumerable<ProductOptionDto>> GetOptionsForProductAsync(int productId);
        
        /// <summary>
        /// Belirli bir kategoride kullanılan option'ları listeler (öneriler için).
        /// </summary>
        Task<IEnumerable<ProductOptionDto>> GetOptionsForCategoryAsync(int categoryId);
        
        /// <summary>
        /// En çok kullanılan option'ları listeler.
        /// </summary>
        Task<IEnumerable<ProductOptionDto>> GetMostUsedOptionsAsync(int limit = 10);
        
        #endregion
        
        #region Batch Operations (XML Import için)
        
        /// <summary>
        /// Batch olarak option ve value'ları oluşturur.
        /// XML import sırasında performans için kullanılır.
        /// </summary>
        Task<IDictionary<string, IDictionary<string, int>>> BatchGetOrCreateAsync(
            IDictionary<string, IEnumerable<string>> optionValueMap);
        
        #endregion
    }
}
