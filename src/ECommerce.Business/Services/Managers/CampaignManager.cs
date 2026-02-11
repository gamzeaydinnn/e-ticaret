using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Promotions;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Kampanya yönetimi servisi.
    /// Kampanya CRUD işlemleri ve indirim hesaplama mantığını yönetir.
    /// 
    /// Desteklenen kampanya türleri:
    /// 1. Percentage: Yüzdelik indirim (örn: %10)
    /// 2. FixedAmount: Sabit tutar indirim (örn: 50 TL)
    /// 3. BuyXPayY: X al Y öde (örn: 3 al 2 öde)
    /// 4. FreeShipping: Ücretsiz kargo
    /// 
    /// Kampanya seçim mantığı:
    /// - Aynı ürüne birden fazla kampanya uygunsa, EN YÜKSEK İNDİRİMİ veren seçilir
    /// - BuyXPayY kampanyaları kategori içinde karışık ürünlerde de çalışır
    /// </summary>
    public class CampaignManager : ICampaignService
    {
        private readonly ICampaignRepository _campaignRepository;
        private readonly ILogger<CampaignManager>? _logger;

        public CampaignManager(
            ICampaignRepository campaignRepository,
            ILogger<CampaignManager>? logger = null)
        {
            _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
            _logger = logger;
        }

        #region CRUD Operasyonları

        public Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null)
        {
            return _campaignRepository.GetActiveCampaignsAsync(now);
        }

        public async Task<List<Campaign>> GetAllAsync()
        {
            var campaigns = await _campaignRepository.GetAllAsync();
            return campaigns.ToList();
        }

        public Task<Campaign?> GetByIdAsync(int id)
        {
            return _campaignRepository.GetByIdAsync(id);
        }

        public async Task<Campaign> CreateAsync(CampaignSaveDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            // Tarih validasyonu
            if (dto.EndDate <= dto.StartDate)
            {
                throw new ArgumentException("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            }

            // BuyXPayY validasyonu
            if (dto.Type == CampaignType.BuyXPayY)
            {
                if (!dto.BuyQty.HasValue || !dto.PayQty.HasValue)
                {
                    throw new ArgumentException("X Al Y Öde kampanyası için BuyQty ve PayQty zorunludur.");
                }
                if (dto.PayQty >= dto.BuyQty)
                {
                    throw new ArgumentException("Ödenecek adet (PayQty), alınacak adetten (BuyQty) küçük olmalıdır.");
                }
            }

            // ====================================================================
            // UNIQUE NAME VALİDASYONU
            // Aynı isimde aktif bir kampanyanın var olup olmadığını kontrol eder.
            // Database unique index'i daha sonra son koruma katmanı olarak çalışır.
            // ====================================================================
            var existingCampaign = (await _campaignRepository.GetAllAsync())
                .FirstOrDefault(c => c.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                                     && c.IsActive);

            if (existingCampaign != null)
            {
                throw new InvalidOperationException(
                    $"'{dto.Name}' adında aktif bir kampanya zaten mevcut. Lütfen farklı bir isim kullanın.");
            }

            var campaign = new Campaign
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                
                // Yeni alanlar
                Type = dto.Type,
                TargetType = dto.TargetType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinCartTotal = dto.MinCartTotal,
                MinQuantity = dto.MinQuantity,
                BuyQty = dto.BuyQty,
                PayQty = dto.PayQty,
                Priority = dto.Priority,
                IsStackable = dto.IsStackable,
                ImageUrl = dto.ImageUrl
            };

            // Geriye dönük uyumluluk için eski alanları da doldur
            #pragma warning disable CS0618
            if (!string.IsNullOrWhiteSpace(dto.ConditionJson))
            {
                campaign.Rules.Add(new CampaignRule
                {
                    ConditionJson = dto.ConditionJson.Trim(),
                    Campaign = campaign
                });
            }

            campaign.Rewards.Add(new CampaignReward
            {
                RewardType = string.IsNullOrWhiteSpace(dto.RewardType) ? "Percent" : dto.RewardType,
                Value = dto.RewardValue > 0 ? dto.RewardValue : dto.DiscountValue,
                Campaign = campaign
            });
            #pragma warning restore CS0618

            // Hedef türünü belirle
            CampaignTargetKind targetKind = CampaignTargetKind.Product; // Default
            if (dto.TargetType == CampaignTargetType.Category)
            {
                targetKind = CampaignTargetKind.Category;
            }

            // Hedef ID'leri hazırla
            IEnumerable<int>? targetIds = null;
            if (dto.TargetType != CampaignTargetType.All && dto.TargetIds?.Any() == true)
            {
                targetIds = dto.TargetIds;
            }

            // Kampanyayı hedefleriyle birlikte atomik olarak oluştur (transaction kullanarak)
            // Bu sayede kampanya oluşturulup hedefler eklenemezse rollback yapılır
            campaign = await _campaignRepository.CreateCampaignWithTargetsAsync(campaign, targetIds, targetKind);

            _logger?.LogInformation(
                "Kampanya oluşturuldu: {CampaignName}, Tür: {Type}, Hedef: {TargetType}",
                campaign.Name, campaign.Type, campaign.TargetType);

            return campaign;
        }

        public async Task<Campaign> UpdateAsync(int id, CampaignSaveDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                throw new KeyNotFoundException($"Kampanya bulunamadı: {id}");
            }

            // Tarih validasyonu
            if (dto.EndDate <= dto.StartDate)
            {
                throw new ArgumentException("Bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            }

            // BuyXPayY validasyonu
            if (dto.Type == CampaignType.BuyXPayY)
            {
                if (!dto.BuyQty.HasValue || !dto.PayQty.HasValue)
                {
                    throw new ArgumentException("X Al Y Öde kampanyası için BuyQty ve PayQty zorunludur.");
                }
                if (dto.PayQty >= dto.BuyQty)
                {
                    throw new ArgumentException("Ödenecek adet (PayQty), alınacak adetten (BuyQty) küçük olmalıdır.");
                }
            }

            // ====================================================================
            // UNIQUE NAME VALİDASYONU
            // Aynı isimde aktif başka bir kampanyanın var olup olmadığını kontrol eder.
            // Kendi ID'sini hariç tutar (excludeId).
            // ====================================================================
            var existingCampaign = (await _campaignRepository.GetAllAsync())
                .FirstOrDefault(c => c.Id != id  // Kendi ID'sini hariç tut
                                     && c.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                                     && c.IsActive);

            if (existingCampaign != null)
            {
                throw new InvalidOperationException(
                    $"'{dto.Name}' adında başka bir aktif kampanya zaten mevcut. Lütfen farklı bir isim kullanın.");
            }

            // Temel alanları güncelle
            campaign.Name = dto.Name.Trim();
            campaign.Description = dto.Description?.Trim();
            campaign.ImageUrl = dto.ImageUrl;
            campaign.StartDate = dto.StartDate;
            campaign.EndDate = dto.EndDate;
            campaign.IsActive = dto.IsActive;
            
            // Yeni alanları güncelle
            campaign.Type = dto.Type;
            campaign.TargetType = dto.TargetType;
            campaign.DiscountValue = dto.DiscountValue;
            campaign.MaxDiscountAmount = dto.MaxDiscountAmount;
            campaign.MinCartTotal = dto.MinCartTotal;
            campaign.MinQuantity = dto.MinQuantity;
            campaign.BuyQty = dto.BuyQty;
            campaign.PayQty = dto.PayQty;
            campaign.Priority = dto.Priority;
            campaign.IsStackable = dto.IsStackable;
            campaign.UpdatedAt = DateTime.UtcNow;

            // Geriye dönük uyumluluk için eski alanları güncelle
            #pragma warning disable CS0618
            var condition = dto.ConditionJson?.Trim();
            if (!string.IsNullOrEmpty(condition))
            {
                var rule = campaign.Rules.FirstOrDefault();
                if (rule == null)
                {
                    rule = new CampaignRule { CampaignId = campaign.Id };
                    campaign.Rules.Add(rule);
                }
                rule.ConditionJson = condition;
            }
            else
            {
                var rule = campaign.Rules.FirstOrDefault();
                if (rule != null)
                {
                    rule.ConditionJson = string.Empty;
                }
            }

            var reward = campaign.Rewards.FirstOrDefault();
            if (reward == null)
            {
                reward = new CampaignReward { CampaignId = campaign.Id };
                campaign.Rewards.Add(reward);
            }
            reward.RewardType = string.IsNullOrWhiteSpace(dto.RewardType) ? "Percent" : dto.RewardType;
            reward.Value = dto.RewardValue > 0 ? dto.RewardValue : dto.DiscountValue;
            #pragma warning restore CS0618

            await _campaignRepository.UpdateAsync(campaign);

            // Hedefleri güncelle
            if (dto.TargetType == CampaignTargetType.All)
            {
                // Tüm hedefleri temizle
                await _campaignRepository.ClearTargetsAsync(campaign.Id);
            }
            else if (dto.TargetIds?.Any() == true)
            {
                var targetKind = dto.TargetType == CampaignTargetType.Product 
                    ? CampaignTargetKind.Product 
                    : CampaignTargetKind.Category;
                
                await _campaignRepository.UpdateTargetsAsync(campaign.Id, dto.TargetIds, targetKind);
            }

            _logger?.LogInformation(
                "Kampanya güncellendi: {CampaignId} - {CampaignName}", 
                campaign.Id, campaign.Name);

            return campaign;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return false;
            }

            // Önce hedefleri temizle
            await _campaignRepository.ClearTargetsAsync(id);
            
            // HARD DELETE - Kampanyayı veritabanından tamamen sil
            await _campaignRepository.HardDeleteAsync(campaign);
            
            _logger?.LogInformation("Kampanya kalıcı olarak silindi: {CampaignId}", id);
            
            return true;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
            {
                return false;
            }

            campaign.IsActive = !campaign.IsActive;
            campaign.UpdatedAt = DateTime.UtcNow;
            
            await _campaignRepository.UpdateAsync(campaign);
            
            _logger?.LogInformation(
                "Kampanya durumu değiştirildi: {CampaignId} -> {IsActive}", 
                id, campaign.IsActive);
            
            return true;
        }

        #endregion

        #region Aktif Kampanya Sorgulama

        public Task<List<Campaign>> GetApplicableCampaignsForProductAsync(int productId, int categoryId)
        {
            return _campaignRepository.GetCampaignsForProductAsync(productId, categoryId);
        }

        public async Task<Campaign?> GetFreeShippingCampaignAsync(decimal cartTotal)
        {
            var freeShippingCampaigns = await _campaignRepository.GetActiveCampaignsByTypeAsync(CampaignType.FreeShipping);
            
            // Sepet tutarı koşulunu sağlayan ilk kampanyayı döndür
            return freeShippingCampaigns.FirstOrDefault(c => 
                !c.MinCartTotal.HasValue || cartTotal >= c.MinCartTotal.Value);
        }

        #endregion

        #region Kampanya Hesaplama

        /// <summary>
        /// Sepet için tüm kampanya indirimlerini hesaplar.
        /// 
        /// Algoritma:
        /// 1. Aktif kampanyaları getir
        /// 2. Her satır için uygun kampanyaları bul
        /// 3. Her satır için en yüksek indirimi veren kampanyayı seç
        /// 4. BuyXPayY kampanyalarını ayrı hesapla (kategori bazlı gruplama)
        /// 5. FreeShipping kontrolü yap
        /// </summary>
        public async Task<CampaignCalculationResult> CalculateCampaignDiscountsAsync(
            IEnumerable<CartItemForCampaign> items, 
            decimal cartTotal)
        {
            var result = new CampaignCalculationResult();
            var itemsList = items.ToList();

            if (!itemsList.Any())
            {
                return result;
            }

            try
            {
                var activeCampaigns = await GetActiveCampaignsAsync();
                
                if (!activeCampaigns.Any())
                {
                    return result;
                }

                // 1. Önce BuyXPayY kampanyalarını işle (kategori bazlı gruplama gerektirir)
                var buyXPayYCampaigns = activeCampaigns
                    .Where(c => c.Type == CampaignType.BuyXPayY)
                    .ToList();

                foreach (var campaign in buyXPayYCampaigns)
                {
                    var scopedItems = GetScopedItems(campaign, itemsList);
                    if (scopedItems.Any())
                    {
                        var discount = await CalculateBuyXPayYDiscountAsync(campaign, scopedItems);
                        if (discount > 0)
                        {
                            result.AppliedCampaigns.Add(new AppliedCampaignDto
                            {
                                CampaignId = campaign.Id,
                                CampaignName = campaign.Name,
                                Type = campaign.Type,
                                DiscountAmount = discount,
                                DisplayText = $"{campaign.BuyQty} Al {campaign.PayQty} Öde (-{discount:C2})",
                                AppliedToItemIds = scopedItems.Select(i => i.ProductId).ToList()
                            });
                            result.TotalCampaignDiscount += discount;
                            
                            // Bu ürünleri satır bazlı hesaplamadan çıkar
                            // (BuyXPayY zaten uygulandı)
                            foreach (var item in scopedItems)
                            {
                                result.LineDiscounts[item.ProductId] = 
                                    result.LineDiscounts.GetValueOrDefault(item.ProductId) + 
                                    (discount / scopedItems.Count); // Yaklaşık dağıtım
                            }
                        }
                    }
                }

                // 2. Satır bazlı kampanyaları işle (Percentage, FixedAmount)
                var lineCampaigns = activeCampaigns
                    .Where(c => c.Type == CampaignType.Percentage || c.Type == CampaignType.FixedAmount)
                    .ToList();

                foreach (var item in itemsList)
                {
                    // Bu ürüne BuyXPayY zaten uygulandıysa atla
                    if (result.LineDiscounts.ContainsKey(item.ProductId))
                    {
                        continue;
                    }

                    var (bestCampaign, discount) = await CalculateBestCampaignForItemInternalAsync(
                        lineCampaigns, item.ProductId, item.CategoryId, item.UnitPrice, item.Quantity);

                    if (bestCampaign != null && discount > 0)
                    {
                        // Aynı kampanya zaten eklendiyse tutarı güncelle
                        var existingCampaign = result.AppliedCampaigns
                            .FirstOrDefault(c => c.CampaignId == bestCampaign.Id);

                        if (existingCampaign != null)
                        {
                            existingCampaign.DiscountAmount += discount;
                            existingCampaign.AppliedToItemIds.Add(item.ProductId);
                        }
                        else
                        {
                            var displayText = bestCampaign.Type == CampaignType.Percentage
                                ? $"%{bestCampaign.DiscountValue} İndirim"
                                : $"{bestCampaign.DiscountValue:C2} İndirim";

                            result.AppliedCampaigns.Add(new AppliedCampaignDto
                            {
                                CampaignId = bestCampaign.Id,
                                CampaignName = bestCampaign.Name,
                                Type = bestCampaign.Type,
                                DiscountAmount = discount,
                                DisplayText = displayText,
                                AppliedToItemIds = new List<int> { item.ProductId }
                            });
                        }

                        result.LineDiscounts[item.ProductId] = discount;
                        result.TotalCampaignDiscount += discount;
                    }
                }

                // 3. Ücretsiz kargo kontrolü (kampanya kapsamına göre)
                var freeShippingCampaigns = activeCampaigns
                    .Where(c => c.Type == CampaignType.FreeShipping)
                    .ToList();

                foreach (var campaign in freeShippingCampaigns)
                {
                    var scopedItems = GetScopedItems(campaign, itemsList);
                    if (!scopedItems.Any())
                    {
                        continue;
                    }

                    if (campaign.TargetType != CampaignTargetType.All &&
                        scopedItems.Count != itemsList.Count)
                    {
                        continue;
                    }

                    var scopedTotal = scopedItems.Sum(i => i.LineTotal);
                    var eligibleTotal = campaign.TargetType == CampaignTargetType.All
                        ? cartTotal
                        : scopedTotal;

                    if (campaign.MinCartTotal.HasValue &&
                        eligibleTotal < campaign.MinCartTotal.Value)
                    {
                        continue;
                    }

                    result.IsFreeShipping = true;
                    result.AppliedCampaigns.Add(new AppliedCampaignDto
                    {
                        CampaignId = campaign.Id,
                        CampaignName = campaign.Name,
                        Type = CampaignType.FreeShipping,
                        DiscountAmount = 0, // Kargo ücreti ayrıca hesaplanacak
                        DisplayText = "Ücretsiz Kargo",
                        AppliedToItemIds = scopedItems.Select(i => i.ProductId).ToList()
                    });
                    break;
                }

                _logger?.LogDebug(
                    "Kampanya hesaplama tamamlandı. Toplam indirim: {TotalDiscount}, Uygulanan kampanya sayısı: {Count}",
                    result.TotalCampaignDiscount, result.AppliedCampaigns.Count);
            }
            // ====================================================================
            // GELİŞMİŞ HATA YÖNETİMİ
            // Sessizce hata yutma yerine, hataları sınıflandırarak fırlatır.
            // Controller katmanında uygun HTTP status kodları ile döndürülür.
            // ====================================================================
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger?.LogError(dbEx, "Kampanya hesaplama - Veritabanı hatası. CartTotal: {CartTotal}, Items: {ItemCount}",
                    cartTotal, itemsList.Count);

                throw new Exceptions.CampaignCalculationException(
                    "Kampanya bilgileri veritabanından alınamadı. Lütfen tekrar deneyin.",
                    $"DbUpdateException: {dbEx.Message}",
                    dbEx);
            }
            catch (TimeoutException timeoutEx)
            {
                _logger?.LogError(timeoutEx, "Kampanya hesaplama - Zaman aşımı. CartTotal: {CartTotal}, Items: {ItemCount}",
                    cartTotal, itemsList.Count);

                throw new Exceptions.CampaignCalculationException(
                    "Kampanya hesaplama işlemi zaman aşımına uğradı. Lütfen tekrar deneyin.",
                    $"TimeoutException: {timeoutEx.Message}",
                    timeoutEx);
            }
            catch (NullReferenceException nullEx)
            {
                _logger?.LogError(nullEx, "Kampanya hesaplama - Null referans hatası. CartTotal: {CartTotal}, Items: {ItemCount}",
                    cartTotal, itemsList.Count);

                throw new Exceptions.CampaignCalculationException(
                    "Kampanya bilgilerinde eksik veri bulundu. Lütfen teknik destek ile iletişime geçin.",
                    $"NullReferenceException at {nullEx.TargetSite?.Name}: {nullEx.Message}",
                    nullEx);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex,
                    "Kampanya hesaplama - Beklenmeyen hata. CartTotal: {CartTotal}, Items: {ItemCount}, Exception: {ExceptionType}",
                    cartTotal, itemsList.Count, ex.GetType().Name);

                throw new Exceptions.CampaignCalculationException(
                    "Kampanya indirimleri hesaplanamadı. Lütfen tekrar deneyin veya destek ekibi ile iletişime geçin.",
                    $"Unexpected {ex.GetType().Name}: {ex.Message}",
                    ex);
            }

            return result;
        }

        /// <summary>
        /// Tek bir satır için en iyi kampanyayı ve indirim tutarını hesaplar
        /// </summary>
        public async Task<(Campaign? Campaign, decimal Discount)> CalculateBestCampaignForItemAsync(
            int productId, 
            int categoryId, 
            decimal unitPrice, 
            int quantity)
        {
            var applicableCampaigns = await GetApplicableCampaignsForProductAsync(productId, categoryId);
            
            // Sadece satır bazlı kampanyaları filtrele (Percentage, FixedAmount)
            var lineCampaigns = applicableCampaigns
                .Where(c => c.Type == CampaignType.Percentage || c.Type == CampaignType.FixedAmount)
                .ToList();

            return await CalculateBestCampaignForItemInternalAsync(lineCampaigns, productId, categoryId, unitPrice, quantity);
        }

        /// <summary>
        /// X Al Y Öde kampanyası için indirim hesaplar.
        /// 
        /// Algoritma:
        /// 1. Kapsamdaki ürünleri birim fiyata göre sırala (ucuzdan pahalıya)
        /// 2. Toplam adet / BuyQty = Kaç set var
        /// 3. Her set için (BuyQty - PayQty) adet bedava
        /// 4. Bedava ürünler en ucuzlardan seçilir
        /// 
        /// Örnek: 3 Al 2 Öde, 5 ürün var
        /// - 5 / 3 = 1 set (3 ürünlük grup)
        /// - 1 set * (3-2) = 1 adet bedava
        /// - En ucuz 1 ürün bedava olur
        /// </summary>
        public Task<decimal> CalculateBuyXPayYDiscountAsync(
            Campaign campaign,
            IEnumerable<CartItemForCampaign> scopedItems)
        {
            if (campaign.Type != CampaignType.BuyXPayY ||
                !campaign.BuyQty.HasValue ||
                !campaign.PayQty.HasValue)
            {
                return Task.FromResult(0m);
            }

            var itemsList = scopedItems.ToList();
            if (!itemsList.Any())
            {
                return Task.FromResult(0m);
            }

            int buyQty = campaign.BuyQty.Value;
            int payQty = campaign.PayQty.Value;
            int freePerSet = buyQty - payQty; // Her set için bedava adet

            // Tüm ürünleri birim bazında listele (adet kadar tekrarla)
            // Örn: 2 adet 100 TL'lik ürün -> [100, 100]
            var allUnits = itemsList
                .SelectMany(item => Enumerable.Repeat(item.UnitPrice, item.Quantity))
                .OrderBy(price => price) // Ucuzdan pahalıya
                .ToList();

            int totalQuantity = allUnits.Count;
            
            // Minimum adet kontrolü
            if (campaign.MinQuantity.HasValue && totalQuantity < campaign.MinQuantity.Value)
            {
                return Task.FromResult(0m);
            }

            // Kaç set oluşuyor?
            int sets = totalQuantity / buyQty;
            
            if (sets == 0)
            {
                return Task.FromResult(0m);
            }

            // Bedava olacak toplam adet
            int freeUnits = sets * freePerSet;

            // En ucuz 'freeUnits' adet kadar ürünü bedava yap
            decimal totalDiscount = allUnits.Take(freeUnits).Sum();

            _logger?.LogDebug(
                "BuyXPayY hesaplama: Kampanya={Campaign}, Toplam adet={Total}, Set={Sets}, Bedava={Free}, İndirim={Discount}",
                campaign.Name, totalQuantity, sets, freeUnits, totalDiscount);

            return Task.FromResult(totalDiscount);
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Kampanyanın kapsamındaki ürünleri filtreler
        /// </summary>
        private List<CartItemForCampaign> GetScopedItems(Campaign campaign, List<CartItemForCampaign> items)
        {
            return campaign.TargetType switch
            {
                CampaignTargetType.All => items,
                
                CampaignTargetType.Product => items
                    .Where(i => campaign.Targets.Any(t => 
                        t.TargetKind == CampaignTargetKind.Product && t.TargetId == i.ProductId))
                    .ToList(),
                
                CampaignTargetType.Category => items
                    .Where(i => campaign.Targets.Any(t => 
                        t.TargetKind == CampaignTargetKind.Category && t.TargetId == i.CategoryId))
                    .ToList(),
                
                _ => new List<CartItemForCampaign>()
            };
        }

        /// <summary>
        /// Verilen kampanya listesi içinden en iyi kampanyayı seçer
        /// </summary>
        private Task<(Campaign? Campaign, decimal Discount)> CalculateBestCampaignForItemInternalAsync(
            List<Campaign> campaigns,
            int productId, 
            int categoryId, 
            decimal unitPrice, 
            int quantity)
        {
            Campaign? bestCampaign = null;
            decimal bestDiscount = 0;
            decimal lineTotal = unitPrice * quantity;

            foreach (var campaign in campaigns)
            {
                // Bu kampanya bu ürüne uygulanabilir mi?
                if (!IsCampaignApplicableToProduct(campaign, productId, categoryId))
                {
                    continue;
                }

                // Minimum adet kontrolü
                if (campaign.MinQuantity.HasValue && quantity < campaign.MinQuantity.Value)
                {
                    continue;
                }

                // İndirimi hesapla
                decimal discount = CalculateDiscount(campaign, unitPrice, quantity);

                // En yüksek indirimi veren kampanyayı seç
                if (discount > bestDiscount)
                {
                    bestDiscount = discount;
                    bestCampaign = campaign;
                }
            }

            return Task.FromResult((bestCampaign, bestDiscount));
        }

        /// <summary>
        /// Kampanyanın ürüne uygulanıp uygulanamayacağını kontrol eder
        /// </summary>
        private bool IsCampaignApplicableToProduct(Campaign campaign, int productId, int categoryId)
        {
            return campaign.TargetType switch
            {
                CampaignTargetType.All => true,
                
                CampaignTargetType.Product => campaign.Targets.Any(t => 
                    t.TargetKind == CampaignTargetKind.Product && t.TargetId == productId),
                
                CampaignTargetType.Category => campaign.Targets.Any(t => 
                    t.TargetKind == CampaignTargetKind.Category && t.TargetId == categoryId),
                
                _ => false
            };
        }

        /// <summary>
        /// Kampanya türüne göre indirim tutarını hesaplar
        /// </summary>
        private decimal CalculateDiscount(Campaign campaign, decimal unitPrice, int quantity)
        {
            decimal lineTotal = unitPrice * quantity;
            decimal discount = 0;

            switch (campaign.Type)
            {
                case CampaignType.Percentage:
                    // Yüzdelik indirim
                    discount = lineTotal * (campaign.DiscountValue / 100m);
                    
                    // Maksimum indirim kontrolü
                    if (campaign.MaxDiscountAmount.HasValue && discount > campaign.MaxDiscountAmount.Value)
                    {
                        discount = campaign.MaxDiscountAmount.Value;
                    }
                    break;

                case CampaignType.FixedAmount:
                    // Sabit tutar indirim (satır başına)
                    discount = Math.Min(campaign.DiscountValue, lineTotal);
                    break;

                // BuyXPayY ve FreeShipping burada hesaplanmaz
                default:
                    break;
            }

            return Math.Round(discount, 2);
        }

        #endregion
    }
}

