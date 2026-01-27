using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kampanya Uygulama Motoru - Ürünlere otomatik indirim uygulama servisi.
    /// 
    /// Bu servis şu işlemleri yapar:
    /// 1. Kampanya oluşturulduğunda/güncellendiğinde hedef ürünlerin SpecialPrice'ını hesaplar
    /// 2. Kampanya silindiğinde/pasif yapıldığında SpecialPrice'ı yeniden hesaplar
    /// 3. Birden fazla kampanya varsa EN YÜKSEK İNDİRİMİ uygular
    /// 
    /// İndirim Hesaplama Formülleri:
    /// - Percentage: SpecialPrice = Price * (1 - DiscountValue/100)
    /// - FixedAmount: SpecialPrice = Price - DiscountValue
    /// - BuyXPayY: Sepette uygulanır, SpecialPrice hesaplanmaz
    /// - FreeShipping: Kargo için, SpecialPrice hesaplanmaz
    /// </summary>
    public class CampaignApplicationService : ICampaignApplicationService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<CampaignApplicationService>? _logger;

        public CampaignApplicationService(
            ICampaignRepository campaignRepository,
            IProductRepository productRepository,
            ILogger<CampaignApplicationService>? logger = null)
        {
            _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _logger = logger;
        }

        #region Public Methods

        /// <summary>
        /// Belirli bir kampanyanın indirimlerini hedef ürünlere uygular.
        /// </summary>
        public async Task<int> ApplyCampaignToProductsAsync(int campaignId)
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign == null)
            {
                _logger?.LogWarning("Kampanya bulunamadı: {CampaignId}", campaignId);
                return 0;
            }

            _logger?.LogInformation(
                "Kampanya kontrol ediliyor: ID={CampaignId}, Ad={Name}, Tür={Type}, Hedef={TargetType}, " +
                "İndirim={DiscountValue}, Aktif={IsActive}, Başlangıç={StartDate}, Bitiş={EndDate}, " +
                "Hedef Sayısı={TargetCount}, ŞuAn={Now}",
                campaignId, campaign.Name, campaign.Type, campaign.TargetType, 
                campaign.DiscountValue, campaign.IsActive, campaign.StartDate, campaign.EndDate,
                campaign.Targets?.Count ?? 0, DateTime.UtcNow);

            // Kampanya aktif ve geçerli değilse işlem yapma
            if (!campaign.IsCurrentlyValid())
            {
                _logger?.LogWarning(
                    "Kampanya aktif değil veya tarihi geçmiş: {CampaignId}, IsActive={IsActive}, " +
                    "StartDate={StartDate}, EndDate={EndDate}, Now={Now}",
                    campaignId, campaign.IsActive, campaign.StartDate, campaign.EndDate, DateTime.UtcNow);
                return 0;
            }

            // Sadece Percentage ve FixedAmount kampanyaları SpecialPrice günceller
            if (campaign.Type != CampaignType.Percentage && campaign.Type != CampaignType.FixedAmount)
            {
                _logger?.LogInformation(
                    "Kampanya türü ({Type}) SpecialPrice güncellemesi gerektirmiyor: {CampaignId}",
                    campaign.Type, campaignId);
                return 0;
            }

            // Hedef ürünleri bul
            var products = await GetTargetProductsAsync(campaign);
            if (!products.Any())
            {
                _logger?.LogInformation("Kampanya için hedef ürün bulunamadı: {CampaignId}", campaignId);
                return 0;
            }

            int updatedCount = 0;

            foreach (var product in products)
            {
                // En iyi indirimi hesapla (birden fazla kampanya olabilir)
                var (specialPrice, appliedCampaign) = await CalculateBestPriceAsync(
                    product.Id, product.CategoryId, product.Price);

                // SpecialPrice değişmişse güncelle
                if (product.SpecialPrice != specialPrice)
                {
                    product.SpecialPrice = specialPrice;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    updatedCount++;

                    _logger?.LogDebug(
                        "Ürün SpecialPrice güncellendi: {ProductId}, Fiyat: {Price}, İndirimli: {SpecialPrice}, Kampanya: {CampaignName}",
                        product.Id, product.Price, specialPrice, appliedCampaign?.Name ?? "Yok");
                }
            }

            _logger?.LogInformation(
                "Kampanya uygulandı: {CampaignName} ({CampaignId}), Güncellenen ürün: {UpdatedCount}",
                campaign.Name, campaignId, updatedCount);

            return updatedCount;
        }

        /// <summary>
        /// Kampanya silindiğinde veya pasif yapıldığında ürünlerin SpecialPrice'ını yeniden hesaplar.
        /// </summary>
        public async Task<int> RemoveCampaignFromProductsAsync(int campaignId)
        {
            // Silinmiş dahil kampanyayı getir (hedefleri bulmak için)
            var campaign = await _campaignRepository.GetByIdIncludingDeletedAsync(campaignId);
            if (campaign == null)
            {
                _logger?.LogWarning("Kampanya bulunamadı (silinmiş dahil): {CampaignId}", campaignId);
                return 0;
            }

            // Hedef ürünleri bul
            var products = await GetTargetProductsAsync(campaign);
            if (!products.Any())
            {
                return 0;
            }

            int updatedCount = 0;

            foreach (var product in products)
            {
                // Diğer aktif kampanyalardan en iyi indirimi hesapla
                var (specialPrice, _) = await CalculateBestPriceAsync(
                    product.Id, product.CategoryId, product.Price);

                // SpecialPrice değişmişse güncelle
                if (product.SpecialPrice != specialPrice)
                {
                    product.SpecialPrice = specialPrice;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    updatedCount++;
                }
            }

            _logger?.LogInformation(
                "Kampanya kaldırıldı: {CampaignName} ({CampaignId}), Güncellenen ürün: {UpdatedCount}",
                campaign.Name, campaignId, updatedCount);

            return updatedCount;
        }

        /// <summary>
        /// Tüm aktif kampanyaları yeniden hesaplar ve ürünlere uygular.
        /// </summary>
        public async Task<int> RecalculateAllCampaignsAsync()
        {
            _logger?.LogInformation("Tüm kampanyalar yeniden hesaplanıyor...");

            // Tüm ürünleri getir
            var products = (await _productRepository.GetAllAsync()).ToList();
            if (!products.Any())
            {
                _logger?.LogInformation("Hiç ürün bulunamadı.");
                return 0;
            }

            int updatedCount = 0;

            foreach (var product in products)
            {
                var (specialPrice, _) = await CalculateBestPriceAsync(
                    product.Id, product.CategoryId, product.Price);

                if (product.SpecialPrice != specialPrice)
                {
                    product.SpecialPrice = specialPrice;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    updatedCount++;
                }
            }

            _logger?.LogInformation(
                "Tüm kampanyalar hesaplandı. Toplam ürün: {TotalProducts}, Güncellenen: {UpdatedCount}",
                products.Count, updatedCount);

            return updatedCount;
        }

        /// <summary>
        /// Belirli bir ürün için en iyi kampanya indirimini hesaplar ve uygular.
        /// </summary>
        public async Task<Campaign?> ApplyBestCampaignToProductAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                _logger?.LogWarning("Ürün bulunamadı: {ProductId}", productId);
                return null;
            }

            var (specialPrice, appliedCampaign) = await CalculateBestPriceAsync(
                product.Id, product.CategoryId, product.Price);

            if (product.SpecialPrice != specialPrice)
            {
                product.SpecialPrice = specialPrice;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);

                _logger?.LogInformation(
                    "Ürün indirimi güncellendi: {ProductId}, Kampanya: {CampaignName}",
                    productId, appliedCampaign?.Name ?? "Yok");
            }

            return appliedCampaign;
        }

        /// <summary>
        /// Belirli bir kategorideki tüm ürünlerin kampanya indirimlerini yeniden hesaplar.
        /// </summary>
        public async Task<int> RecalculateCategoryProductsAsync(int categoryId)
        {
            var products = (await _productRepository.GetByCategoryIdAsync(categoryId)).ToList();
            if (!products.Any())
            {
                return 0;
            }

            int updatedCount = 0;

            foreach (var product in products)
            {
                var (specialPrice, _) = await CalculateBestPriceAsync(
                    product.Id, product.CategoryId, product.Price);

                if (product.SpecialPrice != specialPrice)
                {
                    product.SpecialPrice = specialPrice;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _productRepository.UpdateAsync(product);
                    updatedCount++;
                }
            }

            _logger?.LogInformation(
                "Kategori ürünleri hesaplandı: {CategoryId}, Güncellenen: {UpdatedCount}",
                categoryId, updatedCount);

            return updatedCount;
        }

        /// <summary>
        /// Bir ürün için geçerli kampanyalardan en iyi indirimi hesaplar (uygulamaz).
        /// </summary>
        public async Task<(decimal? SpecialPrice, Campaign? AppliedCampaign)> CalculateBestPriceAsync(
            int productId, int categoryId, decimal price)
        {
            // Ürün için geçerli kampanyaları getir
            var campaigns = await _campaignRepository.GetCampaignsForProductAsync(productId, categoryId);

            // Sadece fiyat indirimi yapan kampanyaları filtrele
            var priceDiscountCampaigns = campaigns
                .Where(c => c.Type == CampaignType.Percentage || c.Type == CampaignType.FixedAmount)
                .ToList();

            if (!priceDiscountCampaigns.Any())
            {
                return (null, null);
            }

            Campaign? bestCampaign = null;
            decimal bestDiscount = 0;
            decimal? bestSpecialPrice = null;

            foreach (var campaign in priceDiscountCampaigns)
            {
                var discount = CalculateDiscount(campaign, price);
                
                // MaxDiscountAmount kontrolü
                if (campaign.MaxDiscountAmount.HasValue && discount > campaign.MaxDiscountAmount.Value)
                {
                    discount = campaign.MaxDiscountAmount.Value;
                }

                // En yüksek indirim kontrolü
                if (discount > bestDiscount)
                {
                    bestDiscount = discount;
                    bestCampaign = campaign;
                    bestSpecialPrice = price - discount;

                    // Negatif fiyat kontrolü
                    if (bestSpecialPrice < 0)
                    {
                        bestSpecialPrice = 0;
                    }
                }
            }

            return (bestSpecialPrice, bestCampaign);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kampanya için hedef ürünleri getirir.
        /// </summary>
        private async Task<List<Product>> GetTargetProductsAsync(Campaign campaign)
        {
            var products = new List<Product>();

            switch (campaign.TargetType)
            {
                case CampaignTargetType.All:
                    // Tüm ürünler
                    products = (await _productRepository.GetAllAsync()).ToList();
                    break;

                case CampaignTargetType.Product:
                    // Belirli ürünler
                    var productIds = campaign.Targets
                        .Where(t => t.TargetKind == CampaignTargetKind.Product)
                        .Select(t => t.TargetId)
                        .ToList();

                    foreach (var productId in productIds)
                    {
                        var product = await _productRepository.GetByIdAsync(productId);
                        if (product != null)
                        {
                            products.Add(product);
                        }
                    }
                    break;

                case CampaignTargetType.Category:
                    // Belirli kategorilerdeki ürünler
                    var categoryIds = campaign.Targets
                        .Where(t => t.TargetKind == CampaignTargetKind.Category)
                        .Select(t => t.TargetId)
                        .ToList();

                    foreach (var categoryId in categoryIds)
                    {
                        var categoryProducts = await _productRepository.GetByCategoryIdAsync(categoryId);
                        products.AddRange(categoryProducts);
                    }
                    break;
            }

            // Aktif ürünleri filtrele ve tekrarları kaldır
            return products
                .Where(p => p.IsActive)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Kampanya türüne göre indirim tutarını hesaplar.
        /// </summary>
        private decimal CalculateDiscount(Campaign campaign, decimal price)
        {
            return campaign.Type switch
            {
                // Yüzdelik indirim: Price * (DiscountValue / 100)
                CampaignType.Percentage => price * (campaign.DiscountValue / 100m),
                
                // Sabit tutar indirim: DiscountValue
                CampaignType.FixedAmount => campaign.DiscountValue,
                
                // Diğer türler fiyat indirimi yapmaz
                _ => 0
            };
        }

        #endregion
    }
}
