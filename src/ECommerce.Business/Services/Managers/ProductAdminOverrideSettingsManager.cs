using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    public class ProductAdminOverrideSettingsManager : IProductAdminOverrideSettingsService
    {
        private readonly ECommerceDbContext _context;
        private readonly ILogger<ProductAdminOverrideSettingsManager> _logger;

        public ProductAdminOverrideSettingsManager(
            ECommerceDbContext context,
            ILogger<ProductAdminOverrideSettingsManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProductAdminOverrideSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            var entity = await GetOrCreateEntityAsync(cancellationToken);
            return Map(entity);
        }

        public async Task<ProductAdminOverrideSettingsDto> GetSettingsForAdminAsync(CancellationToken cancellationToken = default)
        {
            var entity = await GetOrCreateEntityAsync(cancellationToken);
            return Map(entity);
        }

        public async Task<ProductAdminOverrideSettingsDto> UpdateSettingsAsync(
            ProductAdminOverrideSettingsUpdateDto updateDto,
            int? updatedByUserId,
            string? updatedByUserName,
            CancellationToken cancellationToken = default)
        {
            var entity = await GetOrCreateEntityAsync(cancellationToken);

            if (updateDto.DefaultAdminOverrideName.HasValue)
            {
                entity.DefaultAdminOverrideName = updateDto.DefaultAdminOverrideName.Value;
            }

            if (updateDto.DefaultAdminOverridePrice.HasValue)
            {
                entity.DefaultAdminOverridePrice = updateDto.DefaultAdminOverridePrice.Value;
            }

            if (updateDto.DefaultAdminOverrideCategory.HasValue)
            {
                entity.DefaultAdminOverrideCategory = updateDto.DefaultAdminOverrideCategory.Value;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = updatedByUserId;
            entity.UpdatedByUserName = string.IsNullOrWhiteSpace(updatedByUserName)
                ? entity.UpdatedByUserName
                : updatedByUserName.Trim();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "[ProductAdminOverrideSettings] Güncellendi. Name={Name}, Price={Price}, Category={Category}",
                entity.DefaultAdminOverrideName,
                entity.DefaultAdminOverridePrice,
                entity.DefaultAdminOverrideCategory);

            return Map(entity);
        }

        private async Task<ProductAdminOverrideSetting> GetOrCreateEntityAsync(CancellationToken cancellationToken)
        {
            var entity = await _context.Set<ProductAdminOverrideSetting>()
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity != null)
            {
                return entity;
            }

            entity = new ProductAdminOverrideSetting
            {
                DefaultAdminOverrideName = false,
                DefaultAdminOverridePrice = false,
                DefaultAdminOverrideCategory = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<ProductAdminOverrideSetting>().Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        private static ProductAdminOverrideSettingsDto Map(ProductAdminOverrideSetting entity)
        {
            return new ProductAdminOverrideSettingsDto
            {
                Id = entity.Id,
                DefaultAdminOverrideName = entity.DefaultAdminOverrideName,
                DefaultAdminOverridePrice = entity.DefaultAdminOverridePrice,
                DefaultAdminOverrideCategory = entity.DefaultAdminOverrideCategory,
                UpdatedAt = entity.UpdatedAt,
                UpdatedByUserName = entity.UpdatedByUserName
            };
        }
    }
}