using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Kampanya repository implementasyonu.
    /// Kampanya ve kampanya hedefleri için veritabanı operasyonları.
    /// </summary>
    public class CampaignRepository : BaseRepository<Campaign>, ICampaignRepository
    {
        public CampaignRepository(ECommerceDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Tüm AKTİF kampanyaları hedefleriyle birlikte getirir.
        /// Soft delete yapılan kampanyalar (IsActive = false) listelenmez.
        /// </summary>
        public override async Task<IEnumerable<Campaign>> GetAllAsync()
        {
            return await _context.Campaigns
                .Where(c => c.IsActive) // Sadece aktif (silinmemiş) kampanyalar
                .Include(c => c.Targets)
                #pragma warning disable CS0618 // Geriye dönük uyumluluk için eski alanları da dahil ediyoruz
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Tüm kampanyaları (silinen dahil) hedefleriyle birlikte getirir.
        /// Admin panelde silinenleri de görmek için kullanılır.
        /// </summary>
        public async Task<IEnumerable<Campaign>> GetAllIncludingDeletedAsync()
        {
            return await _context.Campaigns
                .Include(c => c.Targets)
                #pragma warning disable CS0618
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// ID'ye göre aktif kampanya getirir (hedefler dahil).
        /// Soft delete yapılmış kampanyalar için null döner.
        /// </summary>
        public override async Task<Campaign?> GetByIdAsync(int id)
        {
            return await _context.Campaigns
                .Where(c => c.IsActive) // Silinmiş kampanyaları getirme
                .Include(c => c.Targets)
                #pragma warning disable CS0618
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// ID'ye göre kampanya getirir (silinen dahil).
        /// Hard delete veya restore işlemleri için kullanılır.
        /// </summary>
        public async Task<Campaign?> GetByIdIncludingDeletedAsync(int id)
        {
            return await _context.Campaigns
                .Include(c => c.Targets)
                #pragma warning disable CS0618
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// ID'ye göre kampanya getirir (sadece hedeflerle)
        /// </summary>
        public async Task<Campaign?> GetByIdWithTargetsAsync(int id)
        {
            return await _context.Campaigns
                .Include(c => c.Targets)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Belirtilen tarihte aktif olan kampanyaları getirir
        /// Öncelik sırasına göre sıralı döner
        /// </summary>
        public async Task<List<Campaign>> GetActiveCampaignsAsync(DateTime? now = null)
        {
            var current = now ?? DateTime.UtcNow;

            return await _context.Campaigns
                .Include(c => c.Targets)
                #pragma warning disable CS0618
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .Where(c => c.IsActive
                            && c.StartDate <= current
                            && c.EndDate >= current)
                .OrderBy(c => c.Priority) // Düşük öncelik = yüksek sıra
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Belirli bir ürün için geçerli kampanyaları getirir.
        /// Üç durum kontrol edilir:
        /// 1. TargetType = All (tüm ürünlere uygulanır)
        /// 2. TargetType = Product ve hedefler arasında bu ürün var
        /// 3. TargetType = Category ve hedefler arasında bu kategori var
        /// </summary>
        public async Task<List<Campaign>> GetCampaignsForProductAsync(int productId, int categoryId, DateTime? now = null)
        {
            var current = now ?? DateTime.UtcNow;

            // Aktif kampanyaları getir
            var activeCampaigns = await _context.Campaigns
                .Include(c => c.Targets)
                .Where(c => c.IsActive
                            && c.StartDate <= current
                            && c.EndDate >= current)
                .OrderBy(c => c.Priority)
                .ToListAsync();

            // Ürün için geçerli kampanyaları filtrele
            // Memory'de filtreleme yapıyoruz çünkü koşullar karmaşık
            return activeCampaigns.Where(c =>
                // Tüm ürünlere uygulanan kampanyalar
                c.TargetType == CampaignTargetType.All ||
                // Ürün bazlı kampanyalar
                (c.TargetType == CampaignTargetType.Product &&
                 c.Targets.Any(t => t.TargetKind == CampaignTargetKind.Product && t.TargetId == productId)) ||
                // Kategori bazlı kampanyalar
                (c.TargetType == CampaignTargetType.Category &&
                 c.Targets.Any(t => t.TargetKind == CampaignTargetKind.Category && t.TargetId == categoryId))
            ).ToList();
        }

        /// <summary>
        /// Belirli türdeki aktif kampanyaları getirir
        /// Örn: Sadece FreeShipping kampanyaları
        /// </summary>
        public async Task<List<Campaign>> GetActiveCampaignsByTypeAsync(CampaignType type, DateTime? now = null)
        {
            var current = now ?? DateTime.UtcNow;

            return await _context.Campaigns
                .Include(c => c.Targets)
                .Where(c => c.IsActive
                            && c.Type == type
                            && c.StartDate <= current
                            && c.EndDate >= current)
                .OrderBy(c => c.Priority)
                .ToListAsync();
        }

        /// <summary>
        /// Kampanya hedeflerini günceller.
        /// Mevcut hedefleri siler ve yenilerini ekler (replace stratejisi).
        /// </summary>
        public async Task UpdateTargetsAsync(int campaignId, IEnumerable<int> targetIds, CampaignTargetKind targetKind)
        {
            // Mevcut hedefleri sil
            await ClearTargetsAsync(campaignId);

            // Yeni hedefleri ekle
            var targets = targetIds.Select(id => new CampaignTarget
            {
                CampaignId = campaignId,
                TargetId = id,
                TargetKind = targetKind,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.CampaignTargets.AddRangeAsync(targets);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kampanya hedeflerini temizler
        /// </summary>
        public async Task ClearTargetsAsync(int campaignId)
        {
            var existingTargets = await _context.CampaignTargets
                .Where(t => t.CampaignId == campaignId)
                .ToListAsync();

            if (existingTargets.Any())
            {
                _context.CampaignTargets.RemoveRange(existingTargets);
                await _context.SaveChangesAsync();
            }
        }
    }
}

