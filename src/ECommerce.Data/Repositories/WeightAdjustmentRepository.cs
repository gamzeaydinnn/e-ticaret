using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Ağırlık fark yönetimi repository implementasyonu
    /// Tüm WeightAdjustment veri erişim operasyonlarını gerçekleştirir
    /// </summary>
    public class WeightAdjustmentRepository : BaseRepository<WeightAdjustment>, IWeightAdjustmentRepository
    {
        private readonly ILogger<WeightAdjustmentRepository>? _logger;

        public WeightAdjustmentRepository(
            ECommerceDbContext context,
            ILogger<WeightAdjustmentRepository>? logger = null) : base(context)
        {
            _logger = logger;
        }

        #region Temel Sorgular

        /// <summary>
        /// Sipariş ID'sine göre tüm ağırlık fark kayıtlarını getirir
        /// Include ile ilişkili entity'ler de yüklenir (performans için AsNoTracking)
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetByOrderIdAsync(int orderId)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.OrderItem)
                    .Include(wa => wa.Product)
                    .Where(wa => wa.OrderId == orderId && wa.IsActive)
                    .OrderBy(wa => wa.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Sipariş ağırlık kayıtları alınamadı. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Sipariş kalemi ID'sine göre ağırlık fark kaydını getirir
        /// Her sipariş kalemi için tek kayıt olacağı garanti edilir
        /// </summary>
        public async Task<WeightAdjustment?> GetByOrderItemIdAsync(int orderItemId)
        {
            try
            {
                return await _dbSet
                    .Include(wa => wa.Order)
                    .Include(wa => wa.OrderItem)
                    .Include(wa => wa.Product)
                    .FirstOrDefaultAsync(wa => wa.OrderItemId == orderItemId && wa.IsActive);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Sipariş kalemi ağırlık kaydı alınamadı. OrderItemId: {OrderItemId}", orderItemId);
                throw;
            }
        }

        /// <summary>
        /// Kurye ID'sine göre tartı kayıtlarını getirir
        /// Son 30 gün ile sınırlı - performans optimizasyonu
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetByCourierIdAsync(int courierId)
        {
            try
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                
                return await _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.Order)
                    .Include(wa => wa.Product)
                    .Where(wa => wa.WeighedByCourierId == courierId 
                              && wa.IsActive 
                              && wa.CreatedAt >= thirtyDaysAgo)
                    .OrderByDescending(wa => wa.WeighedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Kurye tartı kayıtları alınamadı. CourierId: {CourierId}", courierId);
                throw;
            }
        }

        #endregion

        #region Durum Bazlı Sorgular

        /// <summary>
        /// Belirli duruma sahip kayıtları getirir
        /// Index kullanımı için Status alanında index mevcut
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetByStatusAsync(WeightAdjustmentStatus status)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.Order)
                    .Include(wa => wa.Product)
                    .Where(wa => wa.Status == status && wa.IsActive)
                    .OrderByDescending(wa => wa.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Durum bazlı kayıtlar alınamadı. Status: {Status}", status);
                throw;
            }
        }

        /// <summary>
        /// Admin onayı bekleyen kayıtları getirir
        /// Öncelikli işlem için CreatedAt'e göre sıralı
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetPendingAdminApprovalAsync()
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.Order)
                    .Include(wa => wa.OrderItem)
                    .Include(wa => wa.Product)
                    .Where(wa => wa.RequiresAdminApproval 
                              && !wa.AdminReviewed 
                              && wa.IsActive)
                    .OrderBy(wa => wa.CreatedAt) // FIFO - İlk gelen ilk işlenir
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Admin onayı bekleyen kayıtlar alınamadı");
                throw;
            }
        }

        /// <summary>
        /// Ödeme/iade bekleyen kayıtları getirir
        /// Otomatik ödeme işlemi için kullanılır
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetPendingSettlementAsync()
        {
            try
            {
                var pendingStatuses = new[]
                {
                    WeightAdjustmentStatus.PendingAdditionalPayment,
                    WeightAdjustmentStatus.PendingRefund
                };

                return await _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.Order)
                    .Include(wa => wa.OrderItem)
                    .Where(wa => pendingStatuses.Contains(wa.Status) 
                              && !wa.IsSettled 
                              && wa.IsActive)
                    .OrderBy(wa => wa.WeighedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Ödeme bekleyen kayıtlar alınamadı");
                throw;
            }
        }

        /// <summary>
        /// Tartı bekleyen sipariş kalemlerini getirir
        /// Kurye paneli tartı ekranı için
        /// </summary>
        public async Task<IEnumerable<WeightAdjustment>> GetPendingWeighingByOrderIdAsync(int orderId)
        {
            try
            {
                return await _dbSet
                    .Include(wa => wa.OrderItem)
                    .Include(wa => wa.Product)
                    .Where(wa => wa.OrderId == orderId 
                              && wa.Status == WeightAdjustmentStatus.PendingWeighing 
                              && wa.IsActive)
                    .OrderBy(wa => wa.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Tartı bekleyen kalemler alınamadı. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Filtreleme ve Sayfalama

        /// <summary>
        /// Filtreleme kriterleriyle sayfalanmış liste döner
        /// Dinamik sorgu oluşturma - SOLID (Open/Closed) prensibi
        /// </summary>
        public async Task<PagedResult<WeightAdjustment>> GetFilteredAsync(WeightAdjustmentFilterDto filter)
        {
            try
            {
                // Temel sorgu
                var query = _dbSet
                    .AsNoTracking()
                    .Include(wa => wa.Order)
                    .Include(wa => wa.Product)
                    .Include(wa => wa.WeighedByCourier)
                    .Where(wa => wa.IsActive);

                // Durum filtresi
                if (filter.Status.HasValue)
                {
                    query = query.Where(wa => wa.Status == filter.Status.Value);
                }

                // Admin onayı filtresi
                if (filter.RequiresAdminApproval.HasValue)
                {
                    query = query.Where(wa => wa.RequiresAdminApproval == filter.RequiresAdminApproval.Value 
                                           && !wa.AdminReviewed);
                }

                // Ödeme bekleyen filtresi
                if (filter.PendingSettlement.HasValue && filter.PendingSettlement.Value)
                {
                    query = query.Where(wa => !wa.IsSettled);
                }

                // Tarih aralığı filtresi
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(wa => wa.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    // Bitiş tarihini gün sonuna ayarla
                    var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(wa => wa.CreatedAt <= endOfDay);
                }

                // Sipariş numarası araması
                if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
                {
                    query = query.Where(wa => wa.Order != null 
                                           && wa.Order.OrderNumber.Contains(filter.OrderNumber));
                }

                // Kurye filtresi
                if (filter.CourierId.HasValue)
                {
                    query = query.Where(wa => wa.WeighedByCourierId == filter.CourierId.Value);
                }

                // Toplam sayı hesaplama (sayfalama öncesi)
                var totalCount = await query.CountAsync();

                // Sayfalama ve sıralama
                var skip = (filter.Page - 1) * filter.PageSize;
                var items = await query
                    .OrderByDescending(wa => wa.CreatedAt)
                    .Skip(skip)
                    .Take(filter.PageSize)
                    .ToListAsync();

                return new PagedResult<WeightAdjustment>(items, totalCount, skip, filter.PageSize);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Filtrelenmiş liste alınamadı");
                throw;
            }
        }

        #endregion

        #region İstatistikler

        /// <summary>
        /// Dashboard için istatistikleri hesaplar
        /// Aggregate fonksiyonları tek sorguda çalıştırılır - performans
        /// </summary>
        public async Task<WeightAdjustmentStatsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var todayStart = now.Date;
                var weekStart = now.AddDays(-(int)now.DayOfWeek).Date;

                // Base query
                var query = _dbSet.AsNoTracking().Where(wa => wa.IsActive);

                // Tarih filtresi uygula
                if (startDate.HasValue)
                    query = query.Where(wa => wa.CreatedAt >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(wa => wa.CreatedAt <= endDate.Value);

                // Tek sorguda tüm istatistikleri al
                var stats = await query
                    .GroupBy(wa => 1)
                    .Select(g => new WeightAdjustmentStatsDto
                    {
                        TotalCount = g.Count(),
                        PendingWeighingCount = g.Count(wa => wa.Status == WeightAdjustmentStatus.PendingWeighing),
                        PendingAdminApprovalCount = g.Count(wa => wa.RequiresAdminApproval && !wa.AdminReviewed),
                        PendingSettlementCount = g.Count(wa => 
                            (wa.Status == WeightAdjustmentStatus.PendingAdditionalPayment 
                             || wa.Status == WeightAdjustmentStatus.PendingRefund) 
                            && !wa.IsSettled),
                        CompletedCount = g.Count(wa => wa.Status == WeightAdjustmentStatus.Completed),
                        TotalAdditionalPayments = g.Where(wa => wa.PriceDifference > 0 && wa.IsSettled)
                                                   .Sum(wa => wa.PriceDifference),
                        TotalRefunds = g.Where(wa => wa.PriceDifference < 0 && wa.IsSettled)
                                        .Sum(wa => Math.Abs(wa.PriceDifference)),
                        TodayCount = g.Count(wa => wa.CreatedAt >= todayStart),
                        ThisWeekCount = g.Count(wa => wa.CreatedAt >= weekStart)
                    })
                    .FirstOrDefaultAsync();

                return stats ?? new WeightAdjustmentStatsDto();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] İstatistikler hesaplanamadı");
                throw;
            }
        }

        /// <summary>
        /// Belirli tarih aralığındaki toplam fark tutarını hesaplar
        /// Finansal raporlama için kullanılır
        /// </summary>
        public async Task<decimal> GetTotalDifferenceAmountAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Where(wa => wa.IsActive 
                              && wa.IsSettled 
                              && wa.SettledAt >= startDate 
                              && wa.SettledAt <= endDate)
                    .SumAsync(wa => wa.PriceDifference);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Toplam fark tutarı hesaplanamadı");
                throw;
            }
        }

        /// <summary>
        /// Sipariş için toplam fark tutarını hesaplar
        /// </summary>
        public async Task<decimal> GetOrderTotalDifferenceAsync(int orderId)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Where(wa => wa.OrderId == orderId && wa.IsActive)
                    .SumAsync(wa => wa.PriceDifference);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Sipariş fark tutarı hesaplanamadı. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Toplu İşlemler

        /// <summary>
        /// Toplu kayıt ekleme
        /// Sipariş oluşturulduğunda tüm ağırlık bazlı ürünler için kayıt oluşturulur
        /// Transaction içinde çağrılmalı
        /// </summary>
        public async Task AddRangeAsync(IEnumerable<WeightAdjustment> adjustments)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var adj in adjustments)
                {
                    adj.CreatedAt = now;
                    adj.IsActive = true;
                }

                await _dbSet.AddRangeAsync(adjustments);
                await _context.SaveChangesAsync();

                _logger?.LogInformation(
                    "[WEIGHT-ADJ-REPO] ✅ Toplu kayıt eklendi. Adet: {Count}", 
                    adjustments.Count());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] ❌ Toplu kayıt eklenemedi");
                throw;
            }
        }

        /// <summary>
        /// Sipariş için tüm kayıtların durumunu günceller
        /// Sipariş iptal edildiğinde kullanılır
        /// </summary>
        public async Task UpdateStatusByOrderIdAsync(int orderId, WeightAdjustmentStatus status)
        {
            try
            {
                var adjustments = await _dbSet
                    .Where(wa => wa.OrderId == orderId && wa.IsActive)
                    .ToListAsync();

                var now = DateTime.UtcNow;
                foreach (var adj in adjustments)
                {
                    adj.Status = status;
                    adj.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();

                _logger?.LogInformation(
                    "[WEIGHT-ADJ-REPO] ✅ Sipariş kayıtları güncellendi. OrderId: {OrderId}, Status: {Status}, Adet: {Count}", 
                    orderId, status, adjustments.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] ❌ Sipariş kayıtları güncellenemedi. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        #endregion

        #region Doğrulama

        /// <summary>
        /// Siparişin tüm ağırlık bazlı ürünlerinin tartılıp tartılmadığını kontrol eder
        /// Teslimat kesinleştirme öncesi kontrol için kritik
        /// </summary>
        public async Task<bool> AreAllItemsWeighedAsync(int orderId)
        {
            try
            {
                // Tartı bekleyen kayıt var mı kontrol et
                var hasPending = await _dbSet
                    .AsNoTracking()
                    .AnyAsync(wa => wa.OrderId == orderId 
                                 && wa.Status == WeightAdjustmentStatus.PendingWeighing 
                                 && wa.IsActive);

                return !hasPending;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Tartı durumu kontrol edilemedi. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Siparişte admin onayı bekleyen kayıt var mı kontrol eder
        /// </summary>
        public async Task<bool> HasPendingAdminApprovalAsync(int orderId)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .AnyAsync(wa => wa.OrderId == orderId 
                                 && wa.RequiresAdminApproval 
                                 && !wa.AdminReviewed 
                                 && wa.IsActive);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[WEIGHT-ADJ-REPO] Admin onay durumu kontrol edilemedi. OrderId: {OrderId}", orderId);
                throw;
            }
        }

        #endregion
    }
}
