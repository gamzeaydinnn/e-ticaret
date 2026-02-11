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
            // Admin panelde tarihler yerel saat (Türkiye UTC+3) ile kaydediliyor
            // Bu yüzden hem UTC hem yerel saat ile kontrol yapıyoruz
            var currentUtc = now ?? DateTime.UtcNow;
            var currentLocal = now ?? DateTime.Now;

            return await _context.Campaigns
                .Include(c => c.Targets)
                #pragma warning disable CS0618
                .Include(c => c.Rules)
                .Include(c => c.Rewards)
                #pragma warning restore CS0618
                .Where(c => c.IsActive
                            && (
                                // UTC ile kontrol
                                (c.StartDate <= currentUtc && c.EndDate >= currentUtc)
                                ||
                                // Yerel saat ile kontrol (tarihler yerel saat olarak kaydedilmişse)
                                (c.StartDate <= currentLocal && c.EndDate >= currentLocal)
                                ||
                                // Sadece tarih bazlı kontrol (saat farkı sorunu için)
                                (c.StartDate.Date <= currentLocal.Date && c.EndDate.Date >= currentLocal.Date)
                            ))
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
            var currentUtc = now ?? DateTime.UtcNow;
            var currentLocal = now ?? DateTime.Now;

            // Aktif kampanyaları getir
            var activeCampaigns = await _context.Campaigns
                .Include(c => c.Targets)
                .Where(c => c.IsActive
                            && (
                                (c.StartDate <= currentUtc && c.EndDate >= currentUtc)
                                || (c.StartDate <= currentLocal && c.EndDate >= currentLocal)
                                || (c.StartDate.Date <= currentLocal.Date && c.EndDate.Date >= currentLocal.Date)
                            ))
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
            var currentUtc = now ?? DateTime.UtcNow;
            var currentLocal = now ?? DateTime.Now;

            return await _context.Campaigns
                .Include(c => c.Targets)
                .Where(c => c.IsActive
                            && c.Type == type
                            && (
                                (c.StartDate <= currentUtc && c.EndDate >= currentUtc)
                                || (c.StartDate <= currentLocal && c.EndDate >= currentLocal)
                                || (c.StartDate.Date <= currentLocal.Date && c.EndDate.Date >= currentLocal.Date)
                            ))
                .OrderBy(c => c.Priority)
                .ToListAsync();
        }

        /// <summary>
        /// Kampanya hedeflerini günceller (TRANSACTIONAL).
        /// Mevcut hedefleri siler ve yenilerini ekler.
        /// Atomicity sağlamak için transaction kullanır.
        /// </summary>
        public async Task UpdateTargetsAsync(int campaignId, IEnumerable<int> targetIds, CampaignTargetKind targetKind)
        {
            // Transaction başlat - atomicity sağla
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Mevcut hedefleri sil (SaveChanges YOK)
                await ClearTargetsInternalAsync(campaignId);

                // 2. Yeni hedefleri ekle (SaveChanges YOK)
                var targets = targetIds.Select(id => new CampaignTarget
                {
                    CampaignId = campaignId,
                    TargetId = id,
                    TargetKind = targetKind,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.CampaignTargets.AddRangeAsync(targets);

                // 3. Tüm değişiklikleri birlikte commit et
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                // Hata oluşursa rollback yap
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Internal metod: Hedefleri temizler (SaveChanges yapmaz).
        /// Transaction içinde kullanılmak üzere tasarlanmıştır.
        /// </summary>
        private async Task ClearTargetsInternalAsync(int campaignId)
        {
            var existingTargets = await _context.CampaignTargets
                .Where(t => t.CampaignId == campaignId)
                .ToListAsync();

            if (existingTargets.Any())
            {
                _context.CampaignTargets.RemoveRange(existingTargets);
                // SaveChangesAsync ÇAĞRILMIYOR - transaction içinde yapılacak
            }
        }

        /// <summary>
        /// Kampanya hedeflerini temizler (PUBLIC - transaction dışında kullanım için).
        /// Backward compatibility için korunmuştur.
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

        /// <summary>
        /// Kampanyayı hedefleriyle birlikte oluşturur (TRANSACTIONAL).
        /// Atomicity garantisi sağlar - ya her şey başarılı olur, ya hiçbir şey olmaz.
        /// </summary>
        /// <param name="campaign">Oluşturulacak kampanya</param>
        /// <param name="targetIds">Hedef ID'leri (ürün veya kategori)</param>
        /// <param name="targetKind">Hedef türü (Ürün veya Kategori)</param>
        /// <returns>Oluşturulan kampanya (ID ile birlikte)</returns>
        public async Task<Campaign> CreateCampaignWithTargetsAsync(
            Campaign campaign,
            IEnumerable<int>? targetIds,
            CampaignTargetKind targetKind)
        {
            // Transaction başlat
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Kampanyayı oluştur
                await _context.Campaigns.AddAsync(campaign);
                await _context.SaveChangesAsync(); // ID generate edilsin

                // 2. Hedefler varsa ekle
                if (targetIds != null && targetIds.Any())
                {
                    var targets = targetIds.Select(id => new CampaignTarget
                    {
                        CampaignId = campaign.Id, // Yukarıda generate edilen ID
                        TargetId = id,
                        TargetKind = targetKind,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.CampaignTargets.AddRangeAsync(targets);
                    await _context.SaveChangesAsync();
                }

                // 3. Transaction commit et
                await transaction.CommitAsync();

                // 4. Hedeflerle birlikte döndür
                return await GetByIdWithTargetsAsync(campaign.Id)
                    ?? throw new InvalidOperationException("Kampanya oluşturuldu ama tekrar getirilemedi");
            }
            catch
            {
                // Hata oluşursa rollback yap
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

