using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Banner/Poster yönetim servisi implementasyonu
    /// Ana sayfa görselleri (slider, promo kartları, banner'lar) için CRUD işlemleri
    /// </summary>
    public class BannerManager : IBannerService
    {
        private readonly IBannerRepository _bannerRepository;
        
        public BannerManager(IBannerRepository bannerRepository)
        {
            _bannerRepository = bannerRepository;
        }

        /// <summary>
        /// Tüm banner'ları DisplayOrder'a göre sıralı getirir
        /// </summary>
        public async Task<IEnumerable<BannerDto>> GetAllAsync()
        {
            var banners = await _bannerRepository.GetAllAsync();
            return banners.Select(MapToDto);
        }

        /// <summary>
        /// ID'ye göre tek bir banner getirir
        /// </summary>
        public async Task<BannerDto?> GetByIdAsync(int id)
        {
            var banner = await _bannerRepository.GetByIdAsync(id);
            return banner != null ? MapToDto(banner) : null;
        }

        /// <summary>
        /// Tipe göre banner'ları getirir (slider, promo, banner)
        /// Sadece aktif olanları, DisplayOrder'a göre sıralı döndürür
        /// </summary>
        public async Task<IEnumerable<BannerDto>> GetByTypeAsync(string type)
        {
            var banners = await _bannerRepository.GetAllAsync();
            return banners
                .Where(b => b.IsActive && 
                           b.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.DisplayOrder)
                .Select(MapToDto);
        }

        /// <summary>
        /// Sadece aktif banner'ları DisplayOrder'a göre sıralı getirir
        /// </summary>
        public async Task<IEnumerable<BannerDto>> GetActiveAsync()
        {
            var banners = await _bannerRepository.GetAllAsync();
            return banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .Select(MapToDto);
        }

        /// <summary>
        /// Yeni banner ekler
        /// </summary>
        public async Task AddAsync(BannerDto dto)
        {
            var banner = MapToEntity(dto);
            banner.CreatedAt = DateTime.UtcNow;
            await _bannerRepository.AddAsync(banner);
        }

        /// <summary>
        /// Mevcut banner'ı günceller
        /// </summary>
        public async Task UpdateAsync(BannerDto dto)
        {
            var banner = MapToEntity(dto);
            banner.UpdatedAt = DateTime.UtcNow;
            await _bannerRepository.UpdateAsync(banner);
        }

        /// <summary>
        /// Banner'ı siler
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            await _bannerRepository.DeleteAsync(id);
        }

        #region Mapping Methods
        
        /// <summary>
        /// Entity'yi DTO'ya dönüştürür
        /// Tüm alanlar (yeni eklenenler dahil) kopyalanır
        /// </summary>
        private BannerDto MapToDto(Banner banner)
        {
            return new BannerDto
            {
                Id = banner.Id,
                Title = banner.Title,
                SubTitle = banner.SubTitle,
                Description = banner.Description,
                ImageUrl = banner.ImageUrl,
                LinkUrl = banner.LinkUrl,
                ButtonText = banner.ButtonText,
                Type = banner.Type,
                Position = banner.Position,
                IsActive = banner.IsActive,
                DisplayOrder = banner.DisplayOrder,
                StartDate = banner.StartDate,
                EndDate = banner.EndDate,
                ClickCount = banner.ClickCount,
                ViewCount = banner.ViewCount,
                CreatedAt = banner.CreatedAt,
                UpdatedAt = banner.UpdatedAt
            };
        }

        /// <summary>
        /// DTO'yu Entity'ye dönüştürür
        /// Tüm alanlar (yeni eklenenler dahil) kopyalanır
        /// </summary>
        private Banner MapToEntity(BannerDto dto)
        {
            return new Banner
            {
                Id = dto.Id,
                Title = dto.Title,
                SubTitle = dto.SubTitle,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                LinkUrl = dto.LinkUrl,
                ButtonText = dto.ButtonText,
                Type = dto.Type,
                Position = dto.Position,
                IsActive = dto.IsActive,
                DisplayOrder = dto.DisplayOrder,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                ClickCount = dto.ClickCount,
                ViewCount = dto.ViewCount,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt
            };
        }
        
        #endregion
    }
}