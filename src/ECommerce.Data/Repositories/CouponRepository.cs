// =============================================================================
// CouponRepository - Kupon Repository Implementasyonu
// =============================================================================
// Bu repository, kupon ve kupon kullanım verilerine erişim sağlar.
// Entity Framework Core kullanarak veritabanı işlemlerini gerçekleştirir.
// =============================================================================

using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Kupon repository implementasyonu.
    /// Kupon ve CouponUsage veritabanı işlemlerini gerçekleştirir.
    /// </summary>
    public class CouponRepository : BaseRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(ECommerceDbContext context) : base(context) { }

        // =============================================================================
        // Kupon Sorgulama
        // =============================================================================

        /// <inheritdoc />
        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            // Case-insensitive arama - IsActive kontrolü ValidateCouponAsync'te yapılıyor
            var normalizedCode = code.Trim().ToUpperInvariant();
            return await _dbSet
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<Coupon?> GetByCodeWithDetailsAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var normalizedCode = code.Trim().ToUpperInvariant();
            return await _dbSet
                .Include(c => c.Category)
                .Include(c => c.CouponProducts)
                    .ThenInclude(cp => cp.Product)
                .Include(c => c.CouponUsages)
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(c => c.IsActive 
                    && c.ExpirationDate > now
                    && (c.StartDate == null || c.StartDate <= now))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Coupon>> GetByCategoryIdAsync(int categoryId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Where(c => c.IsActive 
                    && c.ExpirationDate > now
                    && (c.StartDate == null || c.StartDate <= now)
                    && c.CategoryId == categoryId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Coupon>> GetByProductIdAsync(int productId)
        {
            var now = DateTime.UtcNow;
            return await _dbSet
                .Include(c => c.CouponProducts)
                .Where(c => c.IsActive 
                    && c.ExpirationDate > now
                    && (c.StartDate == null || c.StartDate <= now)
                    && c.CouponProducts.Any(cp => cp.ProductId == productId))
                .ToListAsync();
        }

        // =============================================================================
        // Kupon Kullanım İşlemleri
        // =============================================================================

        /// <inheritdoc />
        public async Task AddCouponUsageAsync(CouponUsage usage)
        {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));

            await _context.Set<CouponUsage>().AddAsync(usage);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<int> GetUserUsageCountAsync(int couponId, int? userId)
        {
            if (!userId.HasValue)
                return 0;

            return await _context.Set<CouponUsage>()
                .CountAsync(cu => cu.CouponId == couponId && cu.UserId == userId.Value);
        }

        /// <inheritdoc />
        public async Task<int> GetTotalUsageCountAsync(int couponId)
        {
            return await _context.Set<CouponUsage>()
                .CountAsync(cu => cu.CouponId == couponId);
        }

        /// <inheritdoc />
        public async Task<CouponUsage?> GetUsageByOrderIdAsync(int orderId)
        {
            return await _context.Set<CouponUsage>()
                .Include(cu => cu.Coupon)
                .FirstOrDefaultAsync(cu => cu.OrderId == orderId);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CouponUsage>> GetUsageHistoryAsync(int couponId, int take = 50)
        {
            return await _context.Set<CouponUsage>()
                .Include(cu => cu.User)
                .Include(cu => cu.Order)
                .Where(cu => cu.CouponId == couponId)
                .OrderByDescending(cu => cu.UsedAt)
                .Take(take)
                .ToListAsync();
        }
    }
}
