using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Kampanya yönetimi servisi.
    /// CRUD operasyonları ve kampanya hesaplama işlemlerini yönetir.
    /// </summary>
    public interface ICampaignService
    {
        #region CRUD Operasyonları

        /// <summary>
        /// Tüm kampanyaları getirir (aktif/pasif fark etmez)
        /// </summary>
        Task<List<Campaign>> GetAllAsync();
        
        /// <summary>
        /// ID'ye göre kampanya getirir (hedefleri dahil)
        /// </summary>
        Task<Campaign?> GetByIdAsync(int id);
        
        /// <summary>
        /// Yeni kampanya oluşturur
        /// </summary>
        Task<Campaign> CreateAsync(CampaignSaveDto dto);
        
        /// <summary>
        /// Mevcut kampanyayı günceller
        /// </summary>
        Task<Campaign> UpdateAsync(int id, CampaignSaveDto dto);
        
        /// <summary>
        /// Kampanyayı siler
        /// </summary>
        Task<bool> DeleteAsync(int id);
        
        /// <summary>
        /// Kampanyayı aktif/pasif yapar
        /// </summary>
        Task<bool> ToggleActiveAsync(int id);

        #endregion

        #region Aktif Kampanya Sorgulama

        /// <summary>
        /// Belirtilen tarihte aktif olan kampanyaları getirir
        /// Tarih verilmezse şu anki tarih kullanılır
        /// </summary>
        Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null);
        
        /// <summary>
        /// Belirli bir ürün için geçerli kampanyaları getirir
        /// Hem ürün bazlı hem kategori bazlı hem de "tüm ürünler" kampanyalarını kontrol eder
        /// </summary>
        Task<List<Campaign>> GetApplicableCampaignsForProductAsync(int productId, int categoryId);
        
        /// <summary>
        /// Ücretsiz kargo kampanyası var mı kontrol eder
        /// </summary>
        Task<Campaign?> GetFreeShippingCampaignAsync(decimal cartTotal);

        #endregion

        #region Kampanya Hesaplama

        /// <summary>
        /// Sepet için tüm kampanya indirimlerini hesaplar.
        /// Her satır için en iyi kampanyayı seçer (en yüksek indirim).
        /// X Al Y Öde kampanyalarını da hesaplar.
        /// </summary>
        /// <param name="items">Sepet satırları</param>
        /// <param name="cartTotal">Sepet toplam tutarı</param>
        /// <returns>Kampanya hesaplama sonucu</returns>
        Task<CampaignCalculationResult> CalculateCampaignDiscountsAsync(
            IEnumerable<CartItemForCampaign> items, 
            decimal cartTotal);
        
        /// <summary>
        /// Tek bir satır için en iyi kampanyayı ve indirim tutarını hesaplar
        /// </summary>
        Task<(Campaign? Campaign, decimal Discount)> CalculateBestCampaignForItemAsync(
            int productId, 
            int categoryId, 
            decimal unitPrice, 
            int quantity);
        
        /// <summary>
        /// X Al Y Öde kampanyası için indirim hesaplar
        /// Kapsamdaki en ucuz ürünleri bedava yapar
        /// </summary>
        Task<decimal> CalculateBuyXPayYDiscountAsync(
            Campaign campaign,
            IEnumerable<CartItemForCampaign> scopedItems);

        #endregion
    }
}

