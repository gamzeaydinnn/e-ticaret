// ProductOptionRepository: Ürün seçenekleri için repository implementasyonu.
// GetOrCreate pattern ile thread-safe seçenek/değer yönetimi sağlar.
// XML import'ta dinamik seçenek oluşturma için optimize edilmiştir.

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
    /// Ürün seçenekleri için repository implementasyonu.
    /// GetOrCreate pattern ile eksik seçenekleri otomatik oluşturur.
    /// </summary>
    public class ProductOptionRepository : BaseRepository<ProductOption>, IProductOptionRepository
    {
        // Concurrency için lock objesi
        private static readonly object _lockObject = new object();

        public ProductOptionRepository(ECommerceDbContext context) : base(context)
        {
        }

        #region Option Sorguları

        /// <inheritdoc/>
        public async Task<ProductOption?> GetByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Case-insensitive arama (Turkish collation)
            return await _dbSet
                .FirstOrDefaultAsync(o => o.Name == name.Trim());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductOption>> GetAllWithValuesAsync(bool includeInactive = false)
        {
            var query = _dbSet
                .Include(o => o.OptionValues.OrderBy(v => v.DisplayOrder))
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            return await query
                .OrderBy(o => o.DisplayOrder)
                .ThenBy(o => o.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ProductOption?> GetByIdWithValuesAsync(int optionId)
        {
            return await _dbSet
                .Include(o => o.OptionValues.Where(v => v.IsActive).OrderBy(v => v.DisplayOrder))
                .FirstOrDefaultAsync(o => o.Id == optionId);
        }

        #endregion

        #region GetOrCreate Pattern

        /// <inheritdoc/>
        public async Task<ProductOption> GetOrCreateOptionAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Seçenek adı boş olamaz", nameof(name));

            var trimmedName = name.Trim();

            // Önce mevcut kaydı ara
            var existing = await GetByNameAsync(trimmedName);
            if (existing != null)
                return existing;

            // Mevcut değilse oluştur
            // Concurrency sorunları için try-catch
            try
            {
                var newOption = new ProductOption
                {
                    Name = trimmedName,
                    DisplayOrder = await GetNextDisplayOrderAsync(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _dbSet.AddAsync(newOption);
                await _context.SaveChangesAsync();
                return newOption;
            }
            catch (DbUpdateException)
            {
                // Unique constraint hatası - başka bir thread oluşturmuş olabilir
                // Tekrar sorgula
                var retryResult = await GetByNameAsync(trimmedName);
                if (retryResult != null)
                    return retryResult;

                throw; // Başka bir hata
            }
        }

        /// <inheritdoc/>
        public async Task<ProductOptionValue> GetOrCreateValueAsync(int optionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Değer boş olamaz", nameof(value));

            var trimmedValue = value.Trim();

            // Önce mevcut değeri ara
            var existing = await _context.ProductOptionValues
                .FirstOrDefaultAsync(v => v.OptionId == optionId && v.Value == trimmedValue);

            if (existing != null)
                return existing;

            // Mevcut değilse oluştur
            try
            {
                var newValue = new ProductOptionValue
                {
                    OptionId = optionId,
                    Value = trimmedValue,
                    DisplayOrder = await GetNextValueDisplayOrderAsync(optionId),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.ProductOptionValues.AddAsync(newValue);
                await _context.SaveChangesAsync();
                return newValue;
            }
            catch (DbUpdateException)
            {
                // Unique constraint hatası - tekrar sorgula
                var retryResult = await _context.ProductOptionValues
                    .FirstOrDefaultAsync(v => v.OptionId == optionId && v.Value == trimmedValue);

                if (retryResult != null)
                    return retryResult;

                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ProductOptionValue> GetOrCreateOptionValueAsync(string optionName, string value)
        {
            // Önce seçeneği getir/oluştur
            var option = await GetOrCreateOptionAsync(optionName);
            
            // Sonra değeri getir/oluştur
            return await GetOrCreateValueAsync(option.Id, value);
        }

        /// <summary>
        /// Bir sonraki DisplayOrder değerini hesaplar.
        /// </summary>
        private async Task<int> GetNextDisplayOrderAsync()
        {
            var maxOrder = await _dbSet.MaxAsync(o => (int?)o.DisplayOrder) ?? 0;
            return maxOrder + 1;
        }

        /// <summary>
        /// Bir seçenek için sonraki değer DisplayOrder'ını hesaplar.
        /// </summary>
        private async Task<int> GetNextValueDisplayOrderAsync(int optionId)
        {
            var maxOrder = await _context.ProductOptionValues
                .Where(v => v.OptionId == optionId)
                .MaxAsync(v => (int?)v.DisplayOrder) ?? 0;
            return maxOrder + 1;
        }

        #endregion

        #region OptionValue Sorguları

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductOptionValue>> GetValuesByOptionIdAsync(int optionId, bool includeInactive = false)
        {
            var query = _context.ProductOptionValues
                .Where(v => v.OptionId == optionId);

            if (!includeInactive)
            {
                query = query.Where(v => v.IsActive);
            }

            return await query
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<ProductOptionValue?> GetValueByIdAsync(int valueId)
        {
            return await _context.ProductOptionValues
                .Include(v => v.Option)
                .FirstOrDefaultAsync(v => v.Id == valueId);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductOptionValue>> GetValuesByIdsAsync(IEnumerable<int> valueIds)
        {
            var ids = valueIds?.ToList() ?? new List<int>();
            if (ids.Count == 0)
                return Enumerable.Empty<ProductOptionValue>();

            return await _context.ProductOptionValues
                .Include(v => v.Option)
                .Where(v => ids.Contains(v.Id))
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductOptionValue>> SearchValuesAsync(string searchTerm, int? optionId = null, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductOptionValue>();

            var query = _context.ProductOptionValues
                .Include(v => v.Option)
                .Where(v => v.IsActive && v.Value.Contains(searchTerm));

            if (optionId.HasValue)
            {
                query = query.Where(v => v.OptionId == optionId.Value);
            }

            return await query
                .Take(limit)
                .ToListAsync();
        }

        #endregion

        #region OptionValue CRUD

        /// <inheritdoc/>
        public async Task<ProductOptionValue> AddValueAsync(ProductOptionValue value)
        {
            value.CreatedAt = DateTime.UtcNow;
            await _context.ProductOptionValues.AddAsync(value);
            await _context.SaveChangesAsync();
            return value;
        }

        /// <inheritdoc/>
        public async Task UpdateValueAsync(ProductOptionValue value)
        {
            value.UpdatedAt = DateTime.UtcNow;
            _context.ProductOptionValues.Update(value);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteValueAsync(int valueId)
        {
            var value = await _context.ProductOptionValues.FindAsync(valueId);
            if (value != null)
            {
                value.IsActive = false;
                value.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region VariantOptionValue İşlemleri

        /// <inheritdoc/>
        public async Task AssignOptionValueToVariantAsync(int variantId, int optionValueId)
        {
            // Zaten atanmış mı kontrol et
            var exists = await _context.VariantOptionValues
                .AnyAsync(vov => vov.VariantId == variantId && vov.OptionValueId == optionValueId);

            if (!exists)
            {
                var assignment = new VariantOptionValue
                {
                    VariantId = variantId,
                    OptionValueId = optionValueId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.VariantOptionValues.AddAsync(assignment);
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task SetVariantOptionValuesAsync(int variantId, IEnumerable<int> optionValueIds)
        {
            var valueIds = optionValueIds?.ToList() ?? new List<int>();

            // Mevcut atamaları sil
            var existingAssignments = await _context.VariantOptionValues
                .Where(vov => vov.VariantId == variantId)
                .ToListAsync();

            _context.VariantOptionValues.RemoveRange(existingAssignments);

            // Yeni atamaları ekle
            if (valueIds.Count > 0)
            {
                var newAssignments = valueIds.Select(valueId => new VariantOptionValue
                {
                    VariantId = variantId,
                    OptionValueId = valueId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.VariantOptionValues.AddRangeAsync(newAssignments);
            }

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task RemoveOptionValueFromVariantAsync(int variantId, int optionValueId)
        {
            var assignment = await _context.VariantOptionValues
                .FirstOrDefaultAsync(vov => vov.VariantId == variantId && vov.OptionValueId == optionValueId);

            if (assignment != null)
            {
                _context.VariantOptionValues.Remove(assignment);
                await _context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductOptionValue>> GetVariantOptionValuesAsync(int variantId)
        {
            return await _context.VariantOptionValues
                .Where(vov => vov.VariantId == variantId)
                .Include(vov => vov.OptionValue)
                    .ThenInclude(ov => ov.Option)
                .Select(vov => vov.OptionValue)
                .ToListAsync();
        }

        #endregion

        #region İstatistikler

        /// <inheritdoc/>
        public async Task<int> GetOptionCountAsync(bool includeInactive = false)
        {
            var query = _dbSet.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(o => o.IsActive);
            }

            return await query.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> GetValueCountByOptionIdAsync(int optionId, bool includeInactive = false)
        {
            var query = _context.ProductOptionValues
                .Where(v => v.OptionId == optionId);

            if (!includeInactive)
            {
                query = query.Where(v => v.IsActive);
            }

            return await query.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<int> GetValueUsageCountAsync(int optionValueId)
        {
            return await _context.VariantOptionValues
                .CountAsync(vov => vov.OptionValueId == optionValueId);
        }

        #endregion
    }
}
