using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services
{
    public class BannerRepository : IBannerRepository
    {
        private readonly ECommerceDbContext _context;

        public BannerRepository(ECommerceDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Banner>> GetAllAsync()
        {
            Console.WriteLine("🔍 BannerRepository.GetAllAsync çağrıldı");
            try
            {
                var banners = await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync();
                Console.WriteLine($"✅ BannerRepository: {banners.Count} banner bulundu");
                return banners;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ BannerRepository Error: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Banner?> GetByIdAsync(int id)
        {
            return await _context.Banners.FindAsync(id);
        }

        public async Task AddAsync(Banner banner)
        {
            banner.CreatedAt = DateTime.UtcNow;
            await _context.Banners.AddAsync(banner);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Banner banner)
        {
            var existing = await _context.Banners.FindAsync(banner.Id);
            if (existing != null)
            {
                // Temel alanlar
                existing.Title = banner.Title;
                existing.SubTitle = banner.SubTitle;
                existing.Description = banner.Description;
                existing.ImageUrl = banner.ImageUrl;
                existing.LinkUrl = banner.LinkUrl;
                existing.ButtonText = banner.ButtonText;
                existing.Type = banner.Type;
                existing.Position = banner.Position;
                existing.IsActive = banner.IsActive;
                existing.DisplayOrder = banner.DisplayOrder;
                
                // Tarih alanları
                existing.StartDate = banner.StartDate;
                existing.EndDate = banner.EndDate;
                
                // Analytics alanları (sadece açıkça güncelleniyorsa)
                existing.ClickCount = banner.ClickCount;
                existing.ViewCount = banner.ViewCount;
                
                // Güncelleme tarihi
                existing.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
        }
    }
}
