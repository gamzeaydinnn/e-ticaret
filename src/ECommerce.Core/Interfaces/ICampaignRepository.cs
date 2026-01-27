using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Kampanya repository interface'i.
    /// Kampanya ve kampanya hedefleri için veritabanı operasyonları.
    /// </summary>
    public interface ICampaignRepository : IRepository<Campaign>
    {
        /// <summary>
        /// Belirtilen tarihte aktif olan kampanyaları getirir
        /// </summary>
        Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null);
        
        /// <summary>
        /// Kampanyayı hedefleriyle birlikte getirir
        /// </summary>
        Task<Campaign?> GetByIdWithTargetsAsync(int id);
        
        /// <summary>
        /// Belirli bir ürün için geçerli kampanyaları getirir
        /// TargetType = All VEYA TargetType = Product ve TargetId = productId VEYA TargetType = Category ve TargetId = categoryId
        /// </summary>
        Task<List<Campaign>> GetCampaignsForProductAsync(int productId, int categoryId, DateTime? now = null);
        
        /// <summary>
        /// Belirli türdeki aktif kampanyaları getirir
        /// </summary>
        Task<List<Campaign>> GetActiveCampaignsByTypeAsync(CampaignType type, DateTime? now = null);
        
        /// <summary>
        /// Kampanya hedeflerini günceller
        /// Mevcut hedefleri siler ve yenilerini ekler
        /// </summary>
        Task UpdateTargetsAsync(int campaignId, IEnumerable<int> targetIds, CampaignTargetKind targetKind);
        
        /// <summary>
        /// Kampanya hedeflerini temizler
        /// </summary>
        Task ClearTargetsAsync(int campaignId);

        /// <summary>
        /// Tüm kampanyaları (silinen dahil) hedefleriyle birlikte getirir.
        /// Admin panelde silinenleri de görmek için kullanılır.
        /// </summary>
        Task<IEnumerable<Campaign>> GetAllIncludingDeletedAsync();

        /// <summary>
        /// ID'ye göre kampanya getirir (silinen dahil).
        /// Hard delete veya restore işlemleri için kullanılır.
        /// </summary>
        Task<Campaign?> GetByIdIncludingDeletedAsync(int id);
    }
}


