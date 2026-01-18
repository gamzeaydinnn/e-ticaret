// ProductVariantRepository: Ürün varyantları için repository implementasyonu.
// SKU benzersizliği, toplu işlemler ve XML entegrasyonu için optimize edilmiştir.
// Performans için batch işlemler ve efficient sorgular kullanılır.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Ürün varyantları için repository implementasyonu.
    /// XML import sisteminin temel veri erişim katmanı.
    /// </summary>
    public class ProductVariantRepository : BaseRepository<ProductVariant>, IProductVariantRepository
    {
        public ProductVariantRepository(ECommerceDbContext context) : base(context)
        {
        }

        #region Tekil Sorgular

        /// <inheritdoc/>
        public async Task<ProductVariant?> GetBySkuAsync(string sku)
        {
            // SKU case-insensitive olarak aranır (Turkish collation kullanıldığı için)
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(v => v.SKU == sku);
        }

        /// <inheritdoc/>
        public async Task<ProductVariant?> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(v => v.Barcode == barcode && v.IsActive);
        }

        /// <inheritdoc/>
        public async Task<ProductVariant?> GetByIdWithOptionsAsync(int id)
        {
            return await _dbSet
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.OptionValue)
                        .ThenInclude(ov => ov.Option)
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        #endregion

        #region Liste Sorguları

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId, bool includeInactive = false)
        {
            var query = _dbSet
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.OptionValue)
                .Where(v => v.ProductId == productId);

            if (!includeInactive)
            {
                query = query.Where(v => v.IsActive);
            }

            return await query
                .OrderBy(v => v.Title)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductVariant>> GetByParentSkuAsync(string parentSku)
        {
            if (string.IsNullOrWhiteSpace(parentSku))
                return Enumerable.Empty<ProductVariant>();

            return await _dbSet
                .Where(v => v.ParentSku == parentSku && v.IsActive)
                .OrderBy(v => v.Title)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductVariant>> GetBySupplierCodeAsync(string supplierCode)
        {
            if (string.IsNullOrWhiteSpace(supplierCode))
                return Enumerable.Empty<ProductVariant>();

            return await _dbSet
                .Where(v => v.SupplierCode == supplierCode && v.IsActive)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductVariant>> GetVariantsNotSeenSinceAsync(DateTime date)
        {
            // LastSeenAt null olanlar veya belirtilen tarihten önce görülenler
            return await _dbSet
                .Where(v => v.IsActive && 
                           (v.LastSeenAt == null || v.LastSeenAt < date))
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync(int maxStock = 5)
        {
            return await _dbSet
                .Where(v => v.IsActive && v.Stock <= maxStock)
                .OrderBy(v => v.Stock)
                .ToListAsync();
        }

        #endregion

        #region Toplu İşlemler

        /// <inheritdoc/>
        public async Task<Dictionary<string, ProductVariant>> GetBySkusAsync(IEnumerable<string> skus)
        {
            var skuList = skus?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            
            if (skuList == null || skuList.Count == 0)
                return new Dictionary<string, ProductVariant>();

            // Büyük listeler için batch sorgu
            // SQL Server'da IN clause limiti nedeniyle parçalara böl
            const int batchSize = 500;
            var result = new Dictionary<string, ProductVariant>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < skuList.Count; i += batchSize)
            {
                var batch = skuList.Skip(i).Take(batchSize).ToList();
                var variants = await _dbSet
                    .Where(v => batch.Contains(v.SKU))
                    .ToListAsync();

                foreach (var variant in variants)
                {
                    // Case-insensitive key
                    result[variant.SKU] = variant;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<BulkUpsertResult> BulkUpsertAsync(IEnumerable<ProductVariant> variants)
        {
            var result = new BulkUpsertResult();
            var variantList = variants?.ToList() ?? new List<ProductVariant>();

            if (variantList.Count == 0)
                return result;

            try
            {
                // Mevcut SKU'ları toplu sorgula
                var skus = variantList.Select(v => v.SKU).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var existingVariants = await GetBySkusAsync(skus);

                var toInsert = new List<ProductVariant>();
                var toUpdate = new List<ProductVariant>();

                foreach (var variant in variantList)
                {
                    if (string.IsNullOrWhiteSpace(variant.SKU))
                    {
                        result.FailedCount++;
                        result.Errors[variant.SKU ?? "NULL"] = "SKU boş olamaz";
                        continue;
                    }

                    if (existingVariants.TryGetValue(variant.SKU, out var existing))
                    {
                        // Güncelle
                        var hasChanges = UpdateExistingVariant(existing, variant);
                        if (hasChanges)
                        {
                            toUpdate.Add(existing);
                        }
                        else
                        {
                            result.UnchangedCount++;
                        }
                    }
                    else
                    {
                        // Yeni ekle
                        variant.CreatedAt = DateTime.UtcNow;
                        variant.LastSeenAt = DateTime.UtcNow;
                        variant.LastSyncedAt = DateTime.UtcNow;
                        toInsert.Add(variant);
                    }
                }

                // Batch insert
                if (toInsert.Count > 0)
                {
                    await _dbSet.AddRangeAsync(toInsert);
                    result.InsertedCount = toInsert.Count;
                }

                // Batch update (EF Core tracking ile)
                foreach (var variant in toUpdate)
                {
                    variant.UpdatedAt = DateTime.UtcNow;
                    variant.LastSeenAt = DateTime.UtcNow;
                    variant.LastSyncedAt = DateTime.UtcNow;
                }
                result.UpdatedCount = toUpdate.Count;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.FailedCount = variantList.Count;
                result.Errors["BULK_ERROR"] = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Mevcut varyantı yeni değerlerle günceller.
        /// Değişiklik varsa true döner.
        /// </summary>
        private bool UpdateExistingVariant(ProductVariant existing, ProductVariant newValues)
        {
            bool hasChanges = false;

            // Sadece değişen alanları güncelle
            if (existing.Title != newValues.Title && !string.IsNullOrEmpty(newValues.Title))
            {
                existing.Title = newValues.Title;
                hasChanges = true;
            }

            if (existing.Price != newValues.Price)
            {
                existing.Price = newValues.Price;
                hasChanges = true;
            }

            if (existing.Stock != newValues.Stock)
            {
                existing.Stock = newValues.Stock;
                hasChanges = true;
            }

            if (existing.Barcode != newValues.Barcode)
            {
                existing.Barcode = newValues.Barcode;
                hasChanges = true;
            }

            if (existing.Currency != newValues.Currency && !string.IsNullOrEmpty(newValues.Currency))
            {
                existing.Currency = newValues.Currency;
                hasChanges = true;
            }

            if (existing.WeightGrams != newValues.WeightGrams)
            {
                existing.WeightGrams = newValues.WeightGrams;
                hasChanges = true;
            }

            if (existing.VolumeML != newValues.VolumeML)
            {
                existing.VolumeML = newValues.VolumeML;
                hasChanges = true;
            }

            if (existing.SupplierCode != newValues.SupplierCode)
            {
                existing.SupplierCode = newValues.SupplierCode;
                hasChanges = true;
            }

            if (existing.ParentSku != newValues.ParentSku)
            {
                existing.ParentSku = newValues.ParentSku;
                hasChanges = true;
            }

            return hasChanges;
        }

        /// <inheritdoc/>
        public async Task<int> BulkUpdateStockAsync(Dictionary<string, int> stockUpdates)
        {
            if (stockUpdates == null || stockUpdates.Count == 0)
                return 0;

            int updatedCount = 0;
            var skus = stockUpdates.Keys.ToList();
            var variants = await GetBySkusAsync(skus);

            foreach (var kvp in stockUpdates)
            {
                if (variants.TryGetValue(kvp.Key, out var variant))
                {
                    if (variant.Stock != kvp.Value)
                    {
                        variant.Stock = kvp.Value;
                        variant.UpdatedAt = DateTime.UtcNow;
                        variant.LastSyncedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return updatedCount;
        }

        /// <inheritdoc/>
        public async Task<int> BulkDeactivateAsync(IEnumerable<int> variantIds)
        {
            var ids = variantIds?.ToList() ?? new List<int>();
            if (ids.Count == 0)
                return 0;

            // ExecuteUpdate kullanarak tek sorguda güncelle (EF Core 7+)
            var now = DateTime.UtcNow;
            
            var variants = await _dbSet
                .Where(v => ids.Contains(v.Id) && v.IsActive)
                .ToListAsync();

            foreach (var variant in variants)
            {
                variant.IsActive = false;
                variant.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return variants.Count;
        }

        #endregion

        #region Senkronizasyon

        /// <inheritdoc/>
        public async Task MarkAsSeenAsync(int variantId, DateTime? seenAt = null)
        {
            var variant = await _dbSet.FindAsync(variantId);
            if (variant != null)
            {
                variant.LastSeenAt = seenAt ?? DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task BulkMarkAsSeenAsync(IEnumerable<int> variantIds, DateTime? seenAt = null)
        {
            var ids = variantIds?.ToList() ?? new List<int>();
            if (ids.Count == 0)
                return;

            var now = seenAt ?? DateTime.UtcNow;
            var variants = await _dbSet
                .Where(v => ids.Contains(v.Id))
                .ToListAsync();

            foreach (var variant in variants)
            {
                variant.LastSeenAt = now;
            }

            await _context.SaveChangesAsync();
        }

        #endregion

        #region İstatistikler

        /// <inheritdoc/>
        public async Task<int> GetCountByProductIdAsync(int productId)
        {
            return await _dbSet
                .CountAsync(v => v.ProductId == productId && v.IsActive);
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalStockByProductIdAsync(int productId)
        {
            return await _dbSet
                .Where(v => v.ProductId == productId && v.IsActive)
                .SumAsync(v => v.Stock);
        }

        /// <inheritdoc/>
        public async Task<bool> SkuExistsAsync(string sku, int? excludeVariantId = null)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return false;

            var query = _dbSet.Where(v => v.SKU == sku);

            if (excludeVariantId.HasValue)
            {
                query = query.Where(v => v.Id != excludeVariantId.Value);
            }

            return await query.AnyAsync();
        }

        #endregion
    }
}
