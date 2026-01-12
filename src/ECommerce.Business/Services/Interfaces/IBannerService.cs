using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Banner/Poster yönetim servisi interface'i
    /// Ana sayfa slider, promo kartları ve genel banner'lar için kullanılır
    /// </summary>
    public interface IBannerService
    {
        /// <summary>
        /// Tüm banner'ları DisplayOrder'a göre sıralı getirir
        /// </summary>
        Task<IEnumerable<BannerDto>> GetAllAsync();
        
        /// <summary>
        /// ID'ye göre tek bir banner getirir
        /// </summary>
        Task<BannerDto?> GetByIdAsync(int id);
        
        /// <summary>
        /// Tipe göre banner'ları getirir (slider, promo, banner)
        /// Sadece aktif olanları döndürür
        /// </summary>
        Task<IEnumerable<BannerDto>> GetByTypeAsync(string type);
        
        /// <summary>
        /// Sadece aktif banner'ları getirir
        /// </summary>
        Task<IEnumerable<BannerDto>> GetActiveAsync();
        
        /// <summary>
        /// Yeni banner ekler
        /// </summary>
        Task AddAsync(BannerDto dto);
        
        /// <summary>
        /// Mevcut banner'ı günceller
        /// </summary>
        Task UpdateAsync(BannerDto dto);
        
        /// <summary>
        /// Banner'ı siler
        /// </summary>
        Task DeleteAsync(int id);
    }
}