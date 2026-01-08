using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    public class BannerManager : IBannerService
    {
        private readonly IBannerRepository _bannerRepository;
        public BannerManager(IBannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        public async Task<IEnumerable<BannerDto>> GetAllAsync()
        {
            var banners = await _bannerRepository.GetAllAsync();
            return banners.Select(MapToDto);
        }

        public async Task<BannerDto?> GetByIdAsync(int id)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            return banner != null ? MapToDto(banner) : null;
        }

        public async Task AddAsync(BannerDto dto)
        {
            var banner = MapToEntity(dto);
            await _bannerRepository.AddAsync(banner);
        }

        public async Task UpdateAsync(BannerDto dto)
        {
            var banner = MapToEntity(dto);
            await _bannerRepository.UpdateAsync(banner);
        }

        public async Task DeleteAsync(int id)
        {
            await _bannerRepository.DeleteAsync(id);
        }

        private BannerDto MapToDto(Banner banner)
        {
            return new BannerDto
            {
                Id = banner.Id,
                Title = banner.Title,
                ImageUrl = banner.ImageUrl,
                LinkUrl = banner.LinkUrl,
                Type = banner.Type,
                IsActive = banner.IsActive,
                DisplayOrder = banner.DisplayOrder,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }

        private Banner MapToEntity(BannerDto dto)
        {
            return new Banner
            {
                Id = dto.Id,
                Title = dto.Title,
                ImageUrl = dto.ImageUrl,
                LinkUrl = dto.LinkUrl,
                Type = dto.Type,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }
    }
}