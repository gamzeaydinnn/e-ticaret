using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services
{
    public class BannerRepository : IBannerRepository
    {
        private readonly List<Banner> _banners = new();

        public Task<IEnumerable<Banner>> GetAllAsync()
        {
            return Task.FromResult(_banners.AsEnumerable());
        }

        public Task<Banner?> GetByIdAsync(int id)
        {
            return Task.FromResult(_banners.FirstOrDefault(b => b.Id == id));
        }

        public Task AddAsync(Banner banner)
        {
            banner.Id = _banners.Count > 0 ? _banners.Max(b => b.Id) + 1 : 1;
            _banners.Add(banner);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Banner banner)
        {
            var existing = _banners.FirstOrDefault(b => b.Id == banner.Id);
            if (existing != null)
            {
                existing.Title = banner.Title;
                existing.ImageUrl = banner.ImageUrl;
                existing.LinkUrl = banner.LinkUrl;
                existing.IsActive = banner.IsActive;
                existing.DisplayOrder = banner.DisplayOrder;
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id)
        {
            var banner = _banners.FirstOrDefault(b => b.Id == id);
            if (banner != null)
            {
                _banners.Remove(banner);
            }
            return Task.CompletedTask;
        }
    }
}
