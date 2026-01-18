using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Product;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ürün varyant işlemlerini yöneten servis.
    /// </summary>
    public class ProductVariantManager : IProductVariantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProductVariantManager> _logger;
        
        public ProductVariantManager(IUnitOfWork unitOfWork, ILogger<ProductVariantManager> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        
        #region CRUD Operations
        
        public async Task<ProductVariantDetailDto?> GetByIdAsync(int variantId)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            return variant != null ? MapToDetailDto(variant) : null;
        }
        
        public async Task<ProductVariantDetailDto?> GetBySkuAsync(string sku)
        {
            var variant = await _unitOfWork.ProductVariants.GetBySkuAsync(sku);
            return variant != null ? MapToDetailDto(variant) : null;
        }
        
        public async Task<IEnumerable<ProductVariantDetailDto>> GetByProductIdAsync(int productId)
        {
            var variants = await _unitOfWork.ProductVariants.GetByProductIdAsync(productId);
            return variants.Select(MapToDetailDto);
        }
        
        public async Task<ProductVariantDetailDto> CreateAsync(int productId, ProductVariantCreateDto dto)
        {
            var variant = new ProductVariant
            {
                ProductId = productId,
                SKU = dto.SKU,
                Title = dto.Title ?? dto.SKU,
                Price = dto.Price,
                Stock = dto.Stock,
                Currency = dto.Currency ?? "TRY",
                Barcode = dto.Barcode,
                WeightGrams = dto.WeightGrams,
                VolumeML = dto.VolumeML,
                SupplierCode = dto.SupplierCode,
                ParentSku = dto.ParentSku,
                IsActive = true,
                LastSyncedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };
            
            await _unitOfWork.ProductVariants.AddAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Varyant oluşturuldu: SKU={SKU}, ProductId={ProductId}", variant.SKU, productId);
            return MapToDetailDto(variant);
        }
        
        public async Task<ProductVariantDetailDto> UpdateAsync(int variantId, ProductVariantUpdateDto dto)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            if (variant == null)
                throw new KeyNotFoundException($"Varyant bulunamadı: {variantId}");
            
            if (dto.Title != null) variant.Title = dto.Title;
            if (dto.Price.HasValue) variant.Price = dto.Price.Value;
            if (dto.Stock.HasValue) variant.Stock = dto.Stock.Value;
            if (dto.Currency != null) variant.Currency = dto.Currency;
            if (dto.Barcode != null) variant.Barcode = dto.Barcode;
            if (dto.WeightGrams.HasValue) variant.WeightGrams = dto.WeightGrams.Value;
            if (dto.VolumeML.HasValue) variant.VolumeML = dto.VolumeML.Value;
            if (dto.SupplierCode != null) variant.SupplierCode = dto.SupplierCode;
            
            variant.LastSyncedAt = DateTime.UtcNow;
            variant.LastSeenAt = DateTime.UtcNow;
            
            await _unitOfWork.ProductVariants.UpdateAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Varyant güncellendi: ID={Id}, SKU={SKU}", variantId, variant.SKU);
            return MapToDetailDto(variant);
        }
        
        public async Task<bool> DeleteAsync(int variantId)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            if (variant == null) return false;
            
            variant.IsActive = false;
            await _unitOfWork.ProductVariants.UpdateAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Varyant silindi: ID={Id}, SKU={SKU}", variantId, variant.SKU);
            return true;
        }
        
        #endregion
        
        #region Stock Operations
        
        public async Task<bool> UpdateStockAsync(int variantId, int newStock)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            if (variant == null) return false;
            
            variant.Stock = newStock;
            await _unitOfWork.ProductVariants.UpdateAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> AdjustStockAsync(int variantId, int adjustment, string reason)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            if (variant == null) return false;
            
            variant.Stock = Math.Max(0, variant.Stock + adjustment);
            await _unitOfWork.ProductVariants.UpdateAsync(variant);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Stok ayarlandı: ID={Id}, Adjustment={Adj}, Reason={Reason}", 
                variantId, adjustment, reason);
            return true;
        }
        
        public async Task<IEnumerable<ProductVariantDetailDto>> GetLowStockVariantsAsync(int threshold = 5)
        {
            var variants = await _unitOfWork.ProductVariants.GetLowStockVariantsAsync(threshold);
            return variants.Select(MapToDetailDto);
        }
        
        public async Task<bool> CheckStockAvailabilityAsync(int variantId, int requiredQuantity)
        {
            var variant = await _unitOfWork.ProductVariants.GetByIdAsync(variantId);
            return variant != null && variant.Stock >= requiredQuantity;
        }
        
        #endregion
        
        #region Bulk Operations
        
        public async Task<int> BulkUpdateStockAsync(IDictionary<string, int> skuStockMap)
        {
            var skuList = skuStockMap.Keys.ToList();
            var existingVariants = await _unitOfWork.ProductVariants.GetBySkusAsync(skuList);
            
            int count = 0;
            foreach (var kvp in skuStockMap)
            {
                if (existingVariants.TryGetValue(kvp.Key, out var variant))
                {
                    variant.Stock = kvp.Value;
                    variant.LastSyncedAt = DateTime.UtcNow;
                    await _unitOfWork.ProductVariants.UpdateAsync(variant);
                    count++;
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return count;
        }
        
        public async Task<int> BulkUpdatePricesAsync(IDictionary<string, decimal> skuPriceMap)
        {
            var skuList = skuPriceMap.Keys.ToList();
            var existingVariants = await _unitOfWork.ProductVariants.GetBySkusAsync(skuList);
            
            int count = 0;
            foreach (var kvp in skuPriceMap)
            {
                if (existingVariants.TryGetValue(kvp.Key, out var variant))
                {
                    variant.Price = kvp.Value;
                    await _unitOfWork.ProductVariants.UpdateAsync(variant);
                    count++;
                }
            }
            await _unitOfWork.SaveChangesAsync();
            return count;
        }
        
        public async Task<int> DeactivateStaleVariantsAsync(int feedSourceId, int hoursThreshold = 48)
        {
            var threshold = DateTime.UtcNow.AddHours(-hoursThreshold);
            var variants = await _unitOfWork.ProductVariants.GetVariantsNotSeenSinceAsync(threshold);
            
            int count = 0;
            foreach (var variant in variants.Where(v => v.IsActive))
            {
                variant.IsActive = false;
                await _unitOfWork.ProductVariants.UpdateAsync(variant);
                count++;
            }
            await _unitOfWork.SaveChangesAsync();
            return count;
        }
        
        #endregion
        
        #region Option Management
        
        public Task AddOptionValueAsync(int variantId, int optionId, int optionValueId)
        {
            // TODO: VariantOptionValue repository eklenince implement edilecek
            return Task.CompletedTask;
        }
        
        public Task RemoveOptionValueAsync(int variantId, int optionId)
        {
            // TODO: VariantOptionValue repository eklenince implement edilecek
            return Task.CompletedTask;
        }
        
        public Task UpdateOptionValuesAsync(int variantId, IEnumerable<(int OptionId, int ValueId)> optionValues)
        {
            // TODO: VariantOptionValue repository eklenince implement edilecek
            return Task.CompletedTask;
        }
        
        #endregion
        
        #region Query & Statistics
        
        public async Task<VariantStatisticsDto> GetStatisticsAsync(int? feedSourceId = null)
        {
            var allVariants = await _unitOfWork.ProductVariants.GetAllAsync();
            var variants = allVariants.ToList();
            
            return new VariantStatisticsDto
            {
                TotalVariants = variants.Count,
                ActiveVariants = variants.Count(v => v.IsActive),
                InactiveVariants = variants.Count(v => !v.IsActive),
                OutOfStockVariants = variants.Count(v => v.Stock == 0),
                LowStockVariants = variants.Count(v => v.Stock > 0 && v.Stock < 5),
                TotalStockValue = variants.Sum(v => v.Stock * v.Price)
            };
        }
        
        public async Task<ProductVariantDetailDto?> FindByOptionsAsync(int productId, IDictionary<int, int> optionValueMap)
        {
            var variants = await _unitOfWork.ProductVariants.GetByProductIdAsync(productId);
            return variants.Select(MapToDetailDto).FirstOrDefault();
        }
        
        #endregion
        
        #region Private Helpers
        
        private ProductVariantDetailDto MapToDetailDto(ProductVariant entity)
        {
            return new ProductVariantDetailDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                SKU = entity.SKU,
                Title = entity.Title,
                Price = entity.Price,
                Stock = entity.Stock,
                Currency = entity.Currency,
                Barcode = entity.Barcode,
                WeightGrams = entity.WeightGrams,
                VolumeML = entity.VolumeML,
                SupplierCode = entity.SupplierCode,
                ParentSku = entity.ParentSku,
                IsActive = entity.IsActive,
                LastSyncedAt = entity.LastSyncedAt,
                LastSeenAt = entity.LastSeenAt
            };
        }
        
        #endregion
    }
}
